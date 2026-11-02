using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// How each sauce looks (A-009 placeholder): its coat color and its particle cue. Player colors own hue, so the
    /// sauces differ in value and in their cue: Stack sauce is dark with rising bubbles, Slip sauce pale with glints.
    /// View only.
    /// </summary>
    [CreateAssetMenu(menuName = "Block Sauce/Sauce Looks")]
    public class SauceLooks : ScriptableObject
    {
        [Tooltip("Stack sauce: coat color, and the color of its bubbles.")]
        [SerializeField] private Color _stackColor = new Color(0.23f, 0.11f, 0.05f);
        [SerializeField] private Color _stackParticleColor = new Color(0.55f, 0.32f, 0.16f);
        [Tooltip("Slip sauce: coat color, and the color of its glints.")]
        [SerializeField] private Color _slipColor = new Color(0.86f, 0.9f, 0.92f);
        [SerializeField] private Color _slipParticleColor = new Color(1f, 1f, 1f);
        [Tooltip("Material for the sauce particles (bubbles, glints, splashes, the Stack pulse).")]
        [SerializeField] private Material _particleMaterial;

        public Material ParticleMaterial => _particleMaterial;

        public Color GetColor(SauceType type)
        {
            return type == SauceType.Slip ? _slipColor : _stackColor;
        }

        public Color GetParticleColor(SauceType type)
        {
            return type == SauceType.Slip ? _slipParticleColor : _stackParticleColor;
        }
    }
}
