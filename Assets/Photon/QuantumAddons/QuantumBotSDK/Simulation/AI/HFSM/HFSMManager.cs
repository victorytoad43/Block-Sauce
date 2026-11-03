using Photon.Deterministic;
using Quantum.Core;
using System;
using System.Runtime.CompilerServices;

namespace Quantum
{
    public static unsafe partial class HFSMManager
    {
        // ========== PUBLIC METHODS ==================================================================================

        #region Init
        // ---- WITHOUT CONTEXT

        /// <summary>
        /// Initializes the HFSM, making the current state to be equals the initial state
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Init(FrameBase frame, EntityRef entity, AssetRef<HFSMRoot> rootRef)
        {
            var root = frame.FindAsset<HFSMRoot>(rootRef);
            Init(frame, entity, root);
        }

        /// <summary>
        /// Initializes the HFSM, making the current state to be equals the initial state
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Init(FrameBase frame, EntityRef entity, HFSMRoot root)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetupHFSMFromEntity((FrameThreadSafe)frame, entity);

            ThreadSafe.Init((FrameThreadSafe)frame, entity, root, ref aiContext);
        }

        /// <summary>
        /// Initializes the HFSM, making the current state to be equals the initial state
        /// </summary>
        [Obsolete("Specify the HFSMAgent using AIContext instead.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Init(FrameBase frame, HFSMData* hfsmData, EntityRef entity, HFSMRoot root)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetupHFSMFromEntity((FrameThreadSafe)frame, entity);

            ThreadSafe.Init((FrameThreadSafe)frame, hfsmData, entity, root, ref aiContext);
        }

        // ---- WITH CONTEXT

        /// <summary>
        /// Initializes the HFSM, making the current state to be equals the initial state
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Init(FrameBase frame, EntityRef entity, AssetRef<HFSMRoot> rootRef, ref AIContext aiContext)
        {
            var root = frame.FindAsset<HFSMRoot>(rootRef);
            ThreadSafe.Init((FrameThreadSafe)frame, entity, root, ref aiContext);
        }

        /// <summary>
        /// Initializes the HFSM, making the current state to be equals the initial state
        /// </summary>
        [Obsolete("Specify the HFSMAgent using AIContext instead.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Init(FrameBase frame, HFSMData* hfsmData, EntityRef entity, HFSMRoot root, ref AIContext aiContext)
        {
            ThreadSafe.Init((FrameThreadSafe)frame, hfsmData, entity, root, ref aiContext);
        }

        #endregion

        #region Update
        // ---- WITHOUT CONTEXT

        /// <summary>
        /// Update the state of the HFSM.
        /// </summary>
        /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(FrameBase frame, FP deltaTime, EntityRef entity)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetupHFSMFromEntity((FrameThreadSafe)frame, entity);

            ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, entity, ref aiContext);
        }

        /// <summary>
        /// Update the state of the HFSM.
        /// </summary>
        /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
        [Obsolete("Specify the HFSMAgent using AIContext instead.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(FrameBase frame, FP deltaTime, HFSMData* hfsmData, EntityRef entity)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetupHFSMFromEntity((FrameThreadSafe)frame, entity);

            ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, hfsmData, entity, ref aiContext);
        }

        // ---- WITH CONTEXT

        /// <summary>
        /// Update the state of the HFSM.
        /// </summary>
        /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(FrameBase frame, FP deltaTime, EntityRef entity, ref AIContext aiContext)
        {
            ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, entity, ref aiContext);
        }

        /// <summary>
        /// Update the state of the HFSM.
        /// </summary>
        /// <param name="deltaTime">Usually the current deltaTime so the HFSM accumulates the time stood on the current state</param>
        [Obsolete("Specify the HFSMAgent using AIContext instead.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(FrameBase frame, FP deltaTime, HFSMData* hfsmData, EntityRef entity, ref AIContext aiContext)
        {
            ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, hfsmData, entity, ref aiContext);
        }

        #endregion

        #region Events
        // ---- WITHOUT CONTEXT

        /// <summary>
        /// Triggers an event if the target HFSM listens to it
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void TriggerEvent(FrameBase frame, EntityRef entity, string eventName)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetupHFSMFromEntity((FrameThreadSafe)frame, entity);

            ThreadSafe.TriggerEvent((FrameThreadSafe)frame, entity, eventName, ref aiContext);
        }

        /// <summary>
        /// Triggers an event if the target HFSM listens to it
        /// </summary>
        [Obsolete("Specify the HFSMAgent using AIContext instead.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void TriggerEvent(FrameBase frame, HFSMData* hfsmData, EntityRef entity, string eventName)
        {
            AIContext aiContext = new AIContext();
            ThreadSafe.TriggerEvent((FrameThreadSafe)frame, hfsmData, entity, eventName, ref aiContext);
        }

        /// <summary>
        /// Triggers an event if the target HFSM listens to it
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void TriggerEventNumber(FrameBase frame, EntityRef entity, Int32 eventInt)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetupHFSMFromEntity((FrameThreadSafe)frame, entity);

            ThreadSafe.TriggerEventNumber((FrameThreadSafe)frame, entity, eventInt, ref aiContext);
        }

        /// <summary>
        /// Triggers an event if the target HFSM listens to it
        /// </summary>
        [Obsolete("Specify the HFSMAgent using AIContext instead.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void TriggerEventNumber(FrameBase frame, HFSMData* hfsmData, EntityRef entity, Int32 eventInt)
        {
            AIContext aiContext = new AIContext();
            ThreadSafe.TriggerEventNumber((FrameThreadSafe)frame, hfsmData, entity, eventInt, ref aiContext);
        }

        // ---- WITH CONTEXT

        /// <summary>
        /// Triggers an event if the target HFSM listens to it
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void TriggerEvent(FrameBase frame, EntityRef entity, string eventName, ref AIContext aiContext)
        {
            ThreadSafe.TriggerEvent((FrameThreadSafe)frame, entity, eventName, ref aiContext);
        }

        /// <summary>
        /// Triggers an event if the target HFSM listens to it
        /// </summary>
        [Obsolete("Specify the HFSMAgent using AIContext instead.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void TriggerEvent(FrameBase frame, HFSMData* hfsmData, EntityRef entity, string eventName, ref AIContext aiContext)
        {
            ThreadSafe.TriggerEvent((FrameThreadSafe)frame, hfsmData, entity, eventName, ref aiContext);
        }

        /// <summary>
        /// Triggers an event if the target HFSM listens to it
        /// </summary>
        [Obsolete("Specify the HFSMAgent using AIContext instead.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void TriggerEventNumber(FrameBase frame, HFSMData* hfsmData, EntityRef entity, Int32 eventInt, ref AIContext aiContext)
        {
            ThreadSafe.TriggerEventNumber((FrameThreadSafe)frame, hfsmData, entity, eventInt, ref aiContext);
        }

        #endregion

        // ========== INTERNAL METHODS ================================================================================

        /// <summary>
        /// Executes the On Exit actions for the current state, then changes the current state
        /// </summary>
        [Obsolete("Specify the HFSMAgent using AIContext instead.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ChangeState(HFSMState nextState, FrameBase frame, HFSMData* hfsmData, EntityRef entity, string transitionId, ref AIContext aiContext)
        {
            ThreadSafe.ChangeState(nextState, (FrameThreadSafe)frame, entity, transitionId, ref aiContext);
        }
    }
}
