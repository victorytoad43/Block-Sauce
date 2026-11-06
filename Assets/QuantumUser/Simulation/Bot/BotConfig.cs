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
        /// Test only (editor soaks): the level each player slot's bot plays at, overriding its own; null for none. Never
        /// set in a match that's online, since every client must agree.
        /// </summary>
        public static BotSkillLevel[] TestSkillBySlot;

        /// <summary>
        /// Test only (editor soaks): the personality each player slot's bot has, overriding its own; null for none. Never
        /// set in a match that's online.
        /// </summary>
        public static BotPersonalityType[] TestPersonalityBySlot;

        /// <summary>
        /// Returns the bot's difficulty: the skill for its level, or the first one listed if that level has none.
        /// </summary>
        public unsafe BotSkill GetSkill(Frame frame, Bot* bot)
        {
            int level = (int)bot->Skill;
            if (Skills == null || Skills.Length == 0)
                return null;

            return frame.FindAsset(Skills[level < Skills.Length ? level : 0]);
        }

        /// <summary>
        /// Returns the bot's personality asset, or the first one listed if its type has none.
        /// </summary>
        public unsafe BotPersonality GetPersonality(Frame frame, Bot* bot)
        {
            int type = (int)bot->Personality;
            if (Personalities == null || Personalities.Length == 0)
                return null;

            return frame.FindAsset(Personalities[type < Personalities.Length ? type : 0]);
        }

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
        /// The difficulty levels, one per <see cref="BotSkillLevel"/> in order (Easy, Normal, Hard, Expert).
        /// </summary>
        public AssetRef<BotSkill>[] Skills;

        /// <summary>
        /// The personalities, one per <see cref="BotPersonalityType"/> in order (Wrecker, Builder, Klutz).
        /// </summary>
        public AssetRef<BotPersonality>[] Personalities;

        /// <summary>
        /// Seconds any task may take before it fails.
        /// </summary>
        [Header("Tasks")]
        public FP TaskSeconds = 15;

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
        /// Placing on a low tower: the bot holds the block at the lowest pitch that keeps its bottom PlaceClearance
        /// (meters) above the top, which reaches furthest out, and walks up to PlaceStandDistance from the tower's center
        /// (closer shoves it). A tower whose top is above PlaceJumpAbove can't be cleared from the ground: the bot raises
        /// the block as high as it goes (close to the body, clear of the tower), lines up at PlaceLineUpDistance and jumps
        /// straight up; once the block is above the top it lowers it to PlaceJumpReachPitch, reaching out over the tower,
        /// and edges in at PlaceJumpMove of full speed to PlaceJumpStandDistance, no closer. It lets go when the block is
        /// within PlaceOffset (horizontally) of the top block's center, PlaceJumpOffset in a jump (the skill's
        /// PlaceError moves where it aims).
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

        /// <summary>
        /// Throwing: the distances, in meters, a bot throws from (how well it aims is on its skill).
        /// </summary>
        [Header("Throwing")]
        public FP ThrowMinRange = 3;
        public FP ThrowMaxRange = 8;

        /// <summary>
        /// Dunking: how far from a vat's center, in meters, a bot raises the block before walking in, and how long it
        /// lowers, soaks and lifts.
        /// </summary>
        [Header("Dunking")]
        public FP DunkApproachDistance = 3;
        public FP DunkLowerSeconds = 2;
        public FP DunkSoakSeconds = 1;
        public FP DunkLiftSeconds = (FP)3 / 2;

        /// <summary>
        /// Slide tackles, with empty hands: a rival within TackleRange (meters) worth tackling (its personality says who)
        /// gets run at and slid into (how far out and how well lined up is on its skill). The tackle gives up after
        /// TackleSeconds; the next waits TackleCooldownSeconds (scaled by its personality).
        /// </summary>
        [Header("Tackling")]
        public FP TackleRange = 12;
        /// <summary>The slowest run, in m/s, a bot slides from (the slide needs 4).</summary>
        public FP TackleMinSpeed = 5;
        /// <summary>How long after starting the slide, in seconds, a tackle that hasn't hit counts as a miss.</summary>
        public FP TackleSlideSeconds = (FP)6 / 5;
        public FP TackleSeconds = 6;
        public FP TackleCooldownSeconds = 5;

        /// <summary>
        /// Travel slides: running with empty hands toward something more than TravelSlideMinDistance away, with nothing
        /// in the way for TravelSlideClearance, a bot slides with this chance per second (at most once per
        /// TravelSlideCooldownSeconds).
        /// </summary>
        [Header("Travel slides")]
        public FP TravelSlideChance = (FP)15 / 100;
        public FP TravelSlideMinDistance = 10;
        public FP TravelSlideClearance = 6;
        public FP TravelSlideCooldownSeconds = 4;
    }
}
