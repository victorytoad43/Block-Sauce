using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// In the scene: the one-off sauce effects. A block going into a vat splashes the vat's sauce up from the surface,
    /// and a Stack-sauced block shoving rivals away from its tower throws out a ring the size of its reach. View only:
    /// it reacts to simulation events.
    /// </summary>
    public class SauceFxView : MonoBehaviour
    {
        [Tooltip("The sauces' colors and particle material.")]
        [SerializeField] private SauceLooks _looks;
        [Tooltip("Droplets per splash.")]
        [SerializeField] private int _splashCount = 24;
        [Tooltip("Particles in the Stack pulse ring.")]
        [SerializeField] private int _pulseCount = 48;
        [Tooltip("Seconds the pulse ring takes to reach the edge of the shove.")]
        [SerializeField] private float _pulseSeconds = 0.35f;

        private ParticleSystem _splash;
        private ParticleSystem _pulse;

        private void OnEnable()
        {
            if (_looks == null || _looks.ParticleMaterial == null)
                return;

            _splash ??= SauceParticles.CreateSplash(transform, _looks.ParticleMaterial);
            _pulse ??= SauceParticles.CreatePulseRing(transform, _looks.ParticleMaterial);

            QuantumEvent.Subscribe(this, (EventBlockDipStarted e) => OnDipStarted(e));
            QuantumEvent.Subscribe(this, (EventStackSaucePulse e) => OnPulse(e));
        }

        private void OnDisable()
        {
            QuantumEvent.UnsubscribeListener(this);
        }

        private void OnDipStarted(EventBlockDipStarted e)
        {
            Frame frame = e.Game.Frames.Predicted;
            if (frame.TryGet(e.Vat, out SauceVat vat) == false || frame.TryGet(e.Block, out Transform3D block) == false)
                return;

            Vector3 position = block.Position.ToUnityVector3();
            position.y = frame.Get<Transform3D>(e.Vat).Position.Y.AsFloat + vat.SurfaceHeight.AsFloat;
            SauceParticles.Burst(_splash, position, _looks.GetColor(e.Type), _splashCount);
        }

        private void OnPulse(EventStackSaucePulse e)
        {
            Vector3 position = e.Position.ToUnityVector3();
            position.y = Mathf.Max(0.1f, position.y - 0.3f);
            SauceParticles.Burst(_pulse, position, _looks.GetParticleColor(SauceType.Stack), _pulseCount,
                e.Radius.AsFloat / _pulseSeconds);
        }
    }
}
