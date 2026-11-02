using Photon.Deterministic;
using Quantum;
using Quantum.Physics3D;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: plants Guy's feet on uneven ground. Each foot looks straight down for the ground
    /// (statics, blocks and anything else in the predicted frame, except the character and what it holds), the hips drop
    /// so the lower foot can reach, and each leg bends (two-bone IK) to keep the foot's animated lift above its own
    /// ground, tilted to the slope. Only while standing, walking or running on the ground; it fades out for jumps,
    /// sitting, knockdowns and get-ups. Runs before the hold pose, which reaches for the block from the moved hips.
    /// View only: it reads the predicted frame and never writes to the simulation.
    /// </summary>
    public class FootIKView : QuantumEntityViewComponent
    {
        private static readonly int IdleId = Animator.StringToHash("Idle");
        private static readonly int WalkId = Animator.StringToHash("Walk");
        private static readonly int RunId = Animator.StringToHash("Run");

        [Tooltip("The Animator on Guy's model.")]
        [SerializeField] private Animator _animator;
        [Tooltip("How far above the character's feet each ground probe starts, in meters.")]
        [SerializeField] private float _probeHeight = 0.5f;
        [Tooltip("How far below the character's feet each ground probe reaches, in meters.")]
        [SerializeField] private float _probeDepth = 0.5f;
        [Tooltip("The furthest the hips drop to let a foot reach lower ground, in meters.")]
        [SerializeField] private float _maxHipDrop = 0.35f;
        [Tooltip("The steepest slope, in degrees, a planted foot tilts to match.")]
        [SerializeField] private float _maxFootTilt = 30f;
        [Tooltip("How fast the hips follow the ground, per second.")]
        [SerializeField] private float _hipSharpness = 12f;
        [Tooltip("Seconds the IK takes to fade in or out.")]
        [SerializeField] private float _fadeSeconds = 0.15f;

        private Transform _hips;
        private Transform _thighL, _shinL, _footL;
        private Transform _thighR, _shinR, _footR;
        private float _weight;
        private float _hipOffset;

        public override void OnInitialize()
        {
            foreach (Transform bone in _animator.GetComponentsInChildren<Transform>())
            {
                switch (bone.name)
                {
                    case "hips": _hips = bone; break;
                    case "thigh.L": _thighL = bone; break;
                    case "shin.L": _shinL = bone; break;
                    case "foot.L": _footL = bone; break;
                    case "thigh.R": _thighR = bone; break;
                    case "shin.R": _shinR = bone; break;
                    case "foot.R": _footR = bone; break;
                }
            }
        }

        public override void OnUpdateView()
        {
            _weight = Mathf.MoveTowards(_weight, IsPlanted() ? 1f : 0f, Time.deltaTime / _fadeSeconds);
        }

        public override void OnLateUpdateView()
        {
            Frame frame = PredictedFrame;
            if (_weight <= 0f || _hips == null || _footL == null || _footR == null || frame == null)
            {
                _hipOffset = 0f;
                return;
            }

            float rootY = transform.position.y;
            EntityRef held = frame.TryGet(EntityRef, out Grabber grabber) ? grabber.Held : EntityRef.None;
            bool hitL = TryFindGround(frame, _footL.position, held, out Vector3 groundL, out Vector3 normalL);
            bool hitR = TryFindGround(frame, _footR.position, held, out Vector3 groundR, out Vector3 normalR);

            // The hips drop by the lower foot's ground, so that leg can reach it; the other leg bends.
            float drop = Mathf.Min(hitL ? groundL.y - rootY : 0f, hitR ? groundR.y - rootY : 0f, 0f);
            drop = Mathf.Max(drop, -_maxHipDrop);
            _hipOffset = Mathf.Lerp(_hipOffset, drop, 1f - Mathf.Exp(-_hipSharpness * Time.deltaTime));
            _hips.position += Vector3.up * (_hipOffset * _weight);

            if (hitL)
            {
                Plant(_thighL, _shinL, _footL, rootY, groundL, normalL);
            }

            if (hitR)
            {
                Plant(_thighR, _shinR, _footR, rootY, groundR, normalR);
            }
        }

        /// <summary>
        /// True while the character stands, walks or runs on the ground, where the feet should meet it.
        /// </summary>
        private bool IsPlanted()
        {
            Frame frame = PredictedFrame;
            if (frame == null || frame.Has<RagdollOwner>(EntityRef))
                return false;

            if (frame.TryGet(EntityRef, out MovementData movement) && (movement.IsSitting || movement.IsSliding))
                return false;

            if (frame.TryGet(EntityRef, out GroundedTracker grounded) == false || grounded.IsGrounded == false)
                return false;

            int state = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            return state == IdleId || state == WalkId || state == RunId;
        }

        /// <summary>
        /// Bends the leg so the foot keeps its animated lift above the ground under it, tilted toward the slope.
        /// </summary>
        private void Plant(Transform thigh, Transform shin, Transform foot, float rootY, Vector3 ground, Vector3 normal)
        {
            float lift = foot.position.y - (rootY + _hipOffset * _weight);
            Vector3 target = new Vector3(foot.position.x, ground.y + Mathf.Max(lift, 0f), foot.position.z);
            Quaternion tilt = Quaternion.RotateTowards(Quaternion.identity,
                Quaternion.FromToRotation(Vector3.up, normal), _maxFootTilt);
            Quaternion footRotation = Quaternion.Slerp(Quaternion.identity, tilt, _weight) * foot.rotation;

            TwoBoneIK.Solve(thigh, shin, foot, Vector3.Lerp(foot.position, target, _weight));
            foot.rotation = footRotation;
        }

        /// <summary>
        /// Finds the ground straight under <paramref name="foot"/> in the predicted frame, skipping the character itself
        /// and what it holds.
        /// </summary>
        private bool TryFindGround(Frame frame, Vector3 foot, EntityRef held, out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = Vector3.up;
            Vector3 origin = new Vector3(foot.x, transform.position.y + _probeHeight, foot.z);
            HitCollection3D hits = frame.Physics3D.RaycastAll(origin.ToFPVector3(), FPVector3.Down,
                (_probeHeight + _probeDepth).ToFP(), -1,
                QueryOptions.HitStatics | QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.ComputeDetailedInfo);
            hits.SortCastDistance();
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].Entity == EntityRef || (held != EntityRef.None && hits[i].Entity == held))
                    continue;

                point = hits[i].Point.ToUnityVector3();
                normal = hits[i].Normal.ToUnityVector3();
                return true;
            }

            return false;
        }
    }
}
