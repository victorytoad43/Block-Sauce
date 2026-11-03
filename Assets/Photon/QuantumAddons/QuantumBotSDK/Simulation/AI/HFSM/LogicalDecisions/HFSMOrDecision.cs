using System;

namespace Quantum
{
    [Serializable]
    public partial class HFSMOrDecision : HFSMLogicalDecision
    {
        public override bool Decide(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return CheckDecisions(frame, entity, ref aiContext);
        }

        private bool CheckDecisions(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            foreach (var decision in _decisions)
            {
                if (decision.Decide(frame, entity, ref aiContext) == true)
                    return true;
            }
            return false;
        }
    }
}