namespace Quantum
{
    using Quantum.BotSDK;
    using System;

    [Serializable]
    [BotSDKTooltip(Text = "Increases an integer value on the Blackboard by a user specified amount.")]
    public unsafe partial class IncreaseBlackboardInt : AIAction
    {
        [BotSDKTooltip(Text = "Link this slot with a Key slot from a Blackboard node.")]
        public AIBlackboardValueKey Key;

        [BotSDKTooltip(Text = "Amount to increase the blackboard value by.")]
        public AIParamInt IncrementAmount;

        [NonSerialized]
        private int _keyHash;

        public override void Loaded(IResourceManager resourceManager)
        {
            _keyHash = Key.Key.GetHashCodeDeterministic();
        }

        public override unsafe void Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            if (_keyHash == 0)
            {
                _keyHash = Key.Key.GetHashCodeDeterministic();
            }

            var blackboard = aiContext.Blackboard;
            var agent = aiContext.HFSMAgent;
            var aiConfig = frame.FindAsset(agent->Config);

            var incrementValue = IncrementAmount.Resolve(frame, entity, blackboard, aiConfig, ref aiContext);

            var currentAmount = blackboard->GetInteger(frame, _keyHash);
            currentAmount += incrementValue;

            blackboard->Set(frame, _keyHash, currentAmount);
        }
    }
}