using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// One bot personality (docs/PLAN.md, Bot Enhancement): what a bot prefers to do, never shown to players.
    /// <see cref="BotConfig.Personalities"/> lists one per <see cref="BotPersonalityType"/>; each bot rolls one when it
    /// is added (<see cref="Bot.Personality"/>). <see cref="BotChoices"/> scores a bot's options with these weights. Skill
    /// (<see cref="BotSkill"/>) sets how well it plays; personality sets what it goes for.
    /// </summary>
    public class BotPersonality : AssetObject
    {
        /// <summary>
        /// With empty hands, each option scores its weight times how much the situation calls for it, and the highest
        /// wins, after a random 0 to 0.4 is added to each: Build (fetch one of its own loose blocks, always 1), Raid (fetch
        /// a rival's block to throw at the leading rival's tower: the rival's counted blocks over one less than a finished
        /// tower), ClearTop (take a rival's block off the top of its own tower, in reach: always 1.5).
        /// </summary>
        [Header("Choices")]
        public FP BuildWeight = 1;
        public FP RaidWeight = FP._0_20;
        public FP ClearTopWeight = 1;

        /// <summary>
        /// Raids a rival tower from this many counted blocks; 0 uses the skill's RaidAtCount. With
        /// <see cref="RaidOnlyNearWin"/>, only a rival one block from a finished tower is raided.
        /// </summary>
        public int RaidAtCount;
        public bool RaidOnlyNearWin;

        /// <summary>
        /// The chance, 0 to 1, a rival's block in hand is thrown at a rival carrying a block in throwing range, rather than
        /// at the leading tower.
        /// </summary>
        public FP ThrowAtCarrierChance;

        /// <summary>
        /// The bot config's dunk chance is scaled by this.
        /// </summary>
        public FP DunkChanceScale = 1;

        /// <summary>
        /// Slide tackles: which carriers it goes for (0 none, 1 those carrying its own blocks, 2 anyone carrying a block);
        /// whether it tackles rivals building at their own towers; how close to its own tower, in meters, a rival is
        /// tackled (carrying anything, or with <see cref="GuardEmptyHanded"/>, anyone); how long it waits between
        /// tackles, as a share of the bot config's cooldown; and the chance, 0 to 1, it takes a tackle it could make (a
        /// tackle passed up waits the cooldown too).
        /// </summary>
        [Header("Tackling")]
        public FP TackleChance = 1;
        public int TackleCarriers = 1;
        public bool TackleBuilders;
        public FP GuardRadius = 3;
        public bool GuardEmptyHanded;
        public FP TackleCooldownScale = 1;

        /// <summary>
        /// Mistakes on purpose (Klutzes), on top of the skill's: chances per second of running to trip (fall flat, a
        /// knockdown at the run's speed) or slide without looking (into walls, off ledges), per second of carrying to drop
        /// the block, per placing to let go up to OffCenterError meters off, and per throw to throw up to WildThrowError
        /// degrees off (yaw and pitch).
        /// </summary>
        [Header("Mistakes")]
        public FP TripChance;
        public FP WildSlideChance;
        public FP DropChance;
        public FP OffCenterChance;
        public FP OffCenterError = (FP)45 / 100;
        public FP WildThrowChance;
        public FP WildThrowError = 12;
    }
}
