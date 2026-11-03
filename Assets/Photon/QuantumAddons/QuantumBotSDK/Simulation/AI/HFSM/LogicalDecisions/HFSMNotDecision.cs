using System;

namespace Quantum
{
    [Serializable]
    public partial class HFSMNotDecision : HFSMLogicalDecision
    {

        public override bool Decide(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return CheckDecisions(frame, entity, ref aiContext);
        }

        public bool CheckDecisions(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return !_decisions[0].Decide(frame, entity, ref aiContext);
        }
    }
}