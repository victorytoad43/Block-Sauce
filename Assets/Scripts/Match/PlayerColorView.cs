using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a player character's view: tints the character in the player's color. View only.
    /// </summary>
    public class PlayerColorView : QuantumEntityViewComponent
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("The player colors.")]
        [SerializeField] private PlayerPalette _palette;
        [Tooltip("The renderers to tint.")]
        [SerializeField] private Renderer[] _renderers;

        public override void OnActivate(Frame frame)
        {
            if (_palette == null)
                return;

            var properties = new MaterialPropertyBlock();
            properties.SetColor(BaseColorId, _palette.GetColor((int)frame.Get<Player>(EntityRef).Ref));
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null)
                    renderer.SetPropertyBlock(properties);
            }
        }
    }
}
