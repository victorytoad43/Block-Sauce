namespace Quantum
{
    public partial class HFSMTransitionSet : BotSDKAsset
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        [BotSDKHidden]
        public string Label;

        public HFSMTransition[] Transitions;

        // ========== AssetObject INTERFACE ===========================================================================

        public override void Loaded(IResourceManager resourceManager)
        {
            base.Loaded(resourceManager);

            if (Transitions != null)
            {
                for (int i = 0; i < Transitions.Length; i++)
                {
                    Transitions[i].Setup(resourceManager);
                }
            }
        }
    }
}

