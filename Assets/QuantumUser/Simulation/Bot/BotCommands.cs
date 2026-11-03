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
