using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// Sauce tuning (docs/PLAN.md, Sauce system): how deep a dip has to be for full strength, and the physics materials
    /// Stack and Slip sauce swap in, sticky and slippery. Referenced by the game mode, so a mode can tune or drop sauces.
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
        /// The materials a Stack-sauced block takes, from weakest to full strength: the strength picks one in equal steps.
        /// </summary>
        [Header("Stack sauce")]
        public AssetRef<PhysicsMaterial>[] StackMaterials;

        /// <summary>
        /// The materials a Slip-sauced block takes, from weakest to full strength: the strength picks one in equal steps.
        /// </summary>
        [Header("Slip sauce")]
        public AssetRef<PhysicsMaterial>[] SlipMaterials;
    }
}
