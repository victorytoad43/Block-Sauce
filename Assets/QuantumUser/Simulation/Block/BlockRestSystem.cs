using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// The rest assist (the mode's <c>RestAssist</c>): a block resting on another block or on the ground that is only
    /// creeping or rocking, slower than the assist's speeds relative to what it rests on, loses part of that slow movement
    /// every tick. The physics solver leaves a stack of three slightly uneven boxes creeping until it slides apart; this
    /// damps that creep the way static friction would. Anything faster (a hit, a throw, a body check, a fall) is left to
    /// the physics untouched, so towers still fall when something hits them. Nothing is locked or joined. Slip sauce
    /// is left to slide: a block with Slip on it, or resting on one, is skipped.
    /// </summary>
    public unsafe class BlockRestSystem : SystemMainThread
    {
        public override void Update(Frame frame)
        {
            if (frame.TryGetSingleton(out Match match) == false)
                return;

            GameModeConfig mode = frame.FindAsset(match.Mode);
            if (mode == null || mode.RestAssist == false)
                return;

            FP keep = FP._1 - mode.RestAssistDamping;
            FP maxSpeedSqr = mode.RestAssistSpeed * mode.RestAssistSpeed;
            FP maxSpinSqr = mode.RestAssistAngularSpeed * mode.RestAssistAngularSpeed;

            var blocks = frame.Filter<Block, Transform3D, PhysicsBody3D>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* _, out Transform3D* transform, out PhysicsBody3D* body))
            {
                if (body->IsSleeping || body->IsKinematic || IsHeld(frame, entity) || HasSlip(frame, entity))
                    continue;

                if (body->AngularVelocity.SqrMagnitude > maxSpinSqr)
                    continue;

                if (TryGetSupportVelocity(frame, entity, transform->Position, mode, out FPVector3 supportVelocity) == false)
                    continue;

                FPVector3 relative = body->Velocity - supportVelocity;
                if (relative.SqrMagnitude > maxSpeedSqr)
                    continue;

                body->Velocity = supportVelocity + relative * keep;
                body->AngularVelocity *= keep;
            }
        }

        /// <summary>
        /// Finds what the block rests on (straight down from its center, within half a block and a small gap) and its
        /// velocity: zero for the ground, the body's for a block or anything else that moves.
        /// </summary>
        private static bool TryGetSupportVelocity(Frame frame, EntityRef entity, FPVector3 position, GameModeConfig mode, out FPVector3 velocity)
        {
            velocity = FPVector3.Zero;
            FP reach = mode.BlockSize * FP._0_50 + FP._0_05;
            HitCollection3D hits = frame.Physics3D.RaycastAll(position, FPVector3.Down, reach, -1,
                QueryOptions.HitStatics | QueryOptions.HitDynamics | QueryOptions.HitKinematics);
            hits.SortCastDistance();
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].IsTrigger || hits[i].Entity == entity)
                    continue;

                if (hits[i].IsStatic)
                    return true;

                if (frame.Has<Block>(hits[i].Entity) == false || HasSlip(frame, hits[i].Entity)
                    || frame.TryGet(hits[i].Entity, out PhysicsBody3D below) == false || IsHeld(frame, hits[i].Entity))
                    return false;

                velocity = below.Velocity;
                return true;
            }

            return false;
        }

        private static bool HasSlip(Frame frame, EntityRef entity)
        {
            return frame.TryGet(entity, out SauceCoat coat) && coat.SlipStrength > FP._0;
        }

        private static bool IsHeld(Frame frame, EntityRef entity)
        {
            return frame.TryGet(entity, out Grabbable grabbable) && grabbable.Holder != EntityRef.None;
        }
    }
}
