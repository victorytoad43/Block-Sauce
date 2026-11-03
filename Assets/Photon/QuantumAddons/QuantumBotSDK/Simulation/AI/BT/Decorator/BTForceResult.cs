using System;

namespace Quantum
{
    [Serializable]
    public unsafe partial class BTForceResult : BTDecorator
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        public bool Result;

        // ========== BTNode INTERFACE ================================================================================

        public override Boolean CheckConditions(BTParams btParams, ref AIContext aiContext)
        {
            return Result;
        }
    }
}
