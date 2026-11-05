using System;

namespace Quantum
{
    /// <summary>
    /// Bot brain decision: true when the bot has empty hands, isn't waiting after its last tackle, and a rival is worth
    /// a slide tackle (<see cref="BotChoices.ChooseTackleTarget"/>).
    /// </summary>
    [Serializable]
    public unsafe partial class BotTackleDecision : HFSMDecision
    {
        public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            return BotFetchAction.TryGetBot(frame, entity, out Bot* bot, out BotConfig config)
                && BotChoices.ChooseTackleTarget(frame, entity, bot, config) != EntityRef.None;
        }
    }
}
