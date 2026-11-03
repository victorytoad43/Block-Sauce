namespace Quantum
{
    public static unsafe partial class BTManager
    {
        // ========== PUBLIC MEMBERS ==================================================================================

        public class ThreadSafe
        {
            /// <summary>
            /// Made for internal use only.
            /// </summary>
            public static void ClearBTParams(BTParams btParams)
            {
                btParams.Reset();
            }

            /// <summary>
            /// Call this method every frame to update your BT Agent.
            /// Optionally, pass a blackboard component pointer. Otherwise, the BT Manager tries to find one in the entity in the parameters.
            /// </summary>
            public static void Update(FrameThreadSafe frame, EntityRef entity, AIBlackboardComponent* blackboard = null)
            {
                AIContext aiContext = new AIContext();
                aiContext.SetupBTFromEntity(frame, entity);

                Update(frame, entity, ref aiContext, blackboard);
            }

            /// <summary>
            /// Call this method every frame to update your BT Agent.
            /// Optionally, pass a blackboard component pointer. Otherwise, the BT Manager tries to find one in the entity in the parameters.
            /// </summary>
            public static void Update(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext, AIBlackboardComponent* blackboard = null)
            {
                var userParams = new BTParamsUser();
                InternalUpdate(frame, entity, ref userParams, ref aiContext, blackboard);
            }

            /// <summary>
            /// CAUTION: Use this overload with care.<br/>
            /// It allows the definition of custom parameters which are passed through the entire BT pipeline, for easy access.<br/>
            /// The user parameters struct needs to be created from scratch every time BEFORE calling the BT Update method.<br/>
            /// Make sure to also implement BTParamsUser.ClearUser(frame).
            /// Optionally, pass a blackboard component pointer. Otherwise, the BT Manager tries to find one in the entity in the parameters.
            /// </summary>
            /// <param name="userParams">Used to define custom user data. It needs to be created from scratch every time before calling this method.</param>
            public static void Update(FrameThreadSafe frame, EntityRef entity, ref BTParamsUser userParams, AIBlackboardComponent* blackboard = null)
            {
                AIContext aiContext = new AIContext();
                aiContext.SetupBTFromEntity(frame, entity);

                Update(frame, entity, ref userParams, ref aiContext, blackboard);
            }

            /// <summary>
            /// CAUTION: Use this overload with care.<br/>
            /// It allows the definition of custom parameters which are passed through the entire BT pipeline, for easy access.<br/>
            /// The user parameters struct needs to be created from scratch every time BEFORE calling the BT Update method.<br/>
            /// Make sure to also implement BTParamsUser.ClearUser(frame).
            /// Optionally, pass a blackboard component pointer. Otherwise, the BT Manager tries to find one in the entity in the parameters.
            /// </summary>
            /// <param name="userParams">Used to define custom user data. It needs to be created from scratch every time before calling this method.</param>
            public static void Update(FrameThreadSafe frame, EntityRef entity, ref BTParamsUser userParams, ref AIContext aiContext, AIBlackboardComponent* blackboard = null)
            {
                InternalUpdate(frame, entity, ref userParams, ref aiContext, blackboard);
            }

            private static void InternalUpdate(FrameThreadSafe frame, EntityRef entity, ref BTParamsUser userParams, ref AIContext aiContext, AIBlackboardComponent* blackboard = null)
            {
                // If the user provided a specific blackboard, then override it in AI Context
                if (blackboard != null)
                {
                    aiContext.SetBlackboard(blackboard);
                }

                BTParams btParams = new BTParams();
                btParams.SetDefaultParams(frame, aiContext.BTAgent, entity, aiContext.Blackboard);
                btParams.UserParams = userParams;

                btParams.Agent->Update(ref btParams, ref aiContext);
            }
        }
    }
}
