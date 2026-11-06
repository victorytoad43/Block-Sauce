using Quantum;

namespace BlockSauce
{
    /// <summary>
    /// Dev builds and the editor only: what a bot is under the hood (its personality and skill), for the spectator label
    /// and the room screen's bot rows. Players never see personalities (docs/PLAN.md, Bot Enhancement); in a release
    /// build every method returns an empty string.
    /// </summary>
    public static class BotDevInfo
    {
        /// <summary>
        /// " · Wrecker, Hard" for a bot in <paramref name="slot"/>, or empty (not a bot, or a release build).
        /// </summary>
        public static string Describe(PlayerRef slot, bool withSkill = true)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Frame frame = QuantumRunner.Default?.Game?.Frames.Verified;
            if (frame == null || slot.IsValid == false || (int)slot >= frame.MaxPlayerCount
                || PlayerSystem.TryGetCharacter(frame, slot, out EntityRef character) == false
                || frame.TryGet(character, out Bot bot) == false)
                return string.Empty;

            return withSkill ? $" · {bot.Personality}, {bot.Skill}" : $" · {bot.Personality}";
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// The bot's personality as a number for change checks, or -1.
        /// </summary>
        public static int PersonalityKey(Frame frame, PlayerRef slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (PlayerSystem.TryGetCharacter(frame, slot, out EntityRef character) && frame.TryGet(character, out Bot bot))
                return (int)bot.Personality;
#endif
            return -1;
        }
    }
}
