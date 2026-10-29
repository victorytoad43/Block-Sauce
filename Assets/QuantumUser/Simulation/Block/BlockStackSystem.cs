using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// Works out, every tick of a match, which owned blocks are stacked and which count toward their owner's score.
    /// Blocks stay ordinary physics bodies throughout: nothing is frozen or joined, and any block can be grabbed.
    /// <list type="bullet">
    /// <item>Support: a block that isn't held and has a face pointing up looks straight down. Resting on static ground
    /// it is a ground block; resting on another block of the same owner, centered within the mode's offset, it sits
    /// on that block. Anything else (a rival's block, a tilt, a held block) breaks the stack there.</item>
    /// <item>Stacks: following each block's support down gives its level and the ground block (base) of its stack.
    /// A block is stacked when it is at level 1 or above, or is a ground block with an owned block on top.</item>
    /// <item>Counting: a stacked block at rest settles over the mode's settle time and then counts. A hit while
    /// settling restarts it. A block that stops being stacked (knocked off, grabbed, tipped) stops counting.</item>
    /// <item>Performance: a counted block that has come to rest is put to sleep straight away, so a finished stack
    /// drops out of the solver until something touches it.</item>
    /// </list>
    /// </summary>
    public unsafe class BlockStackSystem : SystemMainThread, ISignalOnCollisionEnter3D
    {
        /// <summary>
        /// The stack level of a block that isn't part of an owned stack.
        /// </summary>
        public const byte NotStacked = byte.MaxValue;

        /// <summary>
        /// The deepest stack followed when working out levels.
        /// </summary>
        private const int MaxStackDepth = 16;

        public override void Update(Frame frame)
        {
            if (TryGetPlayingMode(frame, out GameModeConfig mode) == false)
                return;

            FindSupports(frame, mode);
            ResolveStacks(frame);
            UpdateCounting(frame, mode);
        }

        /// <summary>
        /// Restarts a settling block that something runs into, as hard as the mode's settle-reset speed.
        /// </summary>
        void ISignalOnCollisionEnter3D.OnCollisionEnter3D(Frame frame, CollisionInfo3D info)
        {
            if (info.IsStatic || TryGetPlayingMode(frame, out GameModeConfig mode) == false)
                return;

            ResetIfHit(frame, mode, info.Entity, info.Other);
            ResetIfHit(frame, mode, info.Other, info.Entity);
        }

        /// <summary>
        /// Records what each owned block rests on.
        /// </summary>
        private static void FindSupports(Frame frame, GameModeConfig mode)
        {
            var blocks = frame.Filter<Block, Transform3D>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block, out Transform3D* transform))
            {
                block->Supported = false;
                block->Below = EntityRef.None;
                block->Base = EntityRef.None;
                block->HasAbove = false;
                block->StackLevel = NotStacked;

                if (block->Owner.IsValid == false || IsHeld(frame, entity) || IsLevel(transform->Rotation, mode) == false)
                    continue;

                if (TryGetSupport(frame, entity, transform->Position, mode, out Hit3D support) == false)
                    continue;

                if (support.IsStatic)
                {
                    block->Supported = true;
                    continue;
                }

                if (frame.TryGet(support.Entity, out Block below) == false || below.Owner != block->Owner
                    || IsHeld(frame, support.Entity))
                    continue;

                FPVector3 offset = transform->Position - frame.Get<Transform3D>(support.Entity).Position;
                offset.Y = FP._0;
                FP maxOffset = mode.BlockSize * mode.MaxStackOffset;
                if (offset.SqrMagnitude > maxOffset * maxOffset)
                    continue;

                block->Supported = true;
                block->Below = support.Entity;
            }
        }

        /// <summary>
        /// Follows each supported block down to its ground block, giving it a level and a base, and marks the blocks
        /// that have an owned block resting on them.
        /// </summary>
        private static void ResolveStacks(Frame frame)
        {
            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block))
            {
                if (block->Supported == false)
                    continue;

                EntityRef current = entity;
                for (int depth = 0; depth < MaxStackDepth; depth++)
                {
                    Block* step = frame.Unsafe.GetPointer<Block>(current);
                    if (step->Supported == false)
                        break;

                    if (step->Below == EntityRef.None)
                    {
                        block->StackLevel = (byte)depth;
                        block->Base = current;
                        break;
                    }

                    current = step->Below;
                }
            }

            blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef _, out Block* block))
            {
                if (block->StackLevel != NotStacked && block->StackLevel > 0)
                {
                    frame.Unsafe.GetPointer<Block>(block->Below)->HasAbove = true;
                }
            }
        }

        /// <summary>
        /// Settles and counts stacked blocks at rest, and drops blocks that are no longer stacked.
        /// </summary>
        private static void UpdateCounting(Frame frame, GameModeConfig mode)
        {
            var blocks = frame.Filter<Block, PhysicsBody3D>();
            while (blocks.NextUnsafe(out EntityRef entity, out Block* block, out PhysicsBody3D* body))
            {
                if (block->Owner.IsValid == false)
                    continue;

                bool stacked = block->StackLevel != NotStacked && block->StackLevel < mode.TowerHeight
                    && (block->StackLevel > 0 || block->HasAbove)
                    && mode.CanCount(frame, entity, block->StackLevel);

                if (stacked == false)
                {
                    Loosen(frame, mode, entity, block);
                    continue;
                }

                bool atRest = body->Velocity.SqrMagnitude <= mode.RestSpeed * mode.RestSpeed
                    && body->AngularVelocity.SqrMagnitude <= mode.RestAngularSpeed * mode.RestAngularSpeed;

                switch (block->Stack)
                {
                    case StackState.Loose:
                        if (atRest)
                        {
                            block->Stack = StackState.Settling;
                            block->StackProgress = FP._0;
                            frame.Events.BlockSettleStarted(entity, block->Owner);
                        }
                        break;

                    case StackState.Settling:
                        if (atRest)
                        {
                            block->StackProgress += frame.DeltaTime / mode.SettleSeconds;
                            if (block->StackProgress >= FP._1)
                            {
                                block->Stack = StackState.Counted;
                                block->StackProgress = FP._1;
                                frame.Events.BlockCounted(entity, block->Owner, block->StackLevel);
                                mode.OnBlockCounted(frame, entity);
                            }
                        }
                        break;

                    case StackState.Counted:
                        SleepIfStill(frame, body);
                        break;
                }
            }
        }

        private static void Loosen(Frame frame, GameModeConfig mode, EntityRef entity, Block* block)
        {
            if (block->Stack == StackState.Counted)
            {
                frame.Events.BlockUncounted(entity, block->Owner);
                mode.OnBlockUncounted(frame, entity);
            }
            else if (block->Stack == StackState.Settling && block->StackProgress > FP._0)
            {
                frame.Events.BlockSettleReset(entity, block->Owner);
            }

            block->Stack = StackState.Loose;
            block->StackProgress = FP._0;
        }

        /// <summary>
        /// Puts a counted block to sleep once it is as still as the physics engine's own sleep check asks, skipping the
        /// engine's sleep timer. Any contact wakes it again, so how a stack falls when hit is unchanged.
        /// </summary>
        private static void SleepIfStill(Frame frame, PhysicsBody3D* body)
        {
            if (body->IsSleeping || body->AllowSleeping == false)
                return;

            FP linear = frame.SimulationConfig.Physics.LinearSleepTolerance;
            FP angular = frame.SimulationConfig.Physics.AngularSleepToleranceInRad;
            if (body->Velocity.SqrMagnitude <= linear * linear && body->AngularVelocity.SqrMagnitude <= angular * angular)
            {
                body->Sleep();
            }
        }

        private static void ResetIfHit(Frame frame, GameModeConfig mode, EntityRef target, EntityRef striker)
        {
            if (frame.Unsafe.TryGetPointer(target, out Block* block) == false || block->Stack != StackState.Settling)
                return;

            if (frame.Unsafe.TryGetPointer(striker, out PhysicsBody3D* strikerBody) == false)
                return;

            FPVector3 toTarget = frame.Get<Transform3D>(target).Position - frame.Get<Transform3D>(striker).Position;
            if (toTarget.SqrMagnitude <= FP._0)
                return;

            FP closingSpeed = FPVector3.Dot(strikerBody->Velocity - frame.Get<PhysicsBody3D>(target).Velocity,
                toTarget.Normalized);
            if (closingSpeed < mode.SettleResetSpeed)
                return;

            if (block->StackProgress > FP._0)
            {
                frame.Events.BlockSettleReset(target, block->Owner);
            }

            block->StackProgress = FP._0;
        }

        /// <summary>
        /// Finds what the block rests on: the nearest hit straight down from its center, within half a block plus a
        /// small gap.
        /// </summary>
        private static bool TryGetSupport(Frame frame, EntityRef entity, FPVector3 position, GameModeConfig mode, out Hit3D support)
        {
            support = default;
            FP reach = mode.BlockSize * FP._0_50 + FP._0_10 + FP._0_05;
            HitCollection3D hits = frame.Physics3D.RaycastAll(position, FPVector3.Down, reach, -1,
                QueryOptions.HitStatics | QueryOptions.HitDynamics | QueryOptions.HitKinematics);
            hits.SortCastDistance();
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].IsTrigger || hits[i].Entity == entity)
                    continue;

                support = hits[i];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether one of the block's faces points up within the mode's tilt limit (a cube can rest on any face).
        /// </summary>
        private static bool IsLevel(FPQuaternion rotation, GameModeConfig mode)
        {
            FP minDot = FPMath.Cos(mode.MaxTiltDegrees * FP.Deg2Rad);
            FP x = FPMath.Abs((rotation * FPVector3.Right).Y);
            FP y = FPMath.Abs((rotation * FPVector3.Up).Y);
            FP z = FPMath.Abs((rotation * FPVector3.Forward).Y);
            return FPMath.Max(x, FPMath.Max(y, z)) >= minDot;
        }

        private static bool IsHeld(Frame frame, EntityRef entity)
        {
            return frame.TryGet(entity, out Grabbable grabbable) && grabbable.Holder != EntityRef.None;
        }

        /// <summary>
        /// Returns the match's mode when a match is being played; stacks only count during play.
        /// </summary>
        private static bool TryGetPlayingMode(Frame frame, out GameModeConfig mode)
        {
            mode = null;
            if (frame.TryGetSingleton(out Match match) == false || match.State != MatchState.Playing)
                return false;

            mode = frame.FindAsset(match.Mode);
            return mode != null;
        }
    }
}
