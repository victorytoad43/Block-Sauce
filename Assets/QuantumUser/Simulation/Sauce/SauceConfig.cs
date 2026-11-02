using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// Sauce tuning (docs/PLAN.md, Sauce system): how deep a dip has to be for full strength, what Stack sauce does to
    /// rivals near a stacked block, and the physics materials Slip sauce swaps in. Referenced by the game mode, so a
    /// mode can tune or drop sauces.
    /// </summary>
    public class SauceConfig : AssetObject
    {
        /// <summary>
        /// The dipped fraction (share of the block's height under the sauce) that gives full strength; shallower dips
        /// scale down from it.
        /// </summary>
        [Header("Dipping")]
        public FP FullStrengthFraction = FP._0_50;
        /// <summary>
        /// Dips shallower than this fraction leave no coat: a graze on the surface doesn't count.
        /// </summary>
        public FP MinDipFraction = (FP)5 / 100;
        /// <summary>
        /// Seconds a block must stay out of the sauce before its dip ends; going back in sooner continues the same dip.
        /// </summary>
        public FP DipEndSeconds = FP._0_25;

        /// <summary>
        /// How far from a stacked Stack-sauced block, in meters, rivals are shoved away.
        /// </summary>
        [Header("Stack sauce")]
        public FP StackRadius = (FP)25 / 10;
        /// <summary>
        /// The speed change, in m/s, the shove gives a rival at the lowest strength and at full strength. 2.5 staggers
        /// and 6 knocks down (KnockbackConfig).
        /// </summary>
        public FP StackMinSpeedChange = 3;
        public FP StackMaxSpeedChange = (FP)55 / 10;
        /// <summary>
        /// How much the shove lifts, as a share of its horizontal push.
        /// </summary>
        public FP StackUpBias = (FP)3 / 10;
        /// <summary>
        /// Seconds after a shove before the same block shoves again.
        /// </summary>
        public FP StackCooldownSeconds = 2;

        /// <summary>
        /// The materials a Slip-sauced block takes, from weakest to full strength: the strength picks one in equal steps.
        /// </summary>
        [Header("Slip sauce")]
        public AssetRef<PhysicsMaterial>[] SlipMaterials;
    }
}
