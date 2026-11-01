using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// How hard a character gets knocked by hits, and when a hit staggers it.
    /// Hit strength is the speed change a hit gives the character, in m/s, so it is independent of the character's mass.
    /// </summary>
    public class KnockbackConfig : AssetObject
    {
        /// <summary>
        /// The speed toward the character, in m/s, a block or other object must be moving at to count as a hit.
        /// </summary>
        [Header("Hits")]
        public FP MinHitSpeed = 2;
        /// <summary>
        /// The speed, in m/s, another character must be running into this one at to count as a body check.
        /// Higher than MinHitSpeed so walking into someone only pushes them.
        /// </summary>
        public FP MinBodyCheckSpeed = (FP)45 / 10;
        /// <summary>
        /// Scales a hit's speed change on top of what the physics contact already does, for a party-game punch.
        /// </summary>
        public FP HitScale = (FP)3 / 2;
        /// <summary>
        /// The share of a hit's horizontal speed change added upward, so hits lift the character off its feet.
        /// </summary>
        public FP UpBias = FP._0_25;
        /// <summary>
        /// The largest speed change, in m/s, a single hit can give.
        /// </summary>
        public FP MaxHitSpeedChange = 9;

        /// <summary>
        /// The speed change, in m/s, from which a hit staggers the character.
        /// </summary>
        [Header("Stagger")]
        public FP StaggerSpeedChange = (FP)5 / 2;
        /// <summary>
        /// Seconds of stagger per m/s of speed change.
        /// </summary>
        public FP StaggerSecondsPerSpeed = (FP)12 / 100;
        /// <summary>
        /// The shortest stagger, in seconds.
        /// </summary>
        public FP MinStaggerSeconds = FP._0_25;
        /// <summary>
        /// The longest stagger, in seconds.
        /// </summary>
        public FP MaxStaggerSeconds = (FP)12 / 10;

        /// <summary>
        /// The speed change, in m/s, from which a hit knocks the character down into a ragdoll instead of staggering it:
        /// a full-speed throw or jumpshot to the body, a hard swing, a fast-moving block.
        /// </summary>
        [Header("Knockdown")]
        public FP KnockdownSpeedChange = 6;
        /// <summary>
        /// A running body check on a character that is already staggered knocks it down, whatever the hit's size.
        /// </summary>
        public bool BodyCheckKnocksDownStaggered = true;
    }
}
