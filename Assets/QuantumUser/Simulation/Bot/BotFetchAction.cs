using System;

namespace Quantum
{
    /// <summary>
    /// Bot brain action: sets the bot fetching its next block (<see cref="BotChoices.ChooseBlockToFetch"/>); with
    /// nothing to fetch, the task fails at once.
    /// </summary>
    [Serializable]
    public unsafe partial class BotFetchAction : AIAction
    {
        public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            if (TryGetBot(frame, entity, out Bot* bot, out BotConfig config) == false)
                return;

            EntityRef block = BotChoices.ChooseBlockToFetch(frame, entity, bot);
            BotSystem.SetTask(frame, bot, config, BotTaskType.Fetch, block, default);
            if (block == EntityRef.None)
            {
                bot->Status = BotTaskStatus.Failed;
            }
        }

        internal static bool TryGetBot(Frame frame, EntityRef entity, out Bot* bot, out BotConfig config)
        {
            config = frame.TryGetSingleton(out Match match) ? BotSystem.GetConfig(frame, match) : null;
            return frame.Unsafe.TryGetPointer(entity, out bot) && config != null;
        }
    }
}
