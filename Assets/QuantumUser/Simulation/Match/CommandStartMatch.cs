using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Sent by a player in the lobby to start a match. Any player may send it; the match system ignores it outside
    /// the lobby or with too few players.
    /// </summary>
    public class CommandStartMatch : DeterministicCommand
    {
        public override void Serialize(BitStream stream)
        {
        }
    }
}
