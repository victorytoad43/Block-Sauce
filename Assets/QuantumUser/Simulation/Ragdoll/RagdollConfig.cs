using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// The knockdown tuning: the ragdoll body spawned in the character's place, how long it stays down, struggling
    /// free, whether bodies can be carried, and how the character finds its feet again. Adapted from Friendslop's
    /// RagdollConfig. What knocks a character down lives in KnockbackConfig, since every hit goes through knockback.
    /// </summary>
    public class RagdollConfig : AssetObject
    {
        /// <summary>
        /// The ragdoll spawned in the character's place: a root at the feet owning jointed limbs through an entity group.
        /// </summary>
        [Header("Body")]
        public AssetRef<EntityPrototype> RagdollPrototype;
        /// <summary>
        /// The angular speed, in radians per second, each limb starts tumbling at, so the limbs pull against each other
        /// and the body collapses instead of falling stiff.
        /// </summary>
        public FP SpinImpulse = 4;

        /// <summary>
        /// Seconds a knocked-down character stays down.
        /// </summary>
        [Header("Knockdown")]
        public FP Duration = (FP)5 / 2;
        /// <summary>
        /// Seconds each Jump press takes off the knockdown; zero disables struggling.
        /// </summary>
        public FP StruggleRecovery = (FP)3 / 10;
        /// <summary>
        /// Seconds after a struggle before another press counts, so mashing has a ceiling; zero accepts every press.
        /// </summary>
        public FP StruggleCooldown = (FP)1 / 10;
        /// <summary>
        /// The least time, in seconds, a struggle leaves on the knockdown.
        /// </summary>
        public FP MinStruggleRemainder = (FP)1 / 10;

        /// <summary>
        /// Lets players grab and carry a downed body by any limb. Off: bodies can only be pushed and knocked around.
        /// </summary>
        [Header("Carrying bodies")]
        public bool AllowGrabbingBodies = true;
        /// <summary>
        /// Seconds a carried body stays down after it is let go. While carried, the knockdown does not run down.
        /// </summary>
        public FP CarriedReleaseDelay = 1;
        /// <summary>
        /// Seconds a body can be carried before it breaks free and is dropped. Jump presses take the struggle recovery
        /// off it, as they do off the knockdown. It runs once per knockdown: a body that has broken free can't be
        /// picked up again until it has got up.
        /// </summary>
        public FP CarryHoldSeconds = (FP)5 / 2;

        /// <summary>
        /// The speed, in m/s, every limb must be under for the body to count as still.
        /// </summary>
        [Header("Resting")]
        public FP RestSpeed = (FP)15 / 100;
        /// <summary>
        /// The angular speed, in radians per second, every limb must be under for the body to count as still.
        /// </summary>
        public FP RestAngularSpeed = FP._0_50;
        /// <summary>
        /// Seconds the body must stay still before its limbs are put to sleep, so a resting pile costs nothing.
        /// </summary>
        public FP RestSeconds = FP._0_25;

        /// <summary>
        /// How far below the hips, in meters, the get-up looks for ground to stand on.
        /// </summary>
        [Header("Getting up")]
        public FP GroundProbeDistance = 3;
        /// <summary>
        /// How far ahead of the hips, toward the head, the character's feet land when it gets up. Matches the get-up
        /// clips, whose first frame lies with the hips this far behind the feet, so the view's blend starts where the
        /// body lay.
        /// </summary>
        public FP GetUpForwardOffset = (FP)37 / 100;
        /// <summary>
        /// The radii, in meters, of the rings searched for a clear spot when the hips lie under a block or a wall.
        /// </summary>
        public FP[] ClearSpotRadii = { FP._0_50, 1, (FP)3 / 2 };
        /// <summary>
        /// The rotation axes frozen on the character's body when it gets back up, keeping it upright.
        /// </summary>
        public RotationFreezeFlags UprightFreeze = RotationFreezeFlags.FreezeX | RotationFreezeFlags.FreezeZ;
    }
}
