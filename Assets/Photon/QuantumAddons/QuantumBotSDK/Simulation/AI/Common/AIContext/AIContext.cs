namespace Quantum
{
    public partial struct AIContextUser
    {
    }

    public unsafe static class AIContextExtensions
    {
        public static ref AIContextUser UserData(this ref AIContext aiContext)
        {
            return ref *(AIContextUser*)aiContext.UserData;
        }
    }

    public unsafe partial struct AIContext
    {
        private EntityRef _agentEntity;
        private HFSMAgent* _hfsmAgent;
        private BTAgent* _btAgent;
        private UTAgent* _utAgent;
        private AIBlackboardComponent* _blackboard;
        private AIConfig _aiConfig;

        /// <summary>
        /// The specific entity which has the AI agent component.
        /// </summary>
        public EntityRef AgentEntity => _agentEntity;

        /// <summary>
        /// Pointer to an HFSM agent. Assigned via <see cref="SetupHFSMFromEntity"/>.
        /// </summary>
        public HFSMAgent* HFSMAgent => _hfsmAgent;

        /// <summary>
        /// Pointer to a BT agent. Assigned via <see cref="SetupBTFromEntity"/>.
        /// </summary>
        public BTAgent* BTAgent => _btAgent;

        /// <summary>
        /// Pointer to a UT agent. Assigned via <see cref="SetupUTFromEntity"/>.
        /// </summary>
        public UTAgent* UTAgent => _utAgent;

        /// <summary>
        /// Pointer to an AI Blackboard component. Assigned alongside with via the agents API, or directly via <see cref="SetBlackboard(AIBlackboardComponent*)"/>.
        /// </summary>
        public AIBlackboardComponent* Blackboard => _blackboard;

        /// <summary>
        /// Pointer to an AI Config asset. Assigned alongside with via the agents API, or directly via <see cref="SetAIConfig(AIConfig)"/>.
        /// </summary>
        public AIConfig AIConfig => _aiConfig;

        /// <summary>
        /// User data pointer. Free for user code to assign.
        /// </summary>
        public void* UserData { get; private set; }

        // INTERNAL
        internal void SetupHFSMFromEntity(FrameThreadSafe frame, EntityRef entity)
        {
            Assert.Check(frame.Has<HFSMAgent>(entity), "Entity does not contain HFSMAgent.");

            _agentEntity = entity;

            _hfsmAgent = frame.GetPointer<HFSMAgent>(entity);

            _aiConfig = frame.FindAsset<AIConfig>(_hfsmAgent->Config);

            TrySetupBlackboard(frame, entity);
        }

        internal void SetupBTFromEntity(FrameThreadSafe frame, EntityRef entity)
        {
            Assert.Check(frame.Has<BTAgent>(entity), "Entity does not contain BTAgent.");

            _agentEntity = entity;

            _btAgent = frame.GetPointer<BTAgent>(entity);

            _aiConfig = frame.FindAsset<AIConfig>(_btAgent->Config);

            TrySetupBlackboard(frame, entity);
        }

        internal void SetupUTFromEntity(FrameThreadSafe frame, EntityRef entity)
        {
            Assert.Check(frame.Has<UTAgent>(entity), "Entity does not contain UTAgent.");

            _agentEntity = entity;

            _utAgent = frame.GetPointer<UTAgent>(entity);

            _aiConfig = frame.FindAsset<AIConfig>(_utAgent->Config);

            TrySetupBlackboard(frame, entity);
        }

        private void TrySetupBlackboard(FrameThreadSafe frame, EntityRef entity)
        {
            if (frame.Has<AIBlackboardComponent>(entity) == true)
            {
                _blackboard = frame.GetPointer<AIBlackboardComponent>(entity);
            }
        }

        // USER

        /// <summary>
        /// Sets custom user data for adding game specific AI context.
        /// </summary>
        /// <param name="userData"></param>
        public void SetUserData(void* userData)
        {
            Assert.Check(userData != null, "UserData should not be null.");

            UserData = userData;
        }

        // BLACKBOARD

        /// <summary>
        /// Sets a blackboard into the AI context, which is accessible through the entire agent API.
        /// </summary>
        public void SetBlackboard(AIBlackboardComponent* blackboard)
        {
            Assert.Check(blackboard != null, "Blackboard should not be null.");

            _blackboard = blackboard;
        }

        // AI CONFIG

        /// <summary>
        /// Sets a blackboard into the AI context, which is accessible through the entire agent API.
        /// </summary>
        public void SetAIConfig(AIConfig aiConfig)
        {
            Assert.Check(aiConfig != null, "AIConfig is null.");

            _aiConfig = aiConfig;
        }

        // HFSM

        /// <summary>
        /// Sets an HFSMAgent into the AI context, which is accessible through the entire agent API.
        /// </summary>
        public void SetHFSMAgent(HFSMAgent* hfsmAgent, EntityRef agentEntity)
        {
            Assert.Check(hfsmAgent != null, "HFSMAgent should not be null.");

            _hfsmAgent = hfsmAgent;
            _agentEntity = agentEntity;
        }

        /// <summary>
        /// Sets an HFSMAgent and a Blackboard into the AI context, which is accessible through the entire agent API.
        /// </summary>
        public void SetHFSMAgentAndBlackboard(HFSMAgent* hfsmAgent, EntityRef agentEntity, AIBlackboardComponent* blackboard)
        {
            Assert.Check(hfsmAgent != null, "HFSMAgent should not be null.");
            Assert.Check(blackboard != null, "Blackboard should not be null.");

            _hfsmAgent = hfsmAgent;
            _agentEntity = agentEntity;
            _blackboard = blackboard;
        }

        // BT

        /// <summary>
        /// Sets an BTAgent into the AI context, which is accessible through the entire agent API.
        /// </summary>
        public void SetBTAgent(BTAgent* btAgent, EntityRef agentEntity)
        {
            Assert.Check(btAgent != null, "BTAgent should not be null.");

            _agentEntity = agentEntity;
            _btAgent = btAgent;
        }

        /// <summary>
        /// Sets an BTAgent and a Blackboard into the AI context, which is accessible through the entire agent API.
        /// </summary>
        public void SetBTAgentAndBlackboard(BTAgent* btAgent, EntityRef agentEntity, AIBlackboardComponent* blackboard)
        {
            Assert.Check(btAgent != null, "BTAgent should not be null.");
            Assert.Check(blackboard != null, "Blackboard should not be null.");

            _btAgent = btAgent;
            _agentEntity = agentEntity;
            _blackboard = blackboard;
        }

        // UT

        /// <summary>
        /// Sets an UTAgent into the AI context, which is accessible through the entire agent API.
        /// </summary>
        public void SetUTAgent(UTAgent* utAgent, EntityRef agentEntity)
        {
            Assert.Check(utAgent != null, "UTAgent should not be null.");

            _agentEntity = agentEntity;
            _utAgent = utAgent;
        }

        /// <summary>
        /// Sets an UTAgent and a Blackboard into the AI context, which is accessible through the entire agent API.
        /// </summary>
        public void SetUTAgentAndBlackboard(UTAgent* utAgent, EntityRef agentEntity, AIBlackboardComponent* blackboard)
        {
            Assert.Check(utAgent != null, "UTAgent should not be null.");
            Assert.Check(blackboard != null, "Blackboard should not be null.");

            _utAgent = utAgent;
            _agentEntity = agentEntity;
            _blackboard = blackboard;
        }
    }
}