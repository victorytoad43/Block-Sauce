namespace Quantum
{
    using Photon.Deterministic;
    using System;

    public unsafe static partial class UTManager
    {
        public class ThreadSafe
        {
            // ========== PUBLIC METHODS ==================================================================================

            /// <summary>
            /// Updates a <see cref="UTAgent"/> which is part of the entity passed as parameter. The Considerations will be evaluated and the most useful will be executed.
            /// </summary>
            public static void Update(FrameThreadSafe frame, FP deltaTime, EntityRef entity)
            {
                AIContext aiContext = new AIContext();

                Update(frame, deltaTime, entity, ref aiContext);
            }

            /// <summary>
            /// Updates a <see cref="UTAgent"/> which is part of the entity passed as parameter. The Considerations will be evaluated and the most useful will be executed.
            /// Allows the use of a specific <see cref="AIContext"/>.
            /// </summary>
            public static void Update(FrameThreadSafe frame, FP deltaTime, EntityRef entity, ref AIContext aiContext)
            {
                if (entity.IsValid == false)
                {
                    Log.Info($"[Bot SDK] Tried updating UT Agent but the provided entity is not valid.");
                    return;
                }

                aiContext.SetupUTFromEntity(frame, entity);
                var reasoner = &aiContext.UTAgent->UtilityReasoner;
                reasoner->Update(frame, deltaTime, reasoner, entity, ref aiContext);
            }

            /// <summary>
            /// Updates a <see cref="UTAgent"/> which is not part of an entity but rather lives in Frame.Global. The Considerations will be evaluated and the most useful will be executed.
            /// </summary>
            public static void Update(FrameThreadSafe frame, FP deltaTime, UTAgent* utAgent)
            {
                AIContext aiContext = new AIContext();
                aiContext.SetUTAgent(utAgent, agentEntity: default);

                Update(frame, deltaTime, entity: default, ref aiContext);
            }

            /// <summary>
            /// Updates a <see cref="UTAgent"/> which is not part of an entity but rather lives in Frame.Global. The Considerations will be evaluated and the most useful will be executed.
            /// Allows the use of a specific <see cref="AIContext"/>.
            /// </summary>
            public static void Update(FrameThreadSafe frame, FP deltaTime, UTAgent* utAgent, ref AIContext aiContext)
            {
                aiContext.SetUTAgent(utAgent, agentEntity: default);

                Update(frame, deltaTime, entity: default, ref aiContext);
            }

            #region Obsolete
            /// <summary>
            /// Ticks the UtilityReasoner. The Considerations will be evaluated and the most useful will be executed.
            /// It can be agnostic to entities, meaning that it is possible to have a UtilityReasoner as part of Global.
            /// </summary>
            [Obsolete("Use the overload that receives a UTAgent* instead.")]
            public static void Update(FrameThreadSafe frame, UtilityReasoner* reasoner, EntityRef entity = default)
            {
                AIContext aiContext = new AIContext();
                if (entity != default)
                {
                    aiContext.SetupUTFromEntity(frame, entity);
                }

                Update(frame, reasoner, ref aiContext, entity);
            }

            /// <summary>
            /// Ticks the UtilityReasoner. The Considerations will be evaluated and the most useful will be executed.
            /// It can be agnostic to entities, meaning that it is possible to have a UtilityReasoner as part of Global.
            /// </summary>
            [Obsolete("Use the overload that receives a UTAgent* instead.")]
            public static void Update(FrameThreadSafe frame, UtilityReasoner* reasoner, ref AIContext aiContext, EntityRef entity = default)
            {
                if (reasoner == default && entity != default)
                {
                    reasoner = &frame.GetPointer<UTAgent>(entity)->UtilityReasoner;
                }

                reasoner->Update(frame, frame.DeltaTime, reasoner, entity, ref aiContext);
            }
            #endregion
        }
    }
}
