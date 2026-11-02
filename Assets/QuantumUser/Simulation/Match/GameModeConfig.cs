using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// A game mode: the numbers a match is played with plus rule hooks. The match and stack systems call the hooks
    /// at named moments (match start, block counted, block uncounted, win check), so a new mode is a subclass that
    /// overrides hooks rather than a fork of the core systems. The base class plays Stack &amp; Lock.
    /// </summary>
    public class GameModeConfig : AssetObject
    {
        /// <summary>
        /// The block every player is given at match start.
        /// </summary>
        [Header("Blocks")]
        public AssetRef<EntityPrototype> BlockPrototype;
        /// <summary>
        /// The sauce rules blocks are dipped under; none turns sauces off.
        /// </summary>
        public AssetRef<SauceConfig> Sauce;
        /// <summary>
        /// How many blocks each player gets.
        /// </summary>
        public int BlocksPerPlayer = 3;
        /// <summary>
        /// How many counted blocks, stacked on one base, make a finished tower.
        /// </summary>
        public int TowerHeight = 3;
        /// <summary>
        /// The edge length of a block, in meters, used to check that a block sits squarely on the one below.
        /// </summary>
        public FP BlockSize = (FP)8 / 10;

        /// <summary>
        /// Seconds a stacked block must rest undisturbed before it counts.
        /// </summary>
        [Header("Stacking")]
        public FP SettleSeconds = 3;
        /// <summary>
        /// A block counts as resting below this speed, in m/s.
        /// </summary>
        public FP RestSpeed = (FP)3 / 10;
        /// <summary>
        /// A block counts as resting below this spin, in radians per second.
        /// </summary>
        public FP RestAngularSpeed = 1;
        /// <summary>
        /// How far, in degrees, a face of the block may tilt from level and still be stacked.
        /// </summary>
        public FP MaxTiltDegrees = 12;
        /// <summary>
        /// How far, as a share of the block size, a block's center may sit off the center of the block below.
        /// </summary>
        public FP MaxStackOffset = FP._0_50;
        /// <summary>
        /// The speed into a settling block, in m/s, from which a touch counts as a hit and restarts its settling.
        /// </summary>
        public FP SettleResetSpeed = 1;

        /// <summary>
        /// Seconds of countdown before play starts. Players can look around but not move.
        /// </summary>
        [Header("Match flow")]
        public FP CountdownSeconds = 3;
        /// <summary>
        /// Seconds the round end (winner announcement) lasts before the results.
        /// </summary>
        public FP RoundEndSeconds = 3;
        /// <summary>
        /// Seconds the results stay up before everyone returns to the lobby.
        /// </summary>
        public FP ResultsSeconds = 6;
        /// <summary>
        /// The fewest players a match can start with.
        /// </summary>
        public int MinPlayers = 1;

        /// <summary>
        /// Match spawns are the lobby spawn points pushed out from the center by this factor, so towers get room.
        /// </summary>
        [Header("Match spawns")]
        public FP SpawnRadiusScale = (FP)18 / 10;
        /// <summary>
        /// How far in front of a player, in meters, their blocks appear at match start.
        /// </summary>
        public FP BlockSpawnDistance = (FP)18 / 10;
        /// <summary>
        /// The gap between a player's starting blocks, center to center, in meters.
        /// </summary>
        public FP BlockSpawnSpacing = (FP)12 / 10;

        /// <summary>
        /// Called once the match has started. Stack &amp; Lock does nothing extra.
        /// </summary>
        public virtual void OnMatchStarted(Frame frame) { }

        /// <summary>
        /// Called when a stacked block has settled and starts counting.
        /// </summary>
        public virtual void OnBlockCounted(Frame frame, EntityRef block) { }

        /// <summary>
        /// Called when a counted block stops being stacked (knocked off, grabbed, tipped over).
        /// </summary>
        public virtual void OnBlockUncounted(Frame frame, EntityRef block) { }

        /// <summary>
        /// Returns whether <paramref name="block"/> may count at <paramref name="level"/> of its owner's stack.
        /// Stack &amp; Lock counts every level the stack system found.
        /// </summary>
        public virtual bool CanCount(Frame frame, EntityRef block, int level) => true;

        /// <summary>
        /// Returns the winning player, or none. Stack &amp; Lock: the first player whose best stack holds a full tower of
        /// counted blocks.
        /// </summary>
        public virtual unsafe PlayerRef CheckWinner(Frame frame)
        {
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                if (BlockTower.CountBest(frame, player) >= TowerHeight)
                    return player;
            }

            return PlayerRef.None;
        }
    }
}
