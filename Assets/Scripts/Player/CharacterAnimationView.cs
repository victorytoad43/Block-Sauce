using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: drives Guy's Animator from the simulation's horizontal speed. Slow movement plays
    /// the walk clip, sped up to match the ground speed so the feet don't slide, up to a cap. View only: it reads the
    /// predicted frame and never writes to the simulation.
    /// </summary>
    public class CharacterAnimationView : QuantumEntityViewComponent
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int WalkRateId = Animator.StringToHash("WalkRate");

        [Tooltip("The Animator on Guy's model.")]
        [SerializeField] private Animator _animator;
        [Tooltip("Ground speed, in m/s, at which the walk clip's planted foot moves at normal playback speed.")]
        [SerializeField] private float _walkClipSpeed = 0.3f;
        [Tooltip("The fastest the walk clip plays, as a multiple of normal speed.")]
        [SerializeField] private float _maxWalkRate = 4f;

        public override void OnUpdateView()
        {
            if (_animator == null || TryGetPredictedQuantumComponent(out PhysicsBody3D body) == false)
                return;

            Vector3 velocity = body.Velocity.ToUnityVector3();
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            _animator.SetFloat(SpeedId, speed);
            _animator.SetFloat(WalkRateId, Mathf.Min(speed / _walkClipSpeed, _maxWalkRate));
        }
    }
}
