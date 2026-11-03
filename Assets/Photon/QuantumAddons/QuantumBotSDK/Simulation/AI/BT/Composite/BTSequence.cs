using System;

namespace Quantum
{
    /// <summary>
    /// The sequence task is similar to an and operation. It will return failure as soon as one of its child tasks return failure.
    /// If a child task returns success then it will sequentially run the next task. If all child tasks return success then it will return success.
    /// </summary>
    [Serializable]
    [BotSDKTooltip(Text = "Succeeds only if all of its child nodes succeed.")]
    public unsafe partial class BTSequence : BTComposite
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

                if (status == BTStatus.Abort)
                {
                    if (btParams.Agent->IsAborting == true)
                    {
                        return BTStatus.Abort;
                    }
                    else
                    {
                        return BTStatus.Failure;
                    }
                }

                if (status == BTStatus.Success)
                {
                    SetCurrentChildId(btParams.FrameThreadSafe, currentChildId + 1, btParams.Agent);
                }
                else
                {
                    break;
                }
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
                SetCurrentChildId(btParams.FrameThreadSafe, _childInstances.Length, btParams.Agent);

                // If the child failed, then we already know that this sequence failed, so we can force it
                SetStatus(btParams.FrameThreadSafe, BTStatus.Failure, btParams.Agent);

                // Trigger the debugging callbacks
                BotSDKEditorEvents.BT.InvokeOnNodeFailure(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
                BotSDKEditorEvents.BT.InvokeOnNodeExit(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
            }
            else
            {
                var currentChild = GetCurrentChildId(btParams.FrameThreadSafe, btParams.Agent);
                SetCurrentChildId(btParams.FrameThreadSafe, currentChild + 1, btParams.Agent);
            }

            return true;
        }
    }
}