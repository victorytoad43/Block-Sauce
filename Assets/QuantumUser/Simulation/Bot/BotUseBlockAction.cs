using System;

namespace Quantum
{
    /// <summary>
    /// Bot brain action: decides what to do with the block in hand (<see cref="BotChoices.UseHeldBlock"/>).
    /// </summary>
    [Serializable]
    public unsafe partial class BotUseBlockAction : AIAction
    {
        public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            if (BotFetchAction.TryGetBot(frame, entity, out Bot* bot, out BotConfig config))
            {
                BotChoices.UseHeldBlock(frame, entity, bot, config);
            }
        }
    }
}
