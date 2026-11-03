namespace Quantum
{
    using System;

    [Serializable]
    [BotSDKTooltip(Text = "Always returns \"true\".")]
    public partial class TrueDecision : HFSMDecision
    {
        public override unsafe bool Decide(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return true;
        }
    }
}
