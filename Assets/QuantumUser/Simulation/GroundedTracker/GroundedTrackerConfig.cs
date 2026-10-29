using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// The grounded tracker tuning for what counts as ground and where a respawn point is safe to bank.
    /// </summary>
    public class GroundedTrackerConfig : AssetObject
    {
        /// <summary>
        /// The minimum contact normal Y that counts as ground rather than wall; 0.5 admits slopes up to 60 degrees.
        /// </summary>
        public FP GroundNormalThreshold = FP._0_50;

        /// <summary>
        /// Ticks without a ground contact tolerated before the entity counts as airborne.
        /// A resting body touches the floor only on alternating ticks, so this keeps the grounded state from flickering.
        /// </summary>
        public int GroundContactGraceTicks = 3;

        /// <summary>
        /// How far, in meters, the edge probes sit around a candidate respawn point; 0 skips the check.
        /// Keep it just outside the entity's footprint, or narrow ground never passes.
        /// </summary>
        public FP BorderCheckRadius = FP._0_50;

        /// <summary>
        /// The length, in meters, of each edge probe, cast down from 1 meter above the candidate point.
        /// Tuned per prefab, since it depends on where the entity's origin sits; a player's is at its feet.
        /// </summary>
        public FP EdgeProbeLength = 2;

        /// <summary>
        /// How far, in meters, the entity must travel horizontally from the banked point before a new one is banked.
        /// </summary>
        public FP MinRebankDistance = 2;

        /// <summary>
        /// If true, draws the edge probes as debug rays, green over ground and red over a drop.
        /// </summary>
        public bool DrawRays;
    }
}
