using System;
using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Bot brain decision: true once the bot has been in this state for <see cref="Seconds"/>, and, when
    /// <see cref="RequireHolding"/> says so, while it does (or doesn't) hold a block.
    /// </summary>
    [Serializable]
    public unsafe partial class BotWaitedDecision : HFSMDecision
    {
        public FP Seconds = 1;
        /// <summary>0: either way; 1: only while holding a block; 2: only with empty hands.</summary>
        public int RequireHolding;

        public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            if (aiContext.HFSMAgent->Data.StateTimer < Seconds)
                return false;

            bool holding = frame.Get<Grabber>(entity).Held != EntityRef.None;
            return RequireHolding == 0 || holding == (RequireHolding == 1);
        }
    }
}
