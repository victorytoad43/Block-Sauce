using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Sent from the room screen (the host's) to add a bot in the lobby. Ignored outside the lobby or with no free slot.
    /// </summary>
    public class CommandAddBot : DeterministicCommand
    {
        public override void Serialize(BitStream stream)
        {
        }
    }

    /// <summary>
    /// Sent from the room screen (the host's) to remove a bot in the lobby: the one in the highest slot.
    /// </summary>
    public class CommandRemoveBot : DeterministicCommand
    {
        public override void Serialize(BitStream stream)
        {
        }
    }

    /// <summary>
    /// Sent from the room screen (the host's) to set the level the bot in <see cref="Slot"/> plays at; new bots (and
    /// fill bots) take it too. Ignored outside the lobby.
    /// </summary>
    public class CommandSetBotSkill : DeterministicCommand
    {
        public int Slot;
        public BotSkillLevel Level = BotSkillLevel.Normal;

        public override void Serialize(BitStream stream)
        {
            stream.Serialize(ref Slot);
            int level = (int)Level;
            stream.Serialize(ref level);
            Level = (BotSkillLevel)level;
        }
    }

    /// <summary>
    /// Sent from the room screen (the host's) to set whether bots fill the empty slots when a match starts.
    /// </summary>
    public class CommandSetBotFill : DeterministicCommand
    {
        public bool Fill = true;

        public override void Serialize(BitStream stream)
        {
            stream.Serialize(ref Fill);
        }
    }
}
