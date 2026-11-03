namespace Quantum
{
    [BotSDKHidden]
    [System.Serializable]
    public unsafe partial class DefaultAIFunctionBool : AIFunction<bool>
    {
        // ========== AIFunction INTERFACE ============================================================================

        public override bool Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return false;
        }
    }
}
