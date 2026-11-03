using Photon.Deterministic;

namespace Quantum
{
    [BotSDKHidden]
    [System.Serializable]
    public unsafe partial class ResponseCurve : AIFunction<FP>
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        public AIParamFP Input;

        [BotSDKHidden]
        public FPAnimationCurve Curve;

        public bool Clamp01 = true;

        public FP MultiplyFactor = 1;

        // ========== AIFunctionFP INTERFACE ==========================================================================

        public override FP Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            var blackboard = BotSDKGetters.GetBlackboardComponent(frame, entity, ref aiContext);
            var aiConfig = BotSDKGetters.GetAIConfig(frame, entity, ref aiContext);

            FP input = Input.Resolve(frame, entity, blackboard, aiConfig, ref aiContext);
            FP result = Curve.Evaluate(input);

            if (Clamp01 == true)
            {
                if (result > 1) result = 1;
                else if (result < 0) result = 0;
            }

            return result * MultiplyFactor;
        }

        internal FP ExecuteUtilityTheory(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext, out FP inputValue)
        {
            var blackboard = BotSDKGetters.GetBlackboardComponent(frame, entity, ref aiContext);
            var aiConfig = BotSDKGetters.GetAIConfig(frame, entity, ref aiContext);

            inputValue = Input.Resolve(frame, entity, blackboard, aiConfig, ref aiContext);
            FP max = Curve.EndTime;
            var normalized = inputValue / max;

            FP result = Curve.Evaluate(inputValue);
            inputValue = normalized;

            if (Clamp01 == true)
            {
                if (result > 1) result = 1;
                else if (result < 0) result = 0;
            }

            return result * MultiplyFactor;
        }
    }
}
