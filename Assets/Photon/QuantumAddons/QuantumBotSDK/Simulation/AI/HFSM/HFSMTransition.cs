using System;

namespace Quantum
{
    [Serializable]
    public class HFSMTransition
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        public string Id;

        public Int32 EventKey = 0;
        public AssetRef<HFSMDecision> DecisionLink;
        public AssetRef<HFSMState> StateLink;
        public AssetRef<HFSMTransitionSet> TransitionSetLink;

        [NonSerialized]
        public HFSMDecision Decision;
        [NonSerialized]
        public HFSMState State;
        [NonSerialized]
        public HFSMTransitionSet TransitionSet;

        // ========== PUBLIC METHODS ==================================================================================

        public void Setup(IResourceManager resourceManager)
        {
            Decision = (HFSMDecision)resourceManager.GetAsset(DecisionLink.Id);
            State = (HFSMState)resourceManager.GetAsset(StateLink.Id);
            TransitionSet = (HFSMTransitionSet)resourceManager.GetAsset(TransitionSetLink.Id);
        }
    }
}
