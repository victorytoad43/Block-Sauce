using Photon.Deterministic;
using Quantum;
using Quantum.Physics3D;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: while the local player holds Throw to aim, draws the throw's trajectory and a circle
    /// where it lands. The arc is stepped from the simulation's own throw velocity (ThrowHelper) at the simulation's tick
    /// rate and gravity, and the landing is found with read-only Quantum raycasts, so it shows the throw the simulation
    /// will make. View only: it never writes to the simulation.
    /// </summary>
    public class ThrowAimView : QuantumEntityViewComponent
    {
        [Tooltip("Material for the trajectory line and landing circle.")]
        [SerializeField] private Material _material;
        [Tooltip("Width of the trajectory line, in meters.")]
        [SerializeField] private float _lineWidth = 0.05f;
        [Tooltip("Radius of the landing circle, in meters.")]
        [SerializeField] private float _circleRadius = 0.5f;
        [Tooltip("The longest flight the preview follows, in seconds.")]
        [SerializeField] private float _maxFlightSeconds = 3f;

        private const int CircleSegments = 32;

        private LineRenderer _line;
        private LineRenderer _circle;
        private bool _isLocal;
        private readonly System.Collections.Generic.List<Vector3> _points = new System.Collections.Generic.List<Vector3>();

        public override void OnActivate(Frame frame)
        {
            _isLocal = Game.PlayerIsLocal(frame.Get<Player>(EntityRef).Ref);
            if (_isLocal == false)
                return;

            _line = CreateLine("ThrowAimLine", false);
            _circle = CreateLine("ThrowAimCircle", true);
            _circle.positionCount = CircleSegments;
            SetVisible(false);
        }

        public override void OnDeactivate()
        {
            if (_line != null) Destroy(_line.gameObject);
            if (_circle != null) Destroy(_circle.gameObject);
        }

        public override void OnUpdateView()
        {
            if (_isLocal == false)
                return;

            Frame frame = PredictedFrame;
            if (frame == null || frame.TryGet(EntityRef, out Grabber grabber) == false
                || grabber.IsAiming == false || frame.Exists(grabber.Held) == false)
            {
                SetVisible(false);
                return;
            }

            GrabConfig config = frame.FindAsset(grabber.Config);
            FPVector3 velocity = ThrowHelper.GetThrowVelocity(frame, EntityRef, config, out _);
            FPVector3 position = frame.Get<Transform3D>(grabber.Held).Position;
            FPVector3 gravity = frame.SimulationConfig.Physics.Gravity;
            FP dt = frame.DeltaTime;
            int steps = Mathf.CeilToInt(_maxFlightSeconds / dt.AsFloat);

            _points.Clear();
            _points.Add(position.ToUnityVector3());
            bool landed = false;
            Vector3 landingPoint = default, landingNormal = Vector3.up;

            for (int i = 0; i < steps && landed == false; i++)
            {
                velocity += gravity * dt;
                FPVector3 next = position + velocity * dt;
                if (TryHitSegment(frame, position, next, grabber.Held, out Hit3D hit))
                {
                    landingPoint = hit.Point.ToUnityVector3();
                    landingNormal = hit.Normal.ToUnityVector3();
                    _points.Add(landingPoint);
                    landed = true;
                    break;
                }

                position = next;
                _points.Add(position.ToUnityVector3());
            }

            _line.positionCount = _points.Count;
            _line.SetPositions(_points.ToArray());
            _line.enabled = true;

            _circle.enabled = landed;
            if (landed)
            {
                DrawCircle(landingPoint + landingNormal * 0.02f, landingNormal);
            }
        }

        /// <summary>
        /// Returns the first solid hit between two points of the arc, skipping the thrower and the block being thrown.
        /// </summary>
        private bool TryHitSegment(Frame frame, FPVector3 from, FPVector3 to, EntityRef held, out Hit3D hit)
        {
            hit = default;
            FPVector3 delta = to - from;
            FP length = delta.Magnitude;
            if (length <= FP._0)
                return false;

            HitCollection3D hits = frame.Physics3D.RaycastAll(from, delta / length, length, -1,
                QueryOptions.HitStatics | QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.ComputeDetailedInfo);
            hits.SortCastDistance();
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].Entity == EntityRef || hits[i].Entity == held)
                    continue;

                hit = hits[i];
                return true;
            }

            return false;
        }

        private void DrawCircle(Vector3 center, Vector3 normal)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegments;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _circleRadius;
                _circle.SetPosition(i, center + rotation * offset);
            }
        }

        private LineRenderer CreateLine(string name, bool loop)
        {
            var go = new GameObject(name);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.widthMultiplier = _lineWidth;
            line.sharedMaterial = _material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void SetVisible(bool visible)
        {
            if (_line != null) _line.enabled = visible;
            if (_circle != null) _circle.enabled = visible;
        }
    }
}
