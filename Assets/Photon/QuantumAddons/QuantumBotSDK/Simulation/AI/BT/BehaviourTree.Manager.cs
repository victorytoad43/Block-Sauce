using Quantum.Core;
using System.Runtime.CompilerServices;

namespace Quantum
{
    public static unsafe partial class BTManager
    {
        /// <summary>
        /// Call this once, to initialize the BTAgent.
        /// This method internally looks for a Blackboard Component on the entity
        /// and passes it down the pipeline.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="entity"></param>
        /// <param name="root"></param>
        public static void Init(FrameBase frame, EntityRef entity, AssetRef<BTRoot> rootRef)
        {
            var root = frame.FindAsset<BTRoot>(rootRef);
            Init(frame, entity, root);
        }

        /// <summary>
        /// Call this once, to initialize the BTAgent.
        /// This method internally looks for a Blackboard Component on the entity
        /// and passes it down the pipeline.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="entity"></param>
        /// <param name="root"></param>
        public static void Init(FrameBase frame, EntityRef entity, BTRoot root)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetupBTFromEntity((FrameThreadSafe)frame, entity);

            Init(frame, entity, root, ref aiContext);
        }

        /// <summary>
        /// Call this once, to initialize the BTAgent.
        /// This method internally looks for a Blackboard Component on the entity
        /// and passes it down the pipeline.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="entity"></param>
        /// <param name="root"></param>
        public static void Init(FrameBase frame, EntityRef entity, AssetRef<BTRoot> rootRef, ref AIContext aiContext)
        {
            var root = frame.FindAsset<BTRoot>(rootRef);
            Init(frame, entity, root, ref aiContext);
        }

        /// <summary>
        /// Call this once, to initialize the BTAgent.
        /// This method internally looks for a Blackboard Component on the entity
        /// and passes it down the pipeline.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="entity"></param>
        /// <param name="root"></param>
        public static void Init(FrameBase frame, EntityRef entity, BTRoot root, ref AIContext aiContext)
        {
            var btAgent = aiContext.BTAgent;
            var blackboard = aiContext.Blackboard;

            BTParams btParams = new BTParams();
            btParams.SetDefaultParams((FrameThreadSafe)frame, btAgent, entity, blackboard);

            btAgent->Init(root, btParams, ref aiContext);
        }

        /// <summary>
        /// Used to free every collection contained on the BTAgent component and to reset to default all of its fields.
        /// <br/>IMPORTANT: It is crucial to Free such data before destroying the entity to avoid memory leaks.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="entity"></param>
        public static void Free(FrameBase frame, EntityRef entity)
        {
            if (frame.Unsafe.TryGetPointer(entity, out BTAgent* agent))
            {
                agent->Free(frame);
            }
            else
            {
                Log.Error($"[Bot SDK] Tried to Free the BTAgent data from entity {entity} but it does not have such component.");
            }
        }

        /// <summary>
        /// Made for internal use only.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearBTParams(BTParams btParams)
        {
            btParams.Reset();
        }

        /// <summary>
        /// Call this method every frame to update your BT Agent.
        /// Optionally, pass a blackboard component pointer. Otherwise, the BT Manager tries to find one in the entity in the parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(FrameBase frame, EntityRef entity, AIBlackboardComponent* blackboard = null)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetupBTFromEntity((FrameThreadSafe)frame, entity);

            Update(frame, entity, ref aiContext, blackboard);
        }

        /// <summary>
        /// Call this method every frame to update your BT Agent.
        /// Optionally, pass a blackboard component pointer. Otherwise, the BT Manager tries to find one in the entity in the parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(FrameBase frame, EntityRef entity, ref AIContext aiContext, AIBlackboardComponent* blackboard = null)
        {
            ThreadSafe.Update((FrameThreadSafe)frame, entity, ref aiContext, blackboard);
        }

        /// <summary>
        /// CAUTION: Use this overload with care.<br/>
        /// It allows the definition of custom parameters which are passed through the entire BT pipeline, for easy access.<br/>
        /// The user parameters struct needs to be created from scratch every time BEFORE calling the BT Update method.<br/>
        /// Make sure to also implement BTParamsUser.ClearUser(frame).
        /// Optionally, pass a blackboard component pointer. Otherwise, the BT Manager tries to find one in the entity in the parameters.
        /// </summary>
        /// <param name="userParams">Used to define custom user data. It needs to be created from scratch every time before calling this method.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(FrameBase frame, EntityRef entity, ref BTParamsUser userParams, AIBlackboardComponent* blackboard = null)
        {
            AIContext aiContext = new AIContext();
            aiContext.SetupBTFromEntity((FrameThreadSafe)frame, entity);

            Update(frame, entity, ref userParams, ref aiContext, blackboard);
        }

        /// <summary>
        /// CAUTION: Use this overload with care.<br/>
        /// It allows the definition of custom parameters which are passed through the entire BT pipeline, for easy access.<br/>
        /// The user parameters struct needs to be created from scratch every time BEFORE calling the BT Update method.<br/>
        /// Make sure to also implement BTParamsUser.ClearUser(frame).
        /// </summary>
        /// <param name="userParams">Used to define custom user data. It needs to be created from scratch every time before calling this method.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Update(FrameBase frame, EntityRef entity, ref BTParamsUser userParams, ref AIContext aiContext, AIBlackboardComponent* blackboard = null)
        {
            ThreadSafe.Update((FrameThreadSafe)frame, entity, ref userParams, ref aiContext, blackboard);
        }
    }
}
