namespace Quantum
{
    using System;

    public partial class UTRoot : BotSDKAsset
    {
        public AssetRef<Consideration>[] ConsiderationsRefs;

        [NonSerialized]
        private Consideration[] Considerations;

        public override void Loaded(IResourceManager resourceManager)
        {
            base.Loaded(resourceManager);

            Considerations = new Consideration[ConsiderationsRefs == null ? 0 : ConsiderationsRefs.Length];
            if (ConsiderationsRefs != null)
            {
                for (int i = 0; i < ConsiderationsRefs.Length; i++)
                {
                    Considerations[i] = (Consideration)resourceManager.GetAsset(ConsiderationsRefs[i].Id);
                }
            }
        }
    }
}
