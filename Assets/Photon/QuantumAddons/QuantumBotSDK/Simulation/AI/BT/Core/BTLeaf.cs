using System;

namespace Quantum
{
    public unsafe abstract partial class BTLeaf : BTNode
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        public AssetRef<BTService>[] Services;
        public BTService[] ServiceInstances
        {
            get
            {
                return _serviceInstances;
            }
        }

        // ========== BTNode INTERFACE ================================================================================

        public override BTNodeType NodeType
        {
            get
            {
                return BTNodeType.Leaf;
            }
        }

        // ========== PROTECTED MEMBERS ===============================================================================

        [NonSerialized] protected BTService[] _serviceInstances;

        // ========== BTDecorator INTERFACE ===========================================================================

        internal override unsafe void InitInternal(BTParams btParams, ref AIContext aiContext)
        {
            base.InitInternal(btParams, ref aiContext);

            if (Services != null)
            {
                for (int i = 0; i < Services.Length; i++)
                {
                    BTService service = btParams.FrameThreadSafe.FindAsset<BTService>(Services[i].Id);
                    service.InitInternal(btParams, ref aiContext);
                    service.Init(btParams, ref aiContext);
                }
            }
        }

        public override void OnStartedRunning(BTParams btParams, ref AIContext aiContext)
        {
            var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRef<BTService>>(btParams.Agent->ActiveServices);
            for (int i = 0; i < _serviceInstances.Length; i++)
            {
                _serviceInstances[i].OnEnter(btParams, ref aiContext);
                activeServicesList.Add(Services[i]);
            }
        }

        public override void OnEnter(BTParams btParams, ref AIContext aiContext)
        {
            base.OnEnter(btParams, ref aiContext);

            // Some Service nodes execute their Update logic upon its owner's OnEnter
            for (Int32 i = 0; i < _serviceInstances.Length; i++)
            {
                _serviceInstances[i].OnOwnerEntered(btParams, ref aiContext);
            }

            BotSDKEditorEvents.BT.InvokeOnNodeEnter(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
        }

        public override void OnExit(BTParams btParams, ref AIContext aiContext)
        {
            base.OnExit(btParams, ref aiContext);

            var activeServicesList = btParams.FrameThreadSafe.ResolveList<AssetRef<BTService>>(btParams.Agent->ActiveServices);
            for (Int32 i = 0; i < _serviceInstances.Length; i++)
            {
                activeServicesList.Remove(Services[i]);
            }

            BotSDKEditorEvents.BT.InvokeOnNodeExit(aiContext.AgentEntity, btParams.Agent->Tree.Id.Value, Guid.Value, CircuitId);
        }

        // ========== AssetObject INTERFACE ===========================================================================

        public override void Loaded(IResourceManager resourceManager)
        {
            base.Loaded(resourceManager);

            // Cache the service assets links
            _serviceInstances = new BTService[Services == null ? 0 : Services.Length];
            if (Services != null)
            {
                for (int i = 0; i < Services.Length; i++)
                {
                    _serviceInstances[i] = (BTService)resourceManager.GetAsset(Services[i].Id);
                }
            }
        }
    }
}