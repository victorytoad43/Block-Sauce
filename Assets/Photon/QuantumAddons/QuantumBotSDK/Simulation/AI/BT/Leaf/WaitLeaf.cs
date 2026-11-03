using Photon.Deterministic;
using System;

namespace Quantum
{
    [Serializable]
    [BotSDKTooltip(Text = "Keeps RUNNING for a certain interval of time, then succeeds.")]
    public unsafe partial class WaitLeaf : BTLeaf
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        // How many time shall be waited
        // This is measured in seconds
        [BotSDKTooltip(Text = "Amount of time to keep RUNNING")]
        public FP Duration;

        // Indexer for us to store the End Time value on the BTAgent itself
        public BTDataIndex EndTimeIndex;

        // ========== BTNode INTERFACE ================================================================================

        internal override void InitInternal(BTParams btParams, ref AIContext aiContext)
        {
            base.InitInternal(btParams, ref aiContext);
        }

        public override void OnEnter(BTParams btParams, ref AIContext aiContext)
        {
            base.OnEnter(btParams, ref aiContext);

            FP currentTime;
            FP endTime;

            // Get the current time
            currentTime = btParams.FrameThreadSafe.GetSingleton<BotSDKGlobals>().Data.GameTime;
            // Add the Duration value so we know when the Leaf will stop running
            endTime = currentTime + Duration;

            // Store the final value on the Agent data
            btParams.Agent->SetFPData(btParams.FrameThreadSafe, endTime, EndTimeIndex.Index);
        }

        protected override BTStatus OnUpdate(BTParams btParams, ref AIContext aiContext)
        {
            FP currentTime;
            FP endTime;

            currentTime = btParams.FrameThreadSafe.GetSingleton<BotSDKGlobals>().Data.GameTime;
            endTime = btParams.Agent->GetFPData(btParams.FrameThreadSafe, EndTimeIndex.Index);

            // If waiting time isn't over yet, then we need more frames executing this Leaf
            // So we say that we're still Running
            if (currentTime < endTime)
            {
                return BTStatus.Running;
            }

            // If the waiting time is over, then we succeeded on waiting that amount of time
            // Then we return Success
            return BTStatus.Success;
        }
    }
}
