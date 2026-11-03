namespace Quantum
{
    using System;

    [Serializable]
    public partial class IdleAction : AIAction
    {
        public override unsafe void Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
        }
    }
}
