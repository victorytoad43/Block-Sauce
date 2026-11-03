using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// In the scene: the one-off sauce effects. A block going into a vat splashes the vat's sauce up from the surface.
    /// View only: it reacts to simulation events.
    /// </summary>
    public class SauceFxView : MonoBehaviour
    {
        [Tooltip("The sauces' colors and particle material.")]
        [SerializeField] private SauceLooks _looks;
        [Tooltip("Droplets per splash.")]
        [SerializeField] private int _splashCount = 24;

        private ParticleSystem _splash;

        private void OnEnable()
        {
            if (_looks == null || _looks.ParticleMaterial == null)
                return;

            _splash ??= SauceParticles.CreateSplash(transform, _looks.ParticleMaterial);

            QuantumEvent.Subscribe(this, (EventBlockDipStarted e) => OnDipStarted(e));
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
    }
}
