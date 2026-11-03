using System;

namespace Quantum
{
    /// <summary>
    /// This Decorator node's purpose is to "hijack" the <see cref="CheckConditions(BTParams, ref AIContext)"/> of the next Decorator node and execute it, reverting the result.
    /// </summary>
    [BotSDKTooltip(Text = "Negates the result of the next Decorator node if there are any. Else, result is True.")]
    [Serializable]
    public unsafe class BTNegate : BTDecorator
    {
        // This node executes the next Decorator condition check
        // Due to this, it is necessary for this node to force the next node's OnEnter and OnStartedRunning in case CheckConditions needs anything that is initialized in these

        public override void OnEnter(BTParams btParams, ref AIContext aiContext)
        {
            var child = btParams.FrameThreadSafe.FindAsset<BTNode>(Child.Id);
            if (child is BTDecorator)
            {
                child.OnEnter(btParams, ref aiContext);
            }
        }

        public override void OnStartedRunning(BTParams btParams, ref AIContext aiContext)
        {
            var child = btParams.FrameThreadSafe.FindAsset<BTNode>(Child.Id);
            if (child is BTDecorator)
            {
                child.OnStartedRunning(btParams, ref aiContext);
            }
        }

        public override bool CheckConditions(BTParams btParams, ref AIContext aiContext)
        {
            if (Child == null)
            {
                return true;
            }

            var child = btParams.FrameThreadSafe.FindAsset<BTNode>(Child.Id);
            if (child is BTDecorator childDecorator)
            {
                return !childDecorator.CheckConditions(btParams, ref aiContext);
            }
            else
            {
                return true;
            }
        }

        protected override BTStatus OnUpdate(BTParams btParams, ref AIContext aiContext)
        {
            // As this node already executes CheckConditions for the next BT node IF it is a Decorator, then the next node to execute is not the next Decorator, but rather it's child
            if (Child == null)
            {
                return BTStatus.Success;
            }

            var child = btParams.Frame.FindAsset<BTNode>(Child.Id);
            var nextExecuteNode = child;
            if (child is BTDecorator childDecorator)
            {
                var nestedChild = btParams.FrameThreadSafe.FindAsset<BTNode>(childDecorator.Child.Id);
                nextExecuteNode = nestedChild;
            }

            if (CheckConditions(btParams, ref aiContext) == true)
            {
                BotSDKEditorEvents.BT.InvokeOnDecoratorChecked(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId, true);

                // Propagate the editor event to the child too as it won't have its OnUpdate executed
                if (child is BTDecorator)
                {
                    BotSDKEditorEvents.BT.InvokeOnDecoratorChecked(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, child.Guid.Value, child.CircuitId, true);
                }

                if (nextExecuteNode != null)
                {
                    var childResult = nextExecuteNode.Execute(btParams, ref aiContext);
                    if (childResult == BTStatus.Abort)
                    {
                        EvaluateAbortNode(btParams);
                        SetStatus(btParams.FrameThreadSafe, BTStatus.Abort, btParams.Agent);
                        return BTStatus.Abort;
                    }

                    return childResult;
                }

                return BTStatus.Success;
            }

            // Propagate the editor event to the child too as it won't have its OnUpdate executed
            BotSDKEditorEvents.BT.InvokeOnDecoratorChecked(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId, false);
            if (child is BTDecorator)
            {
                BotSDKEditorEvents.BT.InvokeOnDecoratorChecked(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, child.Guid.Value, child.CircuitId, false);
            }

            return BTStatus.Failure;
        }
    }
}