namespace Quantum
{
    using Quantum.Core;

    public static unsafe class AIConfigExtensions
    {
        public static AIConfig GetConfig(this HFSMAgent agent, FrameBase frame)
        {
            return frame.FindAsset(agent.Config);
        }

        public static AIConfig GetConfig(this HFSMAgent agent, FrameThreadSafe frame)
        {
            return frame.FindAsset(agent.Config);
        }

        public static AIConfig GetConfig(this BTAgent agent, FrameBase frame)
        {
            return frame.FindAsset(agent.Config);
        }

        public static AIConfig GetConfig(this BTAgent agent, FrameThreadSafe frame)
        {
            return frame.FindAsset(agent.Config);
        }

        public static AIConfig GetConfig(this UTAgent agent, FrameBase frame)
        {
            return frame.FindAsset(agent.Config);
        }

        public static AIConfig GetConfig(this UTAgent agent, FrameThreadSafe frame)
        {
            return frame.FindAsset(agent.Config);
        }
    }
}