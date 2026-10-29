using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// The player colors (A-001): one per player slot, with a darker shade for the lock dither. View only; the
    /// simulation knows players by index and never sees a color.
    /// </summary>
    [CreateAssetMenu(menuName = "Block Sauce/Player Palette")]
    public class PlayerPalette : ScriptableObject
    {
        [Tooltip("One color per player slot, in player index order.")]
        [SerializeField] private Color[] _colors =
        {
            new Color(0.90f, 0.30f, 0.25f), new Color(0.25f, 0.55f, 0.95f), new Color(0.95f, 0.80f, 0.20f),
            new Color(0.35f, 0.80f, 0.35f), new Color(0.70f, 0.40f, 0.90f), new Color(1.00f, 0.55f, 0.15f),
            new Color(0.25f, 0.85f, 0.85f), new Color(0.95f, 0.45f, 0.75f),
        };
        [Tooltip("Color of blocks nobody owns.")]
        [SerializeField] private Color _neutral = new Color(0.6f, 0.6f, 0.6f);
        [Tooltip("How much darker the lock shade is than the base color (0 = same, 1 = black).")]
        [SerializeField, Range(0f, 1f)] private float _lockDarken = 0.55f;

        /// <summary>
        /// Returns the player's color, or the neutral color for an index outside the palette (no owner).
        /// </summary>
        public Color GetColor(int playerIndex)
        {
            return playerIndex >= 0 && playerIndex < _colors.Length ? _colors[playerIndex] : _neutral;
        }

        /// <summary>
        /// Returns the darker shade used by the lock dither.
        /// </summary>
        public Color GetLockColor(int playerIndex)
        {
            return Color.Lerp(GetColor(playerIndex), Color.black, _lockDarken);
        }
    }
}
