using Quantum.Core;
using System.Collections.Generic;

namespace Quantum
{
    public interface IBotSDKDebugInfo
    {
        public IBotSDKDebugInfoProvider Agent { get; set; }
        public AIBlackboardComponent BlackboardComponent { get; }
        public string GetAssetPath(FrameBase frame);
    }

    public struct BotSDKDebugInfoHFSM : IBotSDKDebugInfo
    {
        public EntityRef EntityRef;
        public HFSMAgent HFSMAgent;
        public AIBlackboardComponent DebuggedBlackboardComponent;

        IBotSDKDebugInfoProvider IBotSDKDebugInfo.Agent
        {
            get => HFSMAgent;
            set => HFSMAgent = (HFSMAgent)value;
        }

        public AIBlackboardComponent BlackboardComponent => DebuggedBlackboardComponent;

        public string GetAssetPath(FrameBase frame)
        {
            return HFSMAgent.GetRootAssetPath(frame);
        }
    }

    public struct BotSDKDebugInfoBT : IBotSDKDebugInfo
    {
        public EntityRef EntityRef;
        public BTAgent BTAgent;
        public AIBlackboardComponent DebuggedBlackboardComponent;

        IBotSDKDebugInfoProvider IBotSDKDebugInfo.Agent
        {
            get => BTAgent;
            set => BTAgent = (BTAgent)value;
        }

        public AIBlackboardComponent BlackboardComponent => DebuggedBlackboardComponent;

        public string GetAssetPath(FrameBase frame)
        {
            return BTAgent.GetTreeAssetPath(frame);
        }
    }

    public struct BotSDKDebugInfoUT : IBotSDKDebugInfo
    {
        public EntityRef EntityRef;
        public UTAgent UTAgent;
        public AIBlackboardComponent DebuggedBlackboardComponent;

        IBotSDKDebugInfoProvider IBotSDKDebugInfo.Agent
        {
            get => UTAgent;
            set => UTAgent = (UTAgent)value;
        }
        public AIBlackboardComponent BlackboardComponent => DebuggedBlackboardComponent;

        public string GetAssetPath(FrameBase frame)
        {
            return UTAgent.GetRootAssetPath(frame);
        }
    }

    public unsafe delegate IEnumerator<IBotSDKDebugInfo> DelegateGetDebugInfo(FrameBase frame, EntityRef entity, void* ptr);
    public interface IBotSDKDebugInfoProvider { DelegateGetDebugInfo GetDebugInfo(); }
}
