namespace Quantum
{
    using Quantum.BotSDK;
    using System;

    public enum EValueComparison
    {
        None,
        LessThan,
        MoreThan,
        EqualTo,
    }

    [Serializable]
    [BotSDKTooltip(Text = "Does a comparison check with a Blackboard variable.")]
    public partial class CheckBlackboardInt : HFSMDecision
    {
        [BotSDKTooltip(Text = "Link this slot with a Key slot from a Blackboard node.")]
        public AIBlackboardValueKey Key;

        [BotSDKTooltip(Text = "Define the operation to be performed.")]
        public EValueComparison Comparison = EValueComparison.MoreThan;

        [BotSDKTooltip(Text = "Define the value used in the comparison.")]
        public AIParamInt DesiredValue = 1;

        [NonSerialized]
        private int _keyHash;

        public override void Loaded(IResourceManager resourceManager)
        {
            _keyHash = Key.Key.GetHashCodeDeterministic();
        }

        public override unsafe bool Decide(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            if (_keyHash == 0)
            {
                _keyHash = Key.Key.GetHashCodeDeterministic();
            }

            var blackboard = aiContext.Blackboard;
            var agent = aiContext.HFSMAgent;
            var aiConfig = frame.FindAsset(agent->Config);

            var comparisonValue = DesiredValue.Resolve(frame, entity, blackboard, aiConfig, ref aiContext);
            var currentAmount = blackboard->GetInteger(frame, Key.Key);

            switch (Comparison)
            {
                case EValueComparison.LessThan: return currentAmount < comparisonValue;
                case EValueComparison.MoreThan: return currentAmount > comparisonValue;
                case EValueComparison.EqualTo: return currentAmount == comparisonValue;
                default: return false;
            }
        }
    }
}