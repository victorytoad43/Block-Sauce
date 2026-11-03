namespace Quantum
{
    [System.Serializable]
    public unsafe partial class AIFunctionOR : AIFunction<bool>
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        public AIParamBool ValueA;
        public AIParamBool ValueB;

        // ========== AIFunction INTERFACE ============================================================================

        public override bool Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return ValueA.Resolve(frame, entity, aiContext.Blackboard, null, ref aiContext)
                || ValueB.Resolve(frame, entity, aiContext.Blackboard, null, ref aiContext);
        }
    }
}
