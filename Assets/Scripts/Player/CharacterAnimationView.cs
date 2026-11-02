using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: drives Guy's Animator from the simulation. Standing still plays the idle; slow
    /// movement plays the walk clip and fast movement the run clip (the controller switches between them on Speed),
    /// each sped up to match the ground speed so the feet don't slide, within limits. Moving against the facing (a
    /// carrier backing off while the swing holds its facing) blends to the backward walk and run. A jump plays the
    /// jump start, time in the air the jump loop, and touching down the landing. View only: it reads the predicted
    /// frame and simulation events, and never writes to the simulation.
    /// </summary>
    public class CharacterAnimationView : QuantumEntityViewComponent
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int WalkRateId = Animator.StringToHash("WalkRate");
        private static readonly int RunRateId = Animator.StringToHash("RunRate");
        private static readonly int ForwardId = Animator.StringToHash("Forward");
        private static readonly int AirborneId = Animator.StringToHash("Airborne");
        private static readonly int JumpId = Animator.StringToHash("Jump");

        [Tooltip("The Animator on Guy's model.")]
        [SerializeField] private Animator _animator;
        [Tooltip("Ground speed, in m/s, at which the walk clip's planted foot moves at normal playback speed.")]
        [SerializeField] private float _walkClipSpeed = 1f;
        [Tooltip("The fastest the walk clip plays, as a multiple of normal speed.")]
        [SerializeField] private float _maxWalkRate = 4f;
        [Tooltip("Ground speed, in m/s, at which the run clip's planted foot moves at normal playback speed.")]
        [SerializeField] private float _runClipSpeed = 3.2f;
        [Tooltip("The slowest and fastest the run clip plays, as multiples of normal speed.")]
        [SerializeField] private Vector2 _runRateRange = new Vector2(0.8f, 2f);
        [Tooltip("Seconds off the ground before the character counts as in the air, so steps and bumps don't play a fall.")]
        [SerializeField] private float _airborneDelay = 0.12f;

        private float _airTime;

        public override void OnActivate(Frame frame)
        {
            QuantumEvent.Subscribe(this, (EventJumped e) =>
            {
                if (e.Entity == EntityRef && _animator != null)
                {
                    _animator.SetTrigger(JumpId);
                }
            });
        }

        public override void OnDeactivate()
        {
            QuantumEvent.UnsubscribeListener(this);
        }

        public override void OnUpdateView()
        {
            if (_animator == null || TryGetPredictedQuantumComponent(out PhysicsBody3D body) == false)
                return;

            Vector3 velocity = body.Velocity.ToUnityVector3();
            velocity.y = 0f;
            float speed = velocity.magnitude;
            _animator.SetFloat(SpeedId, speed);
            _animator.SetFloat(WalkRateId, Mathf.Min(speed / _walkClipSpeed, _maxWalkRate));
            _animator.SetFloat(RunRateId, Mathf.Clamp(speed / _runClipSpeed, _runRateRange.x, _runRateRange.y));
            _animator.SetFloat(ForwardId, speed > 0.05f ? Vector3.Dot(velocity / speed, transform.forward) : 1f);

            // A downed character is never in the air as far as the animation goes: the ragdoll poses it, and the get-up
            // must not be cut short by a fall.
            bool onGround = PredictedFrame.Has<RagdollOwner>(EntityRef)
                || (TryGetPredictedQuantumComponent(out GroundedTracker grounded) && grounded.IsGrounded);
            _airTime = onGround ? 0f : _airTime + Time.deltaTime;
            _animator.SetBool(AirborneId, _airTime >= _airborneDelay);
        }
    }
}
