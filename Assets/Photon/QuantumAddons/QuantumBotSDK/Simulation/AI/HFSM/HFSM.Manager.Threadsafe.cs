using Photon.Deterministic;
using System;

namespace Quantum
{
    public static unsafe partial class HFSMManager
    {
        public static unsafe partial class ThreadSafe
        {
            // ========== PUBLIC METHODS ==================================================================================

            /// <summary>
            /// Initializes the HFSM, making the current state to be equals the initial state
            /// </summary>
            public static unsafe void Init(FrameThreadSafe frame, EntityRef entity, HFSMRoot root)
            {
                AIContext aiContext = new AIContext();
                aiContext.SetupHFSMFromEntity(frame, entity);

                Init(frame, entity, root, ref aiContext);
            }
            /// <summary>
            /// Initializes the HFSM, making the current state to be equals the initial state
            /// </summary>
            public static unsafe void Init(FrameThreadSafe frame, EntityRef entity, HFSMRoot root, ref AIContext aiContext)
            {
                BotSDKEditorEvents.HFSM.InvokeOnSetupDebugger(aiContext.AgentEntity, root.Path);
                aiContext.HFSMAgent->Data.Root = root;
                if (aiContext.HFSMAgent->Data.Root.Equals(default) == false)
                {
                    HFSMState initialState = frame.FindAsset<HFSMState>(root.InitialState.Id);
                    ChangeState(initialState, frame, entity, "", ref aiContext);
                }
            }

            /// <summary>
            /// Initializes the HFSM, making the current state to be equals the initial state
            /// </summary>
            [Obsolete("Specify the HFSMAgent using AIContext instead.")]
            public static unsafe void Init(FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity, HFSMRoot root)
            {
                AIContext aiContext = new AIContext();
                Init(frame, hfsmData, entity, root, ref aiContext);
            }
            /// <summary>
            /// Initializes the HFSM, making the current state to be equals the initial state
            /// </summary>
            [Obsolete("Specify the HFSMAgent using AIContext instead.")]
            public static unsafe void Init(FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity, HFSMRoot root, ref AIContext aiContext)
            {
                BotSDKEditorEvents.HFSM.InvokeOnSetupDebugger(aiContext.AgentEntity, root.Path);
                hfsmData->Root = root;
                if (hfsmData->Root.Equals(default) == false)
                {
                    HFSMState initialState = frame.FindAsset<HFSMState>(root.InitialState.Id);
                    ChangeState(initialState, frame, entity, "", ref aiContext);
                }
            }

            /// <summary>
            /// Update the state of the HFSM.
            /// </summary>
            /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
            public static void Update(FrameThreadSafe frame, FP deltaTime, EntityRef entity)
            {
                AIContext aiContext = new AIContext();
                Update(frame, deltaTime, entity, ref aiContext);
            }

            /// <summary>
            /// Update the state of the HFSM.
            /// </summary>
            /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
            [Obsolete("Specify the HFSMAgent using AIContext instead.")]
            public static void Update(FrameThreadSafe frame, FP deltaTime, HFSMData* hfsmData, EntityRef entity)
            {
                AIContext aiContext = new AIContext();
                Update(frame, deltaTime, hfsmData, entity, ref aiContext);
            }
            /// <summary>
            /// Update the state of the HFSM.
            /// </summary>
            /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
            [Obsolete("Specify the HFSMAgent using AIContext instead.")]
            public static void Update(FrameThreadSafe frame, FP deltaTime, HFSMData* hfsmData, EntityRef entity, ref AIContext aiContext)
            {
                // Make sure the HFSM Root asset is loaded upon Update so it guarantees that late joiners will load and trigger all Loaded callbacks needed,
                // which then caches references to other assets. The root loads all the referenced root states which loads all the child states in sequence, transitions and so on
                var root = frame.FindAsset<HFSMRoot>(hfsmData->Root.Id);

                HFSMState currentState = frame.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
                currentState.Execute(frame, deltaTime, entity, ref aiContext);
            }
            /// <summary>
            /// Update the state of the HFSM.
            /// </summary>
            /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
            public static void Update(FrameThreadSafe frame, FP deltaTime, EntityRef entity, ref AIContext aiContext)
            {
                // Make sure the HFSM Root asset is loaded upon Update so it guarantees that late joiners will load and trigger all Loaded callbacks needed,
                // which then caches references to other assets. The root loads all the referenced root states which loads all the child states in sequence, transitions and so on
                var root = frame.FindAsset<HFSMRoot>(aiContext.HFSMAgent->Data.Root.Id);

                HFSMState currentState = frame.FindAsset<HFSMState>(aiContext.HFSMAgent->Data.CurrentState.Id);
                currentState.Execute(frame, deltaTime, entity, ref aiContext);
            }


            /// <summary>
            /// Triggers an event if the target HFSM listens to it
            /// </summary>
            public static unsafe void TriggerEvent(FrameThreadSafe frame, EntityRef entity, string eventName)
            {
                AIContext aiContext = new AIContext();
                aiContext.SetupHFSMFromEntity(frame, entity);

                TriggerEvent(frame, entity, eventName, ref aiContext);
            }
            /// <summary>
            /// Triggers an event if the target HFSM listens to it
            /// </summary>
            public static unsafe void TriggerEvent(FrameThreadSafe frame, EntityRef entity, string eventName, ref AIContext aiContext)
            {
                var hfsmData = &aiContext.HFSMAgent->Data;

                Int32 eventInt = 0;

                HFSMRoot hfsmRoot = frame.FindAsset<HFSMRoot>(hfsmData->Root.Id);
                if (hfsmRoot.RegisteredEvents.TryGetValue(eventName, out eventInt))
                {
                    if (hfsmData->CurrentState.Equals(default) == false)
                    {
                        HFSMState currentState = frame.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
                        currentState.Event(frame, entity, eventInt, ref aiContext);
                    }
                }
            }

            /// <summary>
            /// Triggers an event if the target HFSM listens to it
            /// </summary>
            public static unsafe void TriggerEventNumber(FrameThreadSafe frame, EntityRef entity, Int32 eventInt)
            {
                AIContext aiContext = new AIContext();
                aiContext.SetupHFSMFromEntity(frame, entity);

                TriggerEventNumber(frame, entity, eventInt, ref aiContext);
            }
            /// <summary>
            /// Triggers an event if the target HFSM listens to it
            /// </summary>
            public static unsafe void TriggerEventNumber(FrameThreadSafe frame, EntityRef entity, Int32 eventInt, ref AIContext aiContext)
            {
                var hfsmData = &aiContext.HFSMAgent->Data;

                if (hfsmData->CurrentState.Equals(default) == false)
                {
                    HFSMState currentState = frame.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
                    currentState.Event(frame, entity, eventInt, ref aiContext);
                }
            }

            /// <summary>
            /// Triggers an event if the target HFSM listens to it
            /// </summary>
            [Obsolete("Specify the HFSMAgent using AIContext instead.")]
            public static unsafe void TriggerEvent(FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity, string eventName)
            {
                AIContext aiContext = new AIContext();
                aiContext.SetupHFSMFromEntity(frame, entity);

                TriggerEvent(frame, hfsmData, entity, eventName, ref aiContext);
            }
            /// <summary>
            /// Triggers an event if the target HFSM listens to it
            /// </summary>
            [Obsolete("Specify the HFSMAgent using AIContext instead.")]
            public static unsafe void TriggerEvent(FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity, string eventName,
                      ref AIContext aiContext)
            {
                Int32 eventInt = 0;

                HFSMRoot hfsmRoot = frame.FindAsset<HFSMRoot>(hfsmData->Root.Id);
                if (hfsmRoot.RegisteredEvents.TryGetValue(eventName, out eventInt))
                {
                    if (hfsmData->CurrentState.Equals(default) == false)
                    {
                        HFSMState currentState = frame.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
                        currentState.Event(frame, entity, eventInt, ref aiContext);
                    }
                }
            }

            /// <summary>
            /// Triggers an event if the target HFSM listens to it
            /// </summary>
            [Obsolete("Specify the HFSMAgent using AIContext instead.")]
            public static unsafe void TriggerEventNumber(FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity, Int32 eventInt)
            {
                AIContext aiContext = new AIContext();
                TriggerEventNumber(frame, hfsmData, entity, eventInt, ref aiContext);
            }
            /// <summary>
            /// Triggers an event if the target HFSM listens to it
            /// </summary>
            [Obsolete("Specify the HFSMAgent using AIContext instead.")]
            public static unsafe void TriggerEventNumber(FrameThreadSafe frame, HFSMData* hfsmData, EntityRef entity, Int32 eventInt,
                      ref AIContext aiContext)
            {
                if (hfsmData->CurrentState.Equals(default) == false)
                {
                    HFSMState currentState = frame.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
                    currentState.Event(frame, entity, eventInt, ref aiContext);
                }
            }

            // ========== INTERNAL METHODS ================================================================================

            /// <summary>
            /// Executes the On Exit actions for the current state, then changes the current state
            /// </summary>
            internal static void ChangeState(HFSMState nextState, FrameThreadSafe frame, EntityRef entity, string transitionId, ref AIContext aiContext)
            {
                Assert.Check(nextState != null, "Tried to change HFSM to a null state");

                var hfsmData = &aiContext.HFSMAgent->Data;

                HFSMState currentState = frame.FindAsset<HFSMState>(hfsmData->CurrentState.Id);
                currentState?.ExitState(nextState, frame, entity, ref aiContext);
                hfsmData->CurrentState = nextState;

                if (frame.IsVerified == true && entity != default(EntityRef))
                {
                    var rootPath = frame.FindAsset<HFSMRoot>(hfsmData->Root.Id).Path;
                    BotSDKEditorEvents.HFSM.InvokeStateChanged(aiContext.AgentEntity, rootPath, hfsmData->Root.Id.Value, nextState.Guid.Value, transitionId, nextState.CircuitId);
                }

                nextState.EnterState(frame, entity, ref aiContext);
            }
        }
    }
}
