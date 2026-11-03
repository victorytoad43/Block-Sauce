namespace Quantum
{
    public abstract partial class HFSMDecision : BotSDKAsset
    {
        [BotSDKHidden]
        public string Label;

        public virtual bool Decide(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return Decide((Frame)frame, entity, ref aiContext);
        }

        public virtual bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext) { return false; }
    }
}