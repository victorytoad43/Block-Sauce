namespace Quantum
{
    using Quantum.BotSDK;
    using System;

    [Serializable]
    [BotSDKTooltip(Text = "Sets an integer value on the Blackboard to a user specified amount.")]
    public unsafe partial class SetBlackboardInt : AIAction
    {
        [BotSDKTooltip(Text = "Link this slot with a Key slot from a Blackboard node.")]
        public AIBlackboardValueKey Key;

        [BotSDKTooltip(Text = "Amount to set the blackboard value to.")]
        public AIParamInt Value;

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

            var value = Value.Resolve(frame, entity, blackboard, aiConfig, ref aiContext);
            blackboard->Set(frame, _keyHash, value);
        }
    }
}