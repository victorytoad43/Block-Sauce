using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// Which clip plays for each game moment (<see cref="GameAudio"/>). The placeholder bank (<c>PH_SoundBank</c>) holds
    /// sounds from a CC0 pack, picked by name and unheard (A-014); real sounds replace them here.
    /// </summary>
    [CreateAssetMenu(menuName = "Block Sauce/Sound Bank")]
    public class SoundBank : ScriptableObject
    {
        [Header("Characters")]
        public AudioClip Jump;
        public AudioClip Knockdown;
        public AudioClip FallRespawn;

        [Header("Blocks")]
        public AudioClip Grab;
        public AudioClip Place;
        public AudioClip Drop;
        public AudioClip Throw;
        [Tooltip("A block or body hitting something; louder the harder the hit.")]
        public AudioClip Impact;
        public AudioClip SettleStarted;
        public AudioClip Counted;
        public AudioClip Uncounted;

        [Header("Match")]
        public AudioClip Countdown;
        public AudioClip MatchStart;
        public AudioClip RoundEnd;
        public AudioClip Results;

        [Header("Interface")]
        public AudioClip Click;

        [Header("Music")]
        public AudioClip MenuMusic;
        public AudioClip MatchMusic;
        [Range(0f, 1f)] public float MusicVolume = 0.25f;
    }
}
