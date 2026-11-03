namespace Quantum
{
    [System.Serializable]
    public unsafe partial class AIFunctionNOT : AIFunction<bool>
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        public AIParamBool Value;

        // ========== AIFunction INTERFACE ============================================================================

        public override bool Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return !Value.Resolve(frame, entity, aiContext.Blackboard, null, ref aiContext);
        }
    }
}
