using System;

namespace Quantum
{

    /// <summary>
    /// The selector task is similar to an or operation. It will return success as soon as one of its child tasks return success.
    /// If a child task returns failure then it will sequentially run the next task. If no child task returns success then it will return failure.
    /// </summary>
    [Serializable]
    [BotSDKTooltip(Text = "Succeeds as soon as one of its child nodes succeed.")]
    public unsafe partial class BTSelector : BTComposite
    {
        // ========== PROTECTED METHODS ===============================================================================

        protected override BTStatus OnUpdate(BTParams btParams, ref AIContext aiContext)
        {
            BTStatus status = BTStatus.Success;

            while (GetCurrentChildId(btParams.FrameThreadSafe, btParams.Agent) < _childInstances.Length)
            {
                var currentChildId = GetCurrentChildId(btParams.FrameThreadSafe, btParams.Agent);
                var child = _childInstances[currentChildId];
                status = child.Execute(btParams, ref aiContext);

                if (status == BTStatus.Abort && btParams.Agent->IsAborting == true)
                {
                    return BTStatus.Abort;
                }

                if (status == BTStatus.Failure || status == BTStatus.Abort)
                {
                    SetCurrentChildId(btParams.FrameThreadSafe, currentChildId + 1, btParams.Agent);
                }
                else
                    break;
            }

            return status;
        }

        // ========== INTERNAL METHODS ================================================================================

        public override bool ChildCompletedRunning(BTParams btParams, BTStatus childResult, ref AIContext aiContext)
        {
            if (childResult == BTStatus.Abort && btParams.Agent->IsAborting == true)
            {
                return true;
            }

            if (childResult == BTStatus.Failure || childResult == BTStatus.Abort)
            {
                var currentChild = GetCurrentChildId(btParams.FrameThreadSafe, btParams.Agent);
                SetCurrentChildId(btParams.FrameThreadSafe, currentChild + 1, btParams.Agent);
            }
            else
            {
                SetCurrentChildId(btParams.FrameThreadSafe, _childInstances.Length, btParams.Agent);

                // If the child succeeded, then we already know that this sequence succeeded, so we can force it
                SetStatus(btParams.FrameThreadSafe, BTStatus.Success, btParams.Agent);

                // Trigger the debugging callbacks
                BotSDKEditorEvents.BT.InvokeOnNodeSuccess(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
                BotSDKEditorEvents.BT.InvokeOnNodeExit(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
            }

            return true;
        }
    }
}