using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: drives Guy's Animator from the simulation's horizontal speed. Slow movement plays
    /// the walk clip and fast movement the run clip (the controller switches between them on Speed), each sped up to
    /// match the ground speed so the feet don't slide, within limits. View only: it reads the predicted frame and never
    /// writes to the simulation.
    /// </summary>
    public class CharacterAnimationView : QuantumEntityViewComponent
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int WalkRateId = Animator.StringToHash("WalkRate");
        private static readonly int RunRateId = Animator.StringToHash("RunRate");

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

        public override void OnUpdateView()
        {
            if (_animator == null || TryGetPredictedQuantumComponent(out PhysicsBody3D body) == false)
                return;

            Vector3 velocity = body.Velocity.ToUnityVector3();
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            _animator.SetFloat(SpeedId, speed);
            _animator.SetFloat(WalkRateId, Mathf.Min(speed / _walkClipSpeed, _maxWalkRate));
            _animator.SetFloat(RunRateId, Mathf.Clamp(speed / _runClipSpeed, _runRateRange.x, _runRateRange.y));
        }
    }
}
