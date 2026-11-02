using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: drives Guy's Animator from the simulation. Standing still plays the idle; slow
    /// movement plays the walk clips and fast movement the run clips (the controller switches between them on Speed),
    /// each sped up to match the ground speed so the feet don't slide, within limits. The walk and run blend between
    /// forward, backward and strafing by the move direction relative to the facing (a carrier strafes and backs off
    /// while the swing holds its facing). Sitting plays the sit-down, blending into the slide while the sitting body
    /// moves fast. A jump plays the jump start, time in the air the jump loop, and touching down the landing. A
    /// stagger plays a flinch for the side that was hit, over the whole body. View only: it reads the predicted frame
    /// and simulation events, and never writes to the simulation.
    /// </summary>
    public class CharacterAnimationView : QuantumEntityViewComponent
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int WalkRateId = Animator.StringToHash("WalkRate");
        private static readonly int RunRateId = Animator.StringToHash("RunRate");
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveZId = Animator.StringToHash("MoveZ");
        private static readonly int SittingId = Animator.StringToHash("Sitting");
        private static readonly int AirborneId = Animator.StringToHash("Airborne");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int StruckFrontId = Animator.StringToHash("StruckFront");
        private static readonly int StruckBackId = Animator.StringToHash("StruckBack");
        private static readonly int StruckLeftId = Animator.StringToHash("StruckLeft");
        private static readonly int StruckRightId = Animator.StringToHash("StruckRight");
        private const string FlinchLayerName = "Flinch";

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
        [Tooltip("The share of a flinch, at its end, over which it fades back out to the other layers.")]
        [SerializeField] private float _flinchFadeOut = 0.3f;

        private float _airTime;
        private int _flinchLayer = -1;
        private bool _isFlinching;
        private int _flinchState;

        public override void OnActivate(Frame frame)
        {
            if (_animator != null)
            {
                _flinchLayer = _animator.GetLayerIndex(FlinchLayerName);
            }

            QuantumEvent.Subscribe(this, (EventJumped e) =>
            {
                if (e.Entity == EntityRef && _animator != null)
                {
                    _animator.SetTrigger(JumpId);
                }
            });
            QuantumEvent.Subscribe(this, (EventKnockedBack e) =>
            {
                if (e.Entity == EntityRef && e.Staggered)
                {
                    PlayFlinch(e.Impulse.ToUnityVector3());
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

            Vector3 localDirection = speed > 0.05f ? transform.InverseTransformDirection(velocity / speed) : Vector3.forward;
            _animator.SetFloat(MoveXId, localDirection.x);
            _animator.SetFloat(MoveZId, localDirection.z);

            // A downed character is never in the air or sitting as far as the animation goes: the ragdoll poses it,
            // and the get-up must not be cut short by a fall or a sit.
            bool isDown = PredictedFrame.Has<RagdollOwner>(EntityRef);
            bool isSitting = isDown == false && TryGetPredictedQuantumComponent(out MovementData movement) && movement.IsSitting;
            _animator.SetBool(SittingId, isSitting);

            bool onGround = isDown || (TryGetPredictedQuantumComponent(out GroundedTracker grounded) && grounded.IsGrounded);
            _airTime = onGround ? 0f : _airTime + Time.deltaTime;
            _animator.SetBool(AirborneId, _airTime >= _airborneDelay);

            UpdateFlinchWeight(isDown || isSitting);
        }

        /// <summary>
        /// Plays the flinch for the side the hit came from: the impulse pushes away from that side.
        /// </summary>
        private void PlayFlinch(Vector3 impulse)
        {
            if (_animator == null || _flinchLayer < 0)
                return;

            Vector3 local = transform.InverseTransformDirection(impulse);
            int state = Mathf.Abs(local.z) >= Mathf.Abs(local.x)
                ? (local.z < 0f ? StruckFrontId : StruckBackId)
                : (local.x > 0f ? StruckLeftId : StruckRightId);

            _animator.Play(state, _flinchLayer, 0f);
            _flinchState = state;
            _animator.SetLayerWeight(_flinchLayer, 1f);
            _isFlinching = true;
        }

        /// <summary>
        /// Fades the flinch layer out over the end of its clip, and drops it at once while down or sitting, where a
        /// standing flinch would fight the pose.
        /// </summary>
        private void UpdateFlinchWeight(bool suppress)
        {
            if (_isFlinching == false)
                return;

            // Until the Animator has updated, the layer still reports the previous flinch's finished time.
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(_flinchLayer);
            float time = state.shortNameHash == _flinchState ? state.normalizedTime : 0f;
            float weight = suppress ? 0f : Mathf.Clamp01((1f - time) / _flinchFadeOut);
            _animator.SetLayerWeight(_flinchLayer, weight);
            _isFlinching = weight > 0f;
        }
    }
}
