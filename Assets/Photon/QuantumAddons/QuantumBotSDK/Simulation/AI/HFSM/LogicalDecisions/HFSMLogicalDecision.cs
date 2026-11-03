using System;

namespace Quantum
{
    public abstract partial class HFSMLogicalDecision : HFSMDecision
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        public AssetRef<HFSMDecision>[] Decisions;

        // ========== PROTECTED MEMBERS ===============================================================================

        protected HFSMDecision[] _decisions;

        // ========== AssetObject INTERFACE ===========================================================================

        public override void Loaded(IResourceManager resourceManager)
        {
            base.Loaded(resourceManager);

            _decisions = new HFSMDecision[Decisions == null ? 0 : Decisions.Length];
            if (Decisions != null)
            {
                for (Int32 i = 0; i < Decisions.Length; i++)
                {
                    _decisions[i] = (HFSMDecision)resourceManager.GetAsset(Decisions[i].Id);
                }
            }
        }
    }
}