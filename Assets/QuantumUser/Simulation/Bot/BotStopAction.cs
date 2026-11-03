using System;

namespace Quantum
{
    /// <summary>
    /// Bot brain action: stops whatever the bot was doing; it stands still.
    /// </summary>
    [Serializable]
    public unsafe partial class BotStopAction : AIAction
    {
        public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
        {
            if (BotFetchAction.TryGetBot(frame, entity, out Bot* bot, out BotConfig config))
            {
                BotSystem.SetTask(frame, bot, config, BotTaskType.None, EntityRef.None, default);
            }
        }
    }
}
