using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a block's view: shows who owns it and how far it has settled into its stack (the loading effect), by feeding
    /// the owner's color and the settle progress to the block's shader. A counted block shows solid. View only.
    /// </summary>
    public class BlockView : QuantumEntityViewComponent
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LockColorId = Shader.PropertyToID("_LockColor");
        private static readonly int LockProgressId = Shader.PropertyToID("_LockProgress");
        private static readonly int LockedId = Shader.PropertyToID("_Locked");

        [Tooltip("The player colors.")]
        [SerializeField] private PlayerPalette _palette;
        [Tooltip("The block's renderer.")]
        [SerializeField] private Renderer _renderer;

        private MaterialPropertyBlock _properties;

        public override void OnUpdateView()
        {
            if (_renderer == null || _palette == null || TryGetPredictedQuantumComponent(out Block block) == false)
                return;

            _properties ??= new MaterialPropertyBlock();
            int owner = block.Owner.IsValid ? (int)block.Owner : -1;
            _properties.SetColor(BaseColorId, _palette.GetColor(owner));
            _properties.SetColor(LockColorId, _palette.GetLockColor(owner));
            _properties.SetFloat(LockProgressId, block.Stack == StackState.Settling ? block.StackProgress.AsFloat : 0f);
            _properties.SetFloat(LockedId, block.Stack == StackState.Counted ? 1f : 0f);
            _renderer.SetPropertyBlock(_properties);
        }
    }
}
