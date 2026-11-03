namespace Quantum
{
    public abstract partial class AIAction : BotSDKAsset
    {
        [BotSDKHidden]
        public string Label;

        public virtual void Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            Execute((Frame)frame, entity, ref aiContext);
        }

        public virtual void Execute(Frame frame, EntityRef entity, ref AIContext aiContext) { }
    }
}