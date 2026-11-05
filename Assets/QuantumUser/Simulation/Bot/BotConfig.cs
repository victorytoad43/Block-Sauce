using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// Bot tuning (docs/PLAN.md, Phase 6): the character a bot plays and how its hands move. Referenced by the game mode.
    /// </summary>
    public class BotConfig : AssetObject
    {
        /// <summary>
        /// The character prototype a bot spawns as; players get theirs from their RuntimePlayer, which bots don't have.
        /// </summary>
        public AssetRef<EntityPrototype> Avatar;

        /// <summary>
        /// The bot's brain: a Bot SDK state machine (built by Block Sauce › Build Bot Brain).
        /// </summary>
        public AssetRef<HFSMRoot> Brain;

        /// <summary>
        /// The chance, 0 to 1, that a bot dunks a block of its own in Stack sauce before building with it.
        /// </summary>
        public FP DunkChance = FP._0_25;

        /// <summary>
        /// Seconds any task may take before it fails.
        /// </summary>
        [Header("Tasks")]
        public FP TaskSeconds = 15;

        /// <summary>
        /// Seconds a bot hesitates before starting each task, picked at random between the two.
        /// </summary>
        public FP ThinkSecondsMin = (FP)3 / 10;
        public FP ThinkSecondsMax = 1;

        /// <summary>
        /// Seconds a bot leaves alone a block it failed to fetch before trying it again.
        /// </summary>
        public FP UnreachableSeconds = 20;

        /// <summary>
        /// Distance, in meters, beyond which a bot with empty hands runs.
        /// </summary>
        [Header("Walking")]
        public FP RunDistance = 5;
        /// <summary>
        /// How far ahead, in meters, a bot looks for something in its way, and the angles it tries around it.
        /// </summary>
        public FP ProbeLength = (FP)3 / 2;
        public FP ProbeStepDegrees = 30;
        /// <summary>
        /// How far ahead, in meters, a bot checks for a ledge too high to step onto (a pillar, the inner ring); it jumps
        /// when one is this close and there's room above it.
        /// </summary>
        public FP LedgeProbeLength = (FP)8 / 10;
        /// <summary>
        /// A bot that wants to move but has gone less than StuckDistance in StuckSeconds walks off at an angle for
        /// DetourSeconds.
        /// </summary>
        public FP StuckSeconds = 1;
        public FP StuckDistance = (FP)1 / 4;
        public FP DetourSeconds = (FP)7 / 10;

        /// <summary>
        /// The most the swing input moves, in degrees per tick; a bot's hands move about as fast as a quick stick.
        /// </summary>
        [Header("Hands")]
        public FP MaxSwingStep = 6;
        /// <summary>
        /// How high, in degrees, a bot holds a block while carrying it.
        /// </summary>
        public FP CarryPitch = 20;
        /// <summary>
        /// Distance, in meters, from a block at which a bot stops walking and turns to grab it.
        /// </summary>
        public FP GrabDistance = (FP)5 / 4;
        /// <summary>
        /// Grab presses a bot tries before giving up on a block.
        /// </summary>
        public int GrabAttempts = 6;

        /// <summary>
        /// Placing on a low tower: the bot holds the block at the lowest pitch that keeps its bottom PlaceClearance
        /// (meters) above the top, which reaches furthest out, and walks up to PlaceStandDistance from the tower's center
        /// (closer shoves it). A tower whose top is above PlaceJumpAbove can't be cleared from the ground: the bot raises
        /// the block as high as it goes (close to the body, clear of the tower), lines up at PlaceLineUpDistance and jumps
        /// straight up; once the block is above the top it lowers it to PlaceJumpReachPitch, reaching out over the tower,
        /// and edges in at PlaceJumpMove of full speed to PlaceJumpStandDistance, no closer. It lets go when the block is
        /// within PlaceOffset (horizontally) of the top block's center, PlaceJumpOffset in a jump, and tries
        /// PlaceAttempts jumps.
        /// </summary>
        [Header("Placing")]
        public FP PlaceClearance = (FP)8 / 100;
        public FP PlaceStandDistance = (FP)95 / 100;
        public FP PlaceJumpAbove = 1;
        public FP PlaceLineUpDistance = (FP)13 / 10;
        public FP PlaceJumpReachPitch = 30;
        public FP PlaceJumpMove = (FP)1 / 2;
        public FP PlaceJumpStandDistance = (FP)115 / 100;
        public FP PlaceOffset = (FP)3 / 10;
        public FP PlaceJumpOffset = (FP)35 / 100;
        public int PlaceAttempts = 3;

        /// <summary>
        /// Throwing: the distances, in meters, a bot throws from, and how close its aim must be, in degrees, to let go.
        /// </summary>
        [Header("Throwing")]
        public FP ThrowMinRange = 3;
        /// <summary>
        /// The counted blocks a rival's tower needs before bots raid it.
        /// </summary>
        public int RaidAtCount = 2;
        public FP ThrowMaxRange = 8;
        public FP AimTolerance = 2;
        public FP MinAimSeconds = (FP)1 / 2;

        /// <summary>
        /// Dunking: how far from a vat's center, in meters, a bot raises the block before walking in, and how long it
        /// lowers, soaks and lifts.
        /// </summary>
        [Header("Dunking")]
        public FP DunkApproachDistance = 3;
        public FP DunkLowerSeconds = 2;
        public FP DunkSoakSeconds = 1;
        public FP DunkLiftSeconds = (FP)3 / 2;
    }
}
