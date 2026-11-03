namespace Quantum
{
    [BotSDKHidden]
    [System.Serializable]
    public unsafe partial class DefaultAIFunctionString : AIFunction<string>
    {
        // ========== AIFunction INTERFACE ============================================================================

        public override string Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return null;
        }
    }
}
