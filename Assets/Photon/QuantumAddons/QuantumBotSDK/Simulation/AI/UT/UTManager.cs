namespace Quantum
{
    using Photon.Deterministic;
    using Quantum.Core;
    using System;

    public unsafe static partial class UTManager
    {
        // ========== PUBLIC METHODS ==================================================================================

        /// <summary>
        /// Initializes the <see cref="UTAgent">, allocating all necessary frame data.
        /// If no UTRoot asset is provided as parameter, it will try to initialize with one already set on the Component, if any.
        /// </summary>
        public static void Init(FrameBase frame, ref AIContext aiContext)
        {
            aiContext.UTAgent->UtilityReasoner.Initialize(frame, aiContext.UTAgent->UtilityReasoner.UTRoot, aiContext.AgentEntity);
        }

        /// <summary>
        /// Initializes the <see cref="UTAgent">, allocating all necessary frame data.
        /// If no UTRoot asset is provided as parameter, it will try to initialize with one already set on the Component, if any.
        /// </summary>
        public static void Init(FrameBase frame, EntityRef entity, UTAgent* utAgent, AssetRef<UTRoot> utRootRef = default)
        {
            utAgent->UtilityReasoner.Initialize(frame, utRootRef, entity);
        }

        /// <summary>
        /// Frees all allocated frame data used by the <see cref="UTAgent"/>.
        /// </summary>
        public static void Free(FrameBase frame, UTAgent* utAgent)
        {
            utAgent->UtilityReasoner.Free(frame);
        }

        /// <summary>
        /// Updates a <see cref="UTAgent"/> which is part of the entity passed as parameter. The Considerations will be evaluated and the most useful will be executed.
        /// </summary>
        public static void Update(FrameBase frame, EntityRef entity)
        {
            AIContext aiContext = new AIContext();
            Update(frame, frame.DeltaTime, entity, ref aiContext);
        }

        /// <summary>
        /// Updates a <see cref="UTAgent"/> which is part of the entity passed as parameter. The Considerations will be evaluated and the most useful will be executed.
        /// </summary>
        public static void Update(FrameBase frame, FP deltaTime, EntityRef entity)
        {
            AIContext aiContext = new AIContext();
            if (entity != default)
            {
                aiContext.SetupUTFromEntity((FrameThreadSafe)frame, entity);
            }

            ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, entity, ref aiContext);
        }

        /// <summary>
        /// Updates a <see cref="UTAgent"/> which is part of the entity passed as parameter. The Considerations will be evaluated and the most useful will be executed.
        /// Allows the use of a specific <see cref="AIContext"/>.
        /// </summary>
        public static void Update(FrameBase frame, EntityRef entity, ref AIContext aiContext)
        {
            Update(frame, frame.DeltaTime, entity, ref aiContext);
        }

        /// <summary>
        /// Updates a <see cref="UTAgent"/> which is part of the entity passed as parameter. The Considerations will be evaluated and the most useful will be executed.
        /// Allows the use of a specific <see cref="AIContext"/>.
        /// </summary>
        public static void Update(FrameBase frame, FP deltaTime, EntityRef entity, ref AIContext aiContext)
        {
            ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, entity, ref aiContext);
        }

        /// <summary>
        /// Updates a <see cref="UTAgent"/> which is not part of an entity but rather lives in Frame.Global. The Considerations will be evaluated and the most useful will be executed.
        /// </summary>
        public static void Update(FrameBase frame, UTAgent* utAgent)
        {
            Update(frame, frame.DeltaTime, utAgent);
        }

        /// <summary>
        /// Updates a <see cref="UTAgent"/> which is not part of an entity but rather lives in Frame.Global. The Considerations will be evaluated and the most useful will be executed.
        /// </summary>
        public static void Update(FrameBase frame, FP deltaTime, UTAgent* utAgent)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetUTAgent(utAgent, agentEntity: default);

            ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, entity: default, ref aiContext);
        }

        /// <summary>
        /// Updates a <see cref="UTAgent"/> which is not part of an entity but rather lives in Frame.Global. The Considerations will be evaluated and the most useful will be executed.
        /// Allows the use of a specific <see cref="AIContext"/>.
        /// </summary>
        public static void Update(FrameBase frame, UTAgent* utAgent, ref AIContext aiContext)
        {
            Update(frame, frame.DeltaTime, utAgent, ref aiContext);
        }

        /// <summary>
        /// Updates a <see cref="UTAgent"/> which is not part of an entity but rather lives in Frame.Global. The Considerations will be evaluated and the most useful will be executed.
        /// Allows the use of a specific <see cref="AIContext"/>.
        /// </summary>
        public static void Update(FrameBase frame, FP deltaTime, UTAgent* utAgent, ref AIContext aiContext)
        {
            aiContext.SetUTAgent(utAgent, agentEntity: default);

            ThreadSafe.Update((FrameThreadSafe)frame, deltaTime, entity: default, ref aiContext);
        }

        #region Obsolete
        /// <summary>
        /// Initializes the Utility Reasoner, allocating all frame data needed.
        /// If no UTRoot asset is passed by parameter, it will try to initialize with one already set on the Component, if any.
        /// </summary>
        [Obsolete("Use the overload that receives a UTAgent* instead.")]
        public static void Init(FrameBase frame, UtilityReasoner* reasoner, AssetRef<UTRoot> utRootRef = default, EntityRef entity = default)
        {
            reasoner->Initialize(frame, utRootRef, entity);
        }

        [Obsolete("Use the overload that receives a UTAgent* instead.")]
        public static void Free(FrameBase frame, UtilityReasoner* reasoner)
        {
            reasoner->Free(frame);
        }

        /// <summary>
        /// Ticks the UtilityReasoner. The Considerations will be evaluated and the most useful will be executed.
        /// It can be agnostic to entities, meaning that it is possible to have a UtilityReasoner as part of Global.
        /// </summary>
        [Obsolete("Use the overload that receives a UTAgent* instead.")]
        public static void Update(FrameBase frame, UtilityReasoner* reasoner, EntityRef entity = default)
        {
            AIContext aiContext = new AIContext();
            if (entity != default)
            {
                aiContext.SetupUTFromEntity((FrameThreadSafe)frame, entity);
            }

            Update(frame, reasoner, ref aiContext, entity);
        }

        /// <summary>
        /// Ticks the UtilityReasoner. The Considerations will be evaluated and the most useful will be executed.
        /// It can be agnostic to entities, meaning that it is possible to have a UtilityReasoner as part of Global.
        /// </summary>
        [Obsolete("Use the overload that receives a UTAgent* instead.")]
        public static void Update(FrameBase frame, UtilityReasoner* reasoner, ref AIContext aiContext, EntityRef entity = default)
        {
            ThreadSafe.Update((FrameThreadSafe)frame, reasoner, ref aiContext, entity);
        }
        #endregion
    }
}
