using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Adds the game's spawn settings to the runtime config.
    /// </summary>
    public partial class RuntimeConfig
    {
        /// <summary>
        /// How far above its spawn point, in meters, a joining player's character appears.
        /// </summary>
        public FP PlayerSpawnHeightOffset = 1;
    }
}
