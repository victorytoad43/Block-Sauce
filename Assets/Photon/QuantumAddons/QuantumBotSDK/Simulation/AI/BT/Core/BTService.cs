using Photon.Deterministic;
using System;

namespace Quantum
{
    public unsafe abstract partial class BTService : BotSDKAsset
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        [BotSDKHidden]
        public string Label;

        [BotSDKTooltip(Text = "The interval to wait before executing this service.")]
        public FP IntervalInSec;

        /// <summary>
        /// This is not used on the Service's OnEnter, but rather on the OWNER node's OnEnter, as to make it possible to
        /// run a Service Update code before it descends on the tree as some child nodes might need this to poll information
        /// before descending into the tree.
        /// It is not used on the Service's OnEnter code because that is not called upon DESCENDING on the tree, but rather when
        /// bubbling up if the Leaf resulted in RUNNING
        /// </summary>
        [BotSDKTooltip(Text = "Should this service be executed when it starts running with no initial interval?")]
        public bool RunOnEnter;

        [BotSDKHidden] public Int32 Id;

        // ========== BTService INTERFACE =============================================================================

        internal virtual void InitInternal(BTParams btParams, ref AIContext aiContext)
        {
            var endTimesList = btParams.FrameThreadSafe.ResolveList<FP>(btParams.Agent->ServicesEndTimes);
            endTimesList.Add(0);
        }

        public virtual void Init(BTParams btParams, ref AIContext aiContext) { }

        internal void RunUpdate(BTParams btParams, ref AIContext aiContext)
        {
            var endTime = GetEndTime(btParams.FrameThreadSafe, btParams.Agent);

            BotSDKEditorEvents.BT.InvokeOnServiceProgress(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, CircuitId, GetTimeProgressPercent(btParams.FrameThreadSafe, btParams.Agent));

            if (btParams.FrameThreadSafe.GetSingleton<BotSDKGlobals>().Data.GameTime >= endTime)
            {
                OnUpdate(btParams, ref aiContext);
                SetEndTime(btParams.FrameThreadSafe, btParams.Agent);
            }
        }

        /// <summary>
        /// Returns the progress for the next Service update, in percentage.
        /// </summary>
        public FP GetTimeProgressPercent(FrameThreadSafe frame, BTAgent* agent)
        {
            var activeServices = frame.ResolveList(agent->ActiveServices);
            if (activeServices.Contains(this) == false)
            {
                return FP._0;
            }

            var startTime = GetStartTime(frame, agent);
            var endTime = GetEndTime(frame, agent);
            var currentTime = frame.GetSingleton<BotSDKGlobals>().Data.GameTime;

            var duration = endTime - startTime;
            // If duration is zero, it means that the service is set to execute every frame, so it's progress is always complete
            if (duration == 0)
            {
                return 1;
            }

            var elapsedTime = currentTime - startTime;

            var percentPassed = elapsedTime / duration;
            return percentPassed;
        }

        /// <summary>
        /// Called when this Service's Owner is entered (i.e Composite and Leaf nodes)
        /// </summary>
        internal void OnOwnerEntered(BTParams btParams, ref AIContext aiContext)
        {
            if (RunOnEnter == true)
            {
                OnUpdate(btParams, ref aiContext);
            }
        }

        /// <summary>
        /// OnEnter for a Service is only called when such Service is added to the current context
        /// i.e when its branch results in RUNNING
        /// </summary>
        public virtual void OnEnter(BTParams btParams, ref AIContext aiContext)
        {
            SetEndTime(btParams.FrameThreadSafe, btParams.Agent);
        }

        /// <summary>
        /// Called whenever the Service is part of the current subtree
        /// and its waiting time is already over
        /// </summary>
        protected abstract void OnUpdate(BTParams btParams, ref AIContext aiContext);

        // ========== PUBLIC METHODS ==================================================================================

        public void SetEndTime(FrameThreadSafe frame, BTAgent* agent)
        {
            var endTimesList = frame.ResolveList<FP>(agent->ServicesEndTimes);
            endTimesList[Id] = frame.GetSingleton<BotSDKGlobals>().Data.GameTime + IntervalInSec;
        }

        public FP GetEndTime(FrameThreadSafe frame, BTAgent* agent)
        {
            var endTime = frame.ResolveList(agent->ServicesEndTimes);
            return endTime[Id];
        }

        public FP GetStartTime(FrameThreadSafe frame, BTAgent* agent)
        {
            var endTime = frame.ResolveList(agent->ServicesEndTimes);
            return endTime[Id] - IntervalInSec;
        }

        public static void TickServices(BTParams btParams, ref AIContext aiContext)
        {
            var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRef<BTService>>(btParams.Agent->ActiveServices);

            for (int i = 0; i < activeServicesList.Count; i++)
            {
                var service = btParams.FrameThreadSafe.FindAsset<BTService>(activeServicesList[i].Id);
                try
                {
                    service.RunUpdate(btParams, ref aiContext);
                }
                catch (Exception e)
                {
                    Log.Error($"Exception in Behaviour Tree service '{service.GetType().ToString()}' ({service.Guid}) - setting node status to Failure");
                    Log.Exception(e);
                }
            }
        }
    }
}
