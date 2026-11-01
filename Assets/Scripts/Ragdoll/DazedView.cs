using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: the dazed indicator (A-016) over a knocked-down player's head, in the player's
    /// color, spinning, and shrinking as the knockdown runs out so others can read how long is left. It pops in on the
    /// knockdown and off on the get-up. View only.
    /// </summary>
    public class DazedView : QuantumEntityViewComponent
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Reads the ragdoll's head while the character is down.")]
        [SerializeField] private RagdollPoseView _pose;
        [Tooltip("The dazed effect model (FX_Daze), instantiated once and shown while down.")]
        [SerializeField] private GameObject _effectPrefab;
        [SerializeField] private PlayerPalette _palette;
        [Tooltip("Meters above the neck joint, along the head, the effect floats.")]
        [SerializeField] private float _heightAboveNeck = 0.6f;
        [Tooltip("Degrees per second the effect spins.")]
        [SerializeField] private float _spinSpeed = 360f;
        [Tooltip("The effect's scale at the start of a knockdown; it shrinks toward the minimum as time runs out.")]
        [SerializeField] private float _maxScale = 4f;
        [SerializeField] private float _minScale = 1.5f;
        [Tooltip("Seconds the pop-in and pop-off take.")]
        [SerializeField] private float _popSeconds = 0.12f;

        private Transform _effect;
        private float _spin;
        private float _pop;

        public override void OnActivate(Frame frame)
        {
            _effect = Instantiate(_effectPrefab, transform).transform;
            _effect.gameObject.SetActive(false);

            int index = frame.TryGet(EntityRef, out Quantum.Player player) ? (int)player.Ref : -1;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, _palette.GetColor(index));
            foreach (Renderer renderer in _effect.GetComponentsInChildren<Renderer>())
            {
                renderer.SetPropertyBlock(block);
            }
        }

        public override void OnDeactivate()
        {
            if (_effect != null)
            {
                Destroy(_effect.gameObject);
            }
        }

        public override void OnLateUpdateView()
        {
            Transform head = _pose.GetLimb("head");
            _pop = Mathf.MoveTowards(_pop, head != null ? 1f : 0f, Time.deltaTime / _popSeconds);

            if (_pop <= 0f)
            {
                _effect.gameObject.SetActive(false);
                return;
            }

            _effect.gameObject.SetActive(true);
            if (head != null)
            {
                _effect.position = head.position + head.up * _heightAboveNeck;
            }

            _spin += _spinSpeed * Time.deltaTime;
            _effect.rotation = Quaternion.Euler(0f, _spin, 0f);
            _effect.localScale = Vector3.one * (Mathf.Lerp(_minScale, _maxScale, GetTimeLeft()) * _pop);
        }

        /// <summary>
        /// Returns the share of the knockdown still to run, from 1 at the hit to 0 at the get-up.
        /// </summary>
        private float GetTimeLeft()
        {
            Frame frame = PredictedFrame;
            if (frame == null || frame.TryGet(EntityRef, out RagdollOwner owner) == false
                || frame.TryGet(EntityRef, out Ragdollable ragdollable) == false)
                return 0f;

            float duration = frame.FindAsset(ragdollable.Config).Duration.AsFloat;
            return duration > 0f ? Mathf.Clamp01(owner.Timer.RemainingSeconds(frame).AsFloat / duration) : 0f;
        }
    }
}
