using Quantum.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Quantum
{
    public unsafe partial struct UTAgent : IBotSDKDebugInfoProvider
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        // Used to setup info on the Unity debugger
        public string GetRootAssetPath(FrameBase frame) => frame.FindAsset<UTRoot>(UtilityReasoner.UTRoot.Id).Path;
        public string GetRootAssetPath(FrameThreadSafe frame) => frame.FindAsset<UTRoot>(UtilityReasoner.UTRoot.Id).Path;

        // ========== PUBLIC METHODS ==================================================================================

        #region DebugInfo
        public static UTAgentsIterator Iterator = new UTAgentsIterator();

        public class UTAgentsIterator : IEnumerator<IBotSDKDebugInfo>
        {
            private int _index = -1;

            private UTAgent _utAgent;
            private AIBlackboardComponent _blackboardComponent;

            public void Initialize(UTAgent utAgent, AIBlackboardComponent blackboardComponent)
            {
                Reset();

                _utAgent = utAgent;
                _blackboardComponent = blackboardComponent;
            }

            public IBotSDKDebugInfo Current
            {
                get
                {
                    return new BotSDKDebugInfoUT
                    {
                        UTAgent = _utAgent,
                        DebuggedBlackboardComponent = _blackboardComponent,
                    };
                }
            }

            object IEnumerator.Current => throw new NotImplementedException();

            public void Dispose() { }

            public bool MoveNext()
            {
                _index++;
                return _index == 0 ? true : false;
            }

            public void Reset()
            {
                _index = -1;
            }
        }

        public DelegateGetDebugInfo GetDebugInfo()
        {
            return GetDebugInfoList;
        }

        public static IEnumerator<IBotSDKDebugInfo> GetDebugInfoList(FrameBase frame, EntityRef entity, void* ptr)
        {
            frame.TryGet<UTAgent>(entity, out var utAgent);
            frame.TryGet<AIBlackboardComponent>(entity, out var blackboardComponent);

            Iterator.Initialize(utAgent, blackboardComponent);
            return Iterator;
        }
        #endregion
    }
}
