using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// One bot difficulty (docs/PLAN.md, Bot Enhancement): how fast a bot reacts and how cleanly it throws, places and
    /// tackles. <see cref="BotConfig.Skills"/> lists one per <see cref="BotSkillLevel"/>; each bot plays at its own
    /// level (<see cref="Bot.Skill"/>). Normal is the Phase 6 bot.
    /// </summary>
    public class BotSkill : AssetObject
    {
        /// <summary>
        /// Seconds a bot hesitates before starting each task, picked at random between the two.
        /// </summary>
        [Header("Reacting")]
        public FP ThinkSecondsMin = (FP)3 / 10;
        public FP ThinkSecondsMax = 1;

        /// <summary>
        /// The counted blocks a rival's tower needs before the bot raids it.
        /// </summary>
        public int RaidAtCount = 2;

        /// <summary>
        /// Whether the bot runs while carrying a block (every bot runs with empty hands).
        /// </summary>
        [Header("Hands")]
        public bool RunWhileCarrying;

        /// <summary>
        /// Grab presses tried before giving up on a block, and jumps tried before giving up on placing on a tall tower.
        /// </summary>
        public int GrabAttempts = 6;
        public int PlaceAttempts = 3;

        /// <summary>
        /// Placing: the bot lets go over a point this far, in meters at most, from the top block's center, picked at
        /// random for each placing.
        /// </summary>
        public FP PlaceError;

        /// <summary>
        /// Throwing: how close the aim must be, in degrees, to let go, the shortest aim, in seconds, and an error, in
        /// degrees at most, added to the aim's yaw and pitch, picked at random for each throw.
        /// </summary>
        [Header("Throwing")]
        public FP AimTolerance = 2;
        public FP MinAimSeconds = (FP)1 / 2;
        public FP AimError;

        /// <summary>
        /// Slide tackles: how far out, in meters, the bot slides, how well its run must line up, in degrees, and how much
        /// it leads a moving rival (0 slides at where the rival is, 1 at where it will be).
        /// </summary>
        [Header("Tackling")]
        public FP TackleSlideDistance = (FP)7 / 2;
        public FP TackleAlignDegrees = 20;
        public FP TackleLead = 1;
    }
}
