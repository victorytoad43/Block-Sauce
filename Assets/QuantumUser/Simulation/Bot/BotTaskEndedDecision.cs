using System;

namespace Quantum
{
    /// <summary>
    /// Bot brain decision: true once the bot's task has ended with <see cref="Status"/>, and, when
    /// <see cref="RequireHolding"/> says so, while it does (or doesn't) hold a block.
    /// </summary>
    [Serializable]
    public unsafe partial class BotTaskEndedDecision : HFSMDecision
    {
        public BotTaskStatus Status = BotTaskStatus.Done;
        /// <summary>0: either way; 1: only while holding a block; 2: only with empty hands.</summary>
        public int RequireHolding;

        public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            if (frame.Unsafe.TryGetPointer(entity, out Bot* bot) == false || bot->Status != Status)
                return false;

            bool holding = frame.Get<Grabber>(entity).Held != EntityRef.None;
            return RequireHolding == 0 || holding == (RequireHolding == 1);
        }
    }
}
