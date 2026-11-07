using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// Guy looks at the block a Grab press would take right now (<see cref="GrabSystem.PeekGrabTarget"/>, the same
    /// rule the grab uses), so every player can see what a character is about to pick up: the head turns toward it,
    /// with a little of the chest, and the eyes take the rest. Only with empty hands and on his feet; it fades in and
    /// out and eases from one block to the next. View only: it reads the predicted frame and never writes.
    /// </summary>
    public class HeadLookView : QuantumEntityViewComponent
    {
        [Tooltip("The Animator on Guy's model.")]
        [SerializeField] private Animator _animator;
        [Tooltip("The furthest the head turns from where the animation points it, in degrees.")]
        [SerializeField] private float _maxHeadAngle = 65f;
        [Tooltip("The share of the head's turn the chest takes as well.")]
        [SerializeField, Range(0f, 1f)] private float _chestShare = 0.2f;
        [Tooltip("The furthest the eyes turn past the head, in degrees.")]
        [SerializeField] private float _maxEyeAngle = 25f;
        [Tooltip("Seconds the look takes to fade in or out.")]
        [SerializeField] private float _fadeSeconds = 0.2f;
        [Tooltip("How fast the look point moves to a new block, per second.")]
        [SerializeField] private float _followSharpness = 12f;

        private Transform _chest;
        private Transform _head;
        private Transform _eyeL;
        private Transform _eyeR;
        private float _weight;
        private Vector3 _lookPoint;
        private bool _hasLookPoint;

        public override void OnInitialize()
        {
            foreach (Transform bone in _animator.GetComponentsInChildren<Transform>())
            {
                switch (bone.name)
                {
                    case "chest": _chest = bone; break;
                    case "head": _head = bone; break;
                    case "eye.L": _eyeL = bone; break;
                    case "eye.R": _eyeR = bone; break;
                }
            }
        }

        public override void OnLateUpdateView()
        {
            if (_head == null)
                return;

            bool hasTarget = TryGetTarget(out Vector3 target);
            _weight = Mathf.MoveTowards(_weight, hasTarget ? 1f : 0f, Time.deltaTime / _fadeSeconds);
            if (hasTarget)
            {
                _lookPoint = _hasLookPoint
                    ? Vector3.Lerp(_lookPoint, target, 1f - Mathf.Exp(-_followSharpness * Time.deltaTime))
                    : target;
                _hasLookPoint = true;
            }

            if (_weight <= 0f)
            {
                _hasLookPoint = false;
                return;
            }

            // The face points along the model's forward; turn it toward the block, limited, and share some with the chest.
            Vector3 face = _animator.transform.forward;
            Quaternion turn = LimitedTurn(face, _lookPoint - _head.position, _maxHeadAngle);
            Quaternion weighted = Quaternion.Slerp(Quaternion.identity, turn, _weight);
            if (_chest != null && _chestShare > 0f)
            {
                _chest.rotation = Quaternion.Slerp(Quaternion.identity, weighted, _chestShare) * _chest.rotation;
            }

            _head.rotation = weighted * _head.rotation;

            // The eyes' own axis points up their bone (local Y is the gaze); they take what the head couldn't.
            TurnEye(_eyeL);
            TurnEye(_eyeR);
        }

        private void TurnEye(Transform eye)
        {
            if (eye == null)
                return;

            Quaternion turn = LimitedTurn(eye.up, _lookPoint - eye.position, _maxEyeAngle);
            eye.rotation = Quaternion.Slerp(Quaternion.identity, turn, _weight) * eye.rotation;
        }

        /// <summary>
        /// The rotation from <paramref name="from"/> toward <paramref name="to"/>, at most <paramref name="maxAngle"/>.
        /// </summary>
        private static Quaternion LimitedTurn(Vector3 from, Vector3 to, float maxAngle)
        {
            if (to.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            Quaternion full = Quaternion.FromToRotation(from, to);
            full.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return Quaternion.AngleAxis(Mathf.Clamp(angle, -maxAngle, maxAngle), axis);
        }

        /// <summary>
        /// Where the block this character would grab is drawn now, if they have empty hands, are on their feet and a
        /// block is in reach.
        /// </summary>
        private bool TryGetTarget(out Vector3 target)
        {
            target = default;
            Frame frame = PredictedFrame;
            if (frame == null || frame.Exists(EntityRef) == false || frame.Has<RagdollOwner>(EntityRef))
                return false;

            if (frame.TryGet(EntityRef, out Grabber grabber) == false || grabber.Held != EntityRef.None)
                return false;

            if (frame.TryGet(EntityRef, out MovementData movement) && movement.Tier != ControlTier.InControl)
                return false;

            GrabConfig config = frame.FindAsset(grabber.Config);
            if (config == null)
                return false;

            EntityRef block = GrabSystem.PeekGrabTarget(frame, EntityRef, config);
            if (block == EntityRef.None)
                return false;

            // The block's drawn (interpolated) position, so the look doesn't step with the simulation's ticks.
            QuantumEntityView view = EntityView != null && EntityView.EntityViewUpdater != null ? EntityView.EntityViewUpdater.GetView(block) : null;
            target = view != null ? view.transform.position : frame.Get<Transform3D>(block).Position.ToUnityVector3();
            return true;
        }
    }
}
