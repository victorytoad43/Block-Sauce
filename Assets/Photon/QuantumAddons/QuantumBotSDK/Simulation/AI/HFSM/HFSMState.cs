using Photon.Deterministic;
using System;

namespace Quantum
{
    public unsafe partial class HFSMState : BotSDKAsset
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        [BotSDKHidden]
        public string Label;

        [BotSDKHidden]
        public byte Index;

        public AssetRef<AIAction>[] OnUpdateLinks;
        public AssetRef<AIAction>[] OnEnterLinks;
        public AssetRef<AIAction>[] OnExitLinks;
        public HFSMTransition[] Transitions;

        public AssetRef<HFSMState>[] ChildrenLinks;
        public AssetRef<HFSMState> ParentLink;
        public int Level;

        [NonSerialized]
        public AIAction[] OnUpdate;
        [NonSerialized]
        public AIAction[] OnEnter;
        [NonSerialized]
        public AIAction[] OnExit;
        [NonSerialized]
        public HFSMState[] Children;
        [NonSerialized]
        public HFSMState Parent;

        // ========== AssetObject INTERFACE ===========================================================================
        public override void Loaded(IResourceManager resourceManager)
        {
            base.Loaded(resourceManager);

            OnUpdate = new AIAction[OnUpdateLinks == null ? 0 : OnUpdateLinks.Length];
            if (OnUpdateLinks != null)
            {
                for (Int32 i = 0; i < OnUpdateLinks.Length; i++)
                {
                    OnUpdate[i] = (AIAction)resourceManager.GetAsset(OnUpdateLinks[i].Id);
                }
            }
            OnEnter = new AIAction[OnEnterLinks == null ? 0 : OnEnterLinks.Length];
            if (OnEnterLinks != null)
            {
                for (Int32 i = 0; i < OnEnterLinks.Length; i++)
                {
                    OnEnter[i] = (AIAction)resourceManager.GetAsset(OnEnterLinks[i].Id);
                }
            }
            OnExit = new AIAction[OnExitLinks == null ? 0 : OnExitLinks.Length];
            if (OnExitLinks != null)
            {
                for (Int32 i = 0; i < OnExitLinks.Length; i++)
                {
                    OnExit[i] = (AIAction)resourceManager.GetAsset(OnExitLinks[i].Id);
                }
            }

            Children = new HFSMState[ChildrenLinks == null ? 0 : ChildrenLinks.Length];
            if (ChildrenLinks != null)
            {
                for (Int32 i = 0; i < ChildrenLinks.Length; i++)
                {
                    Children[i] = (HFSMState)resourceManager.GetAsset(ChildrenLinks[i].Id);
                }
            }

            Parent = (HFSMState)resourceManager.GetAsset(ParentLink.Id);
            if (Transitions != null)
            {
                for (int i = 0; i < Transitions.Length; i++)
                {
                    Transitions[i].Setup(resourceManager);
                }
            }
        }

        // ========== INTERNAL METHODS ================================================================================

        internal Boolean Execute(FrameThreadSafe frame, FP deltaTime, EntityRef entity, ref AIContext aiContext)
        {
            var hfsmData = &aiContext.HFSMAgent->Data;

            // Make sure the HFSM Root asset is loaded upon Update so it guarantees that late joiners will load and trigger all Loaded callbacks needed,
            // which then caches references to other assets. The root loads all the referenced root states which loads all the child states in sequence, transitions and so on
            var root = frame.FindAsset<HFSMRoot>(hfsmData->Root.Id);

            HFSMState parent = Parent;
            Boolean transition = false;

            if (parent != null)
            {
                transition = parent.Execute(frame, deltaTime, entity, ref aiContext);
            }

            if (transition == true)
                return true;

            *hfsmData->Timers.GetPointer(Level) += deltaTime;

            ExecuteActions(frame, entity, ref aiContext, OnUpdate);
            return CheckTransitions(frame, Transitions, entity, 0, ref aiContext);
        }

        internal Boolean Event(FrameThreadSafe frame, EntityRef entity, Int32 eventInt, ref AIContext aiContext)
        {
            // Make sure the HFSM Root asset is loaded
            var root = frame.FindAsset<HFSMRoot>(aiContext.HFSMAgent->Data.Root.Id);

            HFSMState p = Parent;
            Boolean transition = false;
            if (p != null)
            {
                transition = p.Event(frame, entity, eventInt, ref aiContext);
            }

            if (transition)
            {
                return true;
            }

            return CheckTransitions(frame, Transitions, entity, eventInt, ref aiContext);
        }

        internal void EnterState(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            var hfsmData = &aiContext.HFSMAgent->Data;

            // Make sure the HFSM Root asset is loaded
            var root = frame.FindAsset<HFSMRoot>(hfsmData->Root.Id);

            *hfsmData->Timers.GetPointer(Level) = FP._0;

            ExecuteActions(frame, entity, ref aiContext, OnEnter);

            if (hfsmData->UsesStateHistory == true)
            {
                hfsmData->StatesHistory = (byte)(hfsmData->StatesHistory | 1 << Index);
            }

            if (Children != null && Children.Length > 0)
            {
                HFSMState child;
                if (hfsmData->UsesStateHistory == false)
                {
                    child = Children[0];
                }
                else
                {
                    child = FindChildInHistory(frame, ref aiContext);
                }

                HFSMManager.ThreadSafe.ChangeState(child, frame, entity, "", ref aiContext);
            }
        }

        // Returns the last child state updated before leaving this state
        private HFSMState FindChildInHistory(FrameThreadSafe frame, ref AIContext aiContext)
        {
            var hfsmData = &aiContext.HFSMAgent->Data;

            for (int i = 0; i < Children.Length; i++)
            {
                if ((hfsmData->StatesHistory & 1 << Children[i].Index) != 0)
                {
                    return Children[i];
                }
            }

            return Children[0];
        }

        internal void ExitState(HFSMState nextState, FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
        {
            var hfsmData = &aiContext.HFSMAgent->Data;

            // Make sure the HFSM Root asset is loaded
            var root = frame.FindAsset<HFSMRoot>(hfsmData->Root.Id);

            if (nextState != null && nextState.IsChildOf(this) == true)
                return;

            if (hfsmData->UsesStateHistory == true && IsSiblingOf(frame, nextState, ref aiContext) == true)
            {
                hfsmData->StatesHistory = (byte)(hfsmData->StatesHistory ^ 1 << Index);
            }

            ExecuteActions(frame, entity, ref aiContext, OnExit);
            Parent?.ExitState(nextState, frame, entity, ref aiContext);
        }

        internal bool IsChildOf(HFSMState state)
        {
            if (Parent == null)
                return false;

            if (Parent == state)
                return true;

            return Parent.IsChildOf(state);
        }

        internal bool IsSiblingOf(FrameThreadSafe frame, HFSMState state, ref AIContext aiContext)
        {
            HFSMState[] siblings;

            if (Parent != null)
            {
                siblings = Parent.Children;
            }
            else
            {
                siblings = frame.FindAsset<HFSMRoot>(aiContext.HFSMAgent->Data.Root.Id).States;
            }

            for (int i = 0; i < siblings.Length; i++)
            {
                if (siblings[i] == state) return true;
            }

            return false;
        }

        // ========== PRIVATE METHODS =================================================================================

        private void ExecuteActions(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext, AIAction[] actions)
        {
            var hfsmData = &aiContext.HFSMAgent->Data;
            hfsmData->StateTimer = *hfsmData->Timers.GetPointer(Level);

            for (int i = 0; i < actions.Length; i++)
            {
                actions[i].Execute(frame, entity, ref aiContext);
            }
        }

        private bool CheckTransitions(FrameThreadSafe frame, HFSMTransition[] transitions, EntityRef entity, int eventKey, ref AIContext aiContext, int depth = 0)
        {
            var hfsmData = &aiContext.HFSMAgent->Data;
            hfsmData->StateTimer = *hfsmData->Timers.GetPointer(Level);

            // Just to avoid accidental loops
            if (depth == 10)
                return false;

            if (transitions == null)
                return false;

            for (int i = 0; i < transitions.Length; i++)
            {
                var transition = transitions[i];

                if (transition.State == null && transition.TransitionSet == null)
                    continue;

                // Only consider evaluating the event if this transition HAS a event as requisite (EventKey != 0)
                if (transition.EventKey != 0 && transition.EventKey != eventKey)
                    continue;

                if (transition.Decision != null && transition.Decision.Decide(frame, entity, ref aiContext) == false)
                    continue;

                if (transition.State != null)
                {
                    HFSMManager.ThreadSafe.ChangeState(transition.State, frame, entity, transition.Id, ref aiContext);
                    return true;
                }
                else if (CheckTransitions(frame, transition.TransitionSet.Transitions, entity, eventKey, ref aiContext, depth + 1) == true)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
