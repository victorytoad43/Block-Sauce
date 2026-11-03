using Quantum.Core;
using System;

namespace Quantum
{

    public unsafe abstract partial class BTNode : BotSDKAsset
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        [BotSDKHidden] public String Label;
        [BotSDKHidden] public Int32 Id;

        // ========== INTERNAL MEMBERS ================================================================================

        [NonSerialized] internal BTNode Parent;
        [NonSerialized] internal Int32 ParentIndex;

        // ========== BTNode INTERFACE ================================================================================

        public abstract BTNodeType NodeType { get; }

        /// <summary>
        /// Called once, for every Node, when the BT is being initialized. Meant to be used internally only for allocating relevant stuff for the BT to run.
        /// </summary>
        internal virtual void InitInternal(BTParams btParams, ref AIContext aiContext)
        {
            var statusList = btParams.Frame.ResolveList(btParams.Agent->NodesStatus);
            statusList.Add(0);
        }

        /// <summary>
        /// Called once, for every Node, when the BT is being initialized.
        /// </summary>
        // PS: this is meant for USER extension and they might not call base.Init(), so do not add any code here
        public virtual void Init(BTParams btParams, ref AIContext aiContext) { }

        /// <summary>
        /// Called whenever the BT execution includes this node as part of the current context
        /// </summary>
        /// <param name="btParams"></param>
        public virtual void OnEnter(BTParams btParams, ref AIContext aiContext) { }

        public virtual void OnStartedRunning(BTParams btParams, ref AIContext aiContext) { }

        /// <summary>
        /// Called when traversing the tree upwards and the node is already finished with its job.
        /// Used by Composites and Leafs to remove their Services from the list of active services
        /// as it is not anymore part of the current subtree.
        /// Dynamic Composites also remove themselves
        /// </summary>
        /// <param name="btParams"></param>
        public virtual void OnExit(BTParams btParams, ref AIContext aiContext) { }

        internal virtual void OnAbortInternal(BTParams btParams, ref AIContext aiContext, BTAbort abortType)
        {
            OnAbort(btParams, ref aiContext, abortType);
            AbortBranch(btParams, ref aiContext, this, abortType);
        }

        public virtual void OnAbort(BTParams btParams, ref AIContext aiContext, BTAbort abortType)
        {
        }

        private void AbortBranch(BTParams btParams, ref AIContext aiContext, BTNode node, BTAbort abortType)
        {
            // Set the node status to Inactive, as it was running but bailed out due to a dynamic Decorator
            // Setting it to Inactive because it neither failed nor succeeded, it just didn't finish its job
            //if (node.GetStatus(btParams.Frame, btParams.Agent) == BTStatus.Running)
            {
                SetStatus(btParams.Frame, BTStatus.Abort, btParams.Agent);
                BotSDKEditorEvents.BT.InvokeOnDynamicFailure(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, node.Guid.Value, node.CircuitId);
            }

            // Propagate the exiting chain
            if (node.NodeType == BTNodeType.Composite)
            {
                var composite = (BTComposite)node;
                for (int i = 0; i < composite.ChildInstances.Length; i++)
                {
                    var child = composite.ChildInstances[i];
                    if (child.GetStatus(btParams.FrameThreadSafe, btParams.Agent) == BTStatus.Running)
                    {
                        child.OnAbortInternal(btParams, ref aiContext, abortType);
                    }
                }
            }
            else if (node.NodeType == BTNodeType.Decorator)
            {
                var decorator = (BTDecorator)node;
                decorator.ChildInstance.OnAbortInternal(btParams, ref aiContext, abortType);
            }
        }

        /// <summary>
        /// Called when getting out of a sub-branch and this node is being discarded
        /// </summary>
        /// <param name="btParams"></param>
        public unsafe virtual void OnReset(BTParams btParams, ref AIContext aiContext)
        {
            SetStatus(btParams.FrameThreadSafe, BTStatus.Inactive, btParams.Agent);
            BotSDKEditorEvents.BT.InvokeOnNodeInactive(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
        }

        /// <summary>
        /// Used by Decorators to evaluate if a condition succeeds or not.
        /// Upon success, allow the flow to continue.
        /// Upon failure, blocks the execution so another path is taken
        /// </summary>
        /// <param name="btParams"></param>
        /// <returns></returns>
        public virtual Boolean CheckConditions(BTParams btParams, ref AIContext aiContext)
        {
            return false;
        }

        public virtual Boolean OnDynamicRun(BTParams btParams, ref AIContext aiContext)
        {
            return true;
        }

        /// <summary>
        /// Called every tick while this Node is part of the current sub-tree.
        /// Returning "Success/Failure" will make the tree continue its execution.
        /// Returning "Running" will store this Node as the Current Node and re-execute it on the next frame
        /// unless something else interrputs
        /// </summary>
        /// <param name="btParams"></param>
        /// <returns></returns>
        protected abstract BTStatus OnUpdate(BTParams btParams, ref AIContext aiContext);

        /// <summary>
        /// When a Composite node is Updated, it only increase the current child updated
        /// when the child results in either FAIL/SUCCESS. So we need this callback
        /// to be used when the child was RUNNING and then had some result, to properly increase the current 
        /// child ID
        /// </summary>
        /// <returns> If the node should repeat it's execution again (as to allow for resuming a previous execution) </returns>
        public virtual bool ChildCompletedRunning(BTParams btParams, BTStatus childResult, ref AIContext aiContext)
        {
            return false;
        }

        // ========== PUBLIC METHODS ==================================================================================

        // -- STATUS --
        public BTStatus GetStatus(FrameBase frame, BTAgent* agent)
        {
            return GetStatus((FrameThreadSafe)frame, agent);
        }

        public void SetStatus(FrameBase frame, BTStatus status, BTAgent* agent)
        {
            SetStatus((FrameThreadSafe)frame, status, agent);
        }

        public BTStatus GetStatus(FrameThreadSafe frame, BTAgent* agent)
        {
            var nodesAndStatus = frame.ResolveList(agent->NodesStatus);
            return (BTStatus)nodesAndStatus[Id];
        }

        public void SetStatus(FrameThreadSafe frame, BTStatus status, BTAgent* agent)
        {
            var nodesAndStatus = frame.ResolveList(agent->NodesStatus);
            nodesAndStatus[Id] = (Byte)status;
        }

        public void EvaluateAbortNode(BTParams btParams)
        {
            if (btParams.Agent->AbortNodeId == Id)
            {
                btParams.Agent->AbortNodeId = 0;
            }
        }

        public BTStatus Execute(BTParams btParams, ref AIContext aiContext, bool continuingAbort = false)
        {
            var oldStatus = GetStatus(btParams.FrameThreadSafe, btParams.Agent);

            if (oldStatus == BTStatus.Success || oldStatus == BTStatus.Failure)
            {
                return oldStatus;
            }

            if (oldStatus == BTStatus.Abort)
            {
                if (btParams.Agent->IsAborting == true)
                {
                    EvaluateAbortNode(btParams);
                }

                return oldStatus;
            }

            // If this node was inactive, this means that we're entering on it for the first time, so we call OnEnter
            // An exception from this rule is when we chose this node to continue an abort process. In that case,
            // we already executed OnEnter before, so we don't repeat it
            if (oldStatus == BTStatus.Inactive && continuingAbort == false)
            {
                OnEnter(btParams, ref aiContext);

                // OnEnter might trigger a Service logic if it is marked as RunOnEnter = true
                // This means that, at this point, it is possible that a Self Abort routine starts
                // If it does, then this node should not be further updated. It should return the Abort status and Exit right away
                if (btParams.Agent->IsAborting)
                {
                    SetStatus(btParams.FrameThreadSafe, BTStatus.Abort, btParams.Agent);
                    OnExit(btParams, ref aiContext);
                    return BTStatus.Abort;
                }
            }

            var newStatus = BTStatus.Failure;
            try
            {
                newStatus = OnUpdate(btParams, ref aiContext);

                // If the agent has been destroyed during it's own execution, return right away
                if (btParams.Frame.Exists(btParams.Entity) == false)
                {
                    return BTStatus.Failure;
                }

                // It is possible that a Self Abort routine starts after this node's update as it can trigger Reactive Decorators logic
                // If the Abort starts, then this node should result in Abort and exit
                if (btParams.Agent->IsAborting)
                {
                    newStatus = BTStatus.Abort;
                    OnExit(btParams, ref aiContext);
                }

                if (newStatus == BTStatus.Success)
                {
                    if (oldStatus == BTStatus.Inactive)
                    {
                        OnExit(btParams, ref aiContext);

                        // If the agent has been destroyed during it's own execution, return right away
                        if (btParams.Frame.Exists(btParams.Entity) == false)
                        {
                            return BTStatus.Failure;
                        }
                    }

                    BotSDKEditorEvents.BT.InvokeOnNodeSuccess(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
                    BotSDKEditorEvents.BT.InvokeOnNodeExit(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
                }

                if (newStatus == BTStatus.Failure)
                {
                    if (oldStatus == BTStatus.Inactive)
                    {
                        OnExit(btParams, ref aiContext);

                        // If the agent has been destroyed during it's own execution, return right away
                        if (btParams.Frame.Exists(btParams.Entity) == false)
                        {
                            return BTStatus.Failure;
                        }
                    }

                    BotSDKEditorEvents.BT.InvokeOnNodeFailure(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
                    BotSDKEditorEvents.BT.InvokeOnNodeExit(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
                }

                SetStatus(btParams.FrameThreadSafe, newStatus, btParams.Agent);

                if (oldStatus == BTStatus.Inactive && newStatus == BTStatus.Running)
                {
                    OnStartedRunning(btParams, ref aiContext);
                }

                if (newStatus == BTStatus.Running && NodeType == BTNodeType.Leaf)
                {
                    // If we are a leaf, we can store the current node
                    // We know that there has only one leaf node running at any time, no parallel branches possible
                    btParams.Agent->Current = this;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Exception in Behaviour Tree. Node name: {Label} ({Guid})");
                Log.Exception(e);
            }
            return newStatus;
        }
    }
}