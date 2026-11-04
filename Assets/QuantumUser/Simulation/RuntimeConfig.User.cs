using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Adds the game's spawn and fall settings, and the menu's demo match, to the runtime config.
    /// </summary>
    public partial class RuntimeConfig
    {
        /// <summary>
        /// How far above its spawn point, in meters, a joining player's character appears.
        /// </summary>
        public FP PlayerSpawnHeightOffset = 1;

        /// <summary>
        /// The height, in meters, below which a character or ragdoll limb counts as fallen out of the arena and is put
        /// back on its last safe ground.
        /// </summary>
        public FP FallThresholdY = -15;

        /// <summary>
        /// The menu's background match (docs/PLAN.md, Phase 6): an offline game with nobody playing, where bots fill
        /// every slot and each match starts by itself from the lobby, over and over.
        /// </summary>
        public bool DemoMatch;
    }
}
