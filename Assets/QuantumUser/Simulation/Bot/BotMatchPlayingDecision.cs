using System;

namespace Quantum
{
    /// <summary>
    /// Bot brain decision: true while a match is being played (or, with Invert, while it isn't).
    /// </summary>
    [Serializable]
    public partial class BotMatchPlayingDecision : HFSMDecision
    {
        public bool Invert;

        public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            bool playing = frame.TryGetSingleton(out Match match) && match.State == MatchState.Playing;
            return playing != Invert;
        }
    }
}
