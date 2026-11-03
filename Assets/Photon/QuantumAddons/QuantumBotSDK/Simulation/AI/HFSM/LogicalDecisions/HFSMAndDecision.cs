using System;

namespace Quantum
{
    [Serializable]
    public partial class HFSMAndDecision : HFSMLogicalDecision
    {
        public override bool Decide(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return CheckDecisions(frame, entity, ref aiContext);
        }

        public bool CheckDecisions(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            foreach (var decision in _decisions)
            {
                if (decision.Decide(frame, entity, ref aiContext) == false)
                    return false;
            }
            return true;
        }
    }
}