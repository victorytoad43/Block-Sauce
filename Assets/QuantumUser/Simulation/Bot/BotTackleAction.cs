using System;

namespace Quantum
{
    /// <summary>
    /// Bot brain action: sets the bot slide tackling the rival <see cref="BotChoices.ChooseTackleTarget"/> picks; with
    /// nobody to tackle, the task fails at once.
    /// </summary>
    [Serializable]
    public unsafe partial class BotTackleAction : AIAction
    {
        public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            if (BotFetchAction.TryGetBot(frame, entity, out Bot* bot, out BotConfig config) == false)
                return;

            EntityRef target = BotChoices.ChooseTackleTarget(frame, entity, bot, config);
            BotSystem.SetTask(frame, bot, config, BotTaskType.SlideTackle, target, default);
            if (target == EntityRef.None)
            {
                bot->Status = BotTaskStatus.Failed;
                return;
            }

            // No hesitation once the bot has spotted the carrier, and a short limit; no new tackle until this one ends.
            bot->ThinkTimer = default;
            bot->TaskTimer = FrameTimer.FromSeconds(frame, config.TackleSeconds);
            bot->TackleTimer = FrameTimer.FromSeconds(frame,
                config.TackleSeconds + config.TackleCooldownSeconds * config.GetPersonality(frame, bot).TackleCooldownScale);
        }
    }
}
