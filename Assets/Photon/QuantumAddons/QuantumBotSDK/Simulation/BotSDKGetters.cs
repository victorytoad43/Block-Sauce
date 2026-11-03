namespace Quantum
{
    public static unsafe partial class BotSDKGetters
    {
        public static partial AIBlackboardComponent* GetBlackboardComponent(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext);
        public static partial AIConfig GetAIConfig(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext);

        public static partial AIBlackboardComponent* GetBlackboardComponent(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            frame.TryGetPointer<AIBlackboardComponent>(entity, out var blackboard);
            return blackboard;
        }

        public static partial AIConfig GetAIConfig(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            return default;
        }
    }
}