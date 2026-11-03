using System;

namespace Quantum
{
    public unsafe abstract partial class BTComposite : BTNode
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        public AssetRef<BTNode>[] Children;
        public AssetRef<BTService>[] Services;
        [BotSDKHidden] public AssetRef<BTNode> TopmostDecorator;

        public BTDataIndex CurrentChildIndex;

        [BotSDKTooltip(Text = "Should this node's Decorators be checked every tick?")]
        public bool IsDynamic;

        public BTNode[] ChildInstances
        {
            get
            {
                return _childInstances;
            }
        }

        public BTService[] ServiceInstances
        {
            get
            {
                return _serviceInstances;
            }
        }

        public override BTNodeType NodeType
        {
            get
            {
                return BTNodeType.Composite;
            }
        }

        // ========== PROTECTED MEMBERS ===============================================================================

        [NonSerialized] protected BTNode[] _childInstances;
        [NonSerialized] protected BTService[] _serviceInstances;
        [NonSerialized] protected BTNode _topmostDecoratorInstance;

        // ========== BTNode INTERFACE ================================================================================

        internal override void InitInternal(BTParams btParams, ref AIContext aiContext)
        {
            base.InitInternal(btParams, ref aiContext);

            for (Int32 i = 0; i < Services.Length; i++)
            {
                BTService service = btParams.FrameThreadSafe.FindAsset<BTService>(Services[i].Id);
                service.InitInternal(btParams, ref aiContext);
                service.Init(btParams, ref aiContext);
            }
        }

        public override void OnEnter(BTParams btParams, ref AIContext aiContext)
        {
            BotSDKEditorEvents.BT.InvokeOnNodeEnter(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
            SetCurrentChildId(btParams.FrameThreadSafe, 0, btParams.Agent);

            // Some Service nodes execute their Update logic upon its onwer's OnEnter
            for (Int32 i = 0; i < _serviceInstances.Length; i++)
            {
                _serviceInstances[i].OnOwnerEntered(btParams, ref aiContext);
            }
        }

        public override void OnStartedRunning(BTParams btParams, ref AIContext aiContext)
        {
            var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRef<BTService>>(btParams.Agent->ActiveServices);

            for (Int32 i = 0; i < _serviceInstances.Length; i++)
            {
                _serviceInstances[i].OnEnter(btParams, ref aiContext);

                activeServicesList.Add(Services[i]);
            }

            if (IsDynamic == true)
            {
                var dynamicComposites = btParams.FrameThreadSafe.ResolveList<AssetRef<BTComposite>>(btParams.Agent->DynamicComposites);
                dynamicComposites.Add(this);
            }
        }

        public override void OnReset(BTParams btParams, ref AIContext aiContext)
        {
            base.OnReset(btParams, ref aiContext);

            for (Int32 i = 0; i < _childInstances.Length; i++)
                _childInstances[i].OnReset(btParams, ref aiContext);
        }

        public override void OnExit(BTParams btParams, ref AIContext aiContext)
        {
            base.OnExit(btParams, ref aiContext);

            BotSDKEditorEvents.BT.InvokeOnNodeExit(btParams.Entity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);

            var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRef<BTService>>(btParams.Agent->ActiveServices);
            for (Int32 i = 0; i < _serviceInstances.Length; i++)
            {
                activeServicesList.Remove(Services[i]);
            }

            if (IsDynamic == true)
            {
                var dynamicComposites = btParams.FrameThreadSafe.ResolveList<AssetRef<BTComposite>>(btParams.Agent->DynamicComposites);
                dynamicComposites.Remove(this);
            }
        }

        public override bool OnDynamicRun(BTParams btParams, ref AIContext aiContext)
        {
            if (_topmostDecoratorInstance != null)
            {
                return _topmostDecoratorInstance.OnDynamicRun(btParams, ref aiContext);
            }

            return true;
        }

        // ========== AssetObject INTERFACE ===========================================================================

        public override void Loaded(IResourceManager resourceManager)
        {
            base.Loaded(resourceManager);

            // Cache the child assets links
            if (Children != null)
            {
                _childInstances = new BTNode[Children.Length];
                for (Int32 i = 0; i < Children.Length; i++)
                {
                    _childInstances[i] = (BTNode)resourceManager.GetAsset(Children[i].Id);
                    _childInstances[i].Parent = this;
                    _childInstances[i].ParentIndex = i;
                }
            }

            if (Services != null)
            {
                // Cache the service assets links
                _serviceInstances = new BTService[Services.Length];
                for (Int32 i = 0; i < Services.Length; i++)
                {
                    _serviceInstances[i] = (BTService)resourceManager.GetAsset(Services[i].Id);
                }
            }

            if (TopmostDecorator != null)
            {
                _topmostDecoratorInstance = (BTDecorator)resourceManager.GetAsset(TopmostDecorator.Id);
            }
        }

        // ========== PUBLIC METHODS ==================================================================================

        public void AbortNodes(BTParams btParams, ref AIContext aiContext, BTAbort abortType, Int32 firstIndex = 0)
        {
            for (int i = firstIndex; i < _childInstances.Length; i++)
            {
                _childInstances[i].SetStatus(btParams.FrameThreadSafe, BTStatus.Abort, btParams.Agent);
                _childInstances[i].OnAbortInternal(btParams, ref aiContext, abortType);
            }
        }

        // ========== INTERNAL METHODS ================================================================================

        public Int32 GetCurrentChildId(FrameThreadSafe frame, BTAgent* agent)
        {
            Byte currentChild = (Byte)agent->GetIntData(frame, CurrentChildIndex.Index);
            return currentChild;
        }

        public void SetCurrentChildId(FrameThreadSafe frame, Int32 currentIndex, BTAgent* agent)
        {
            agent->SetIntData(frame, currentIndex, CurrentChildIndex.Index);
        }
    }
}