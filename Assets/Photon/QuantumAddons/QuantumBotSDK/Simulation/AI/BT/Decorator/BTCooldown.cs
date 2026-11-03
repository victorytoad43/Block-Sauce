using Photon.Deterministic;
using System;

namespace Quantum
{
    [Serializable]
    public unsafe partial class BTCooldown : BTDecorator
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        // How many time should we wait
        public FP CooldownTime;

        // An indexer so we know when the time started counting
        public BTDataIndex StartTimeIndex;

        // ========== BTNode INTERFACE ================================================================================

        internal override void InitInternal(BTParams btParams, ref AIContext aiContext)
        {
            base.InitInternal(btParams, ref aiContext);

            btParams.Agent->SetFPData(btParams.FrameThreadSafe, -CooldownTime, StartTimeIndex.Index);
        }

        public override void OnExit(BTParams btParams, ref AIContext aiContext)
        {
            base.OnExit(btParams, ref aiContext);

            var frame = btParams.FrameThreadSafe;

            var currentTime = btParams.FrameThreadSafe.DeltaTime * btParams.FrameThreadSafe.Number;
            FP startTime = btParams.Agent->GetFPData(frame, StartTimeIndex.Index);
            if (currentTime >= startTime + CooldownTime)
            {
                btParams.Agent->SetFPData(frame, currentTime, StartTimeIndex.Index);
            }
        }

        // We get the Start Time stored on the BTAgent, then we check if the time + cooldown is already over
        // If it is not over, then we return False, blocking the execution of the children nodes
        public override Boolean CheckConditions(BTParams btParams, ref AIContext aiContext)
        {
            var frame = btParams.FrameThreadSafe;

            var entity = btParams.Entity;
            FP startTime = btParams.Agent->GetFPData(frame, StartTimeIndex.Index);

            var currentTime = btParams.FrameThreadSafe.DeltaTime * btParams.FrameThreadSafe.Number;

            return currentTime >= startTime + CooldownTime;
        }
    }
}
