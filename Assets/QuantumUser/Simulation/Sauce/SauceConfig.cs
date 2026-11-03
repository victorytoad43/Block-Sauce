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
        /// A Stack-sauced block glues itself to any block it touches while the two move slower than this against each
        /// other, in m/s, so a placed block sticks but one bouncing off doesn't.
        /// </summary>
        public FP GlueMaxSpeed = FP._0_50;
        /// <summary>
        /// How close, in meters, another block's surface must be to count as touching.
        /// </summary>
        public FP GlueReach = (FP)3 / 100;
        /// <summary>
        /// How far, in degrees, glued blocks can bend against each other.
        /// </summary>
        public FP GlueAngleDegrees = 2;
        /// <summary>
        /// The speed into a contact, in m/s, of a hit that tears a glued block free, at the lowest strength and at full
        /// strength. Running into a tower is about 5; a throw, jumpshot or flying body more. Grabbing a block frees it too.
        /// </summary>
        public FP GlueMinBreakSpeed = 3;
        public FP GlueMaxBreakSpeed = 7;
        /// <summary>
        /// How far apart, in meters, glued points can be pulled (by a teleport, or forces the joint can't hold) before
        /// the glue tears.
        /// </summary>
        public FP GlueTearDistance = FP._0_10;

        /// <summary>
        /// The materials a Slip-sauced block takes, from weakest to full strength: the strength picks one in equal steps.
        /// </summary>
        [Header("Slip sauce")]
        public AssetRef<PhysicsMaterial>[] SlipMaterials;
    }
}
