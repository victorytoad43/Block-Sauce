namespace Quantum
{
    using Photon.Deterministic;
    using System;

    [Serializable]
    [BotSDKTooltip(Text = "Returns \"true\" after the specified amount of time.")]
    public partial class TimerDecision : HFSMDecision
    {
        [BotSDKTooltip(Text = "How long it takes for this Decision to start returning \"true\".")]
        public AIParamFP TimeToTrueState = FP._3;

        public override unsafe bool Decide(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            var blackboard = aiContext.Blackboard;
            var agent = aiContext.HFSMAgent;
            var aiConfig = frame.FindAsset(agent->Config);

            FP requiredTime = TimeToTrueState.Resolve(frame, entity, blackboard, aiConfig, ref aiContext);

            return agent->Data.StateTimer >= requiredTime;
        }
    }
}
