using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Bot tuning (docs/PLAN.md, Phase 6): the character a bot plays and how it behaves. Referenced by the game mode.
    /// </summary>
    public class BotConfig : AssetObject
    {
        /// <summary>
        /// The character prototype a bot spawns as; players get theirs from their RuntimePlayer, which bots don't have.
        /// </summary>
        public AssetRef<EntityPrototype> Avatar;

        /// <summary>
        /// How fast, in degrees per second, a bot with nothing to do turns while it wanders.
        /// </summary>
        public FP WanderTurnSpeed = 45;
    }
}
