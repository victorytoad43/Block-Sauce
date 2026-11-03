namespace Quantum
{
    public abstract unsafe partial class AIActionBase : BotSDKAsset
    {
        // ========== PUBLIC MEMBERS ==================================================================================


        // ========== AIAction INTERFACE ================================================================================

        public virtual void Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
        }
    }
}
