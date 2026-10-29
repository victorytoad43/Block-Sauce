using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// Locks owned blocks into their owner's tower during play, and knocks them loose again.
    /// A block starts locking once it has been placed (picked up since it spawned or was last knocked loose) and rests,
    /// level and not held, in its owner's next slot: on the ground for the first
    /// block, squarely on the owner's top locked block after that. Progress fills over the mode's lock time and any hit
    /// resets it. A locked block is frozen (kinematic) and can't be grabbed; an impact over the unlock threshold makes
    /// it dynamic again, together with every locked block above it.
    /// A rival's block resting on top of a tower needs no rule of its own: the owner's next block can't rest there.
    /// </summary>
    public unsafe class BlockLockSystem : SystemMainThreadFilter<BlockLockSystem.Filter>, ISignalOnCollisionEnter3D
    {
        /// <summary>
        /// The components the lock system reads for each block.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public Block* Block;
            public Transform3D* Transform;
            public PhysicsBody3D* Body;
        }

        /// <summary>
        /// Advances each owned block's lock while a match is being played.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            if (TryGetPlayingMode(frame, out GameModeConfig mode) == false)
                return;

            Block* block = filter.Block;
            if (block->Owner.IsValid == false || block->Lock == LockState.Locked)
                return;

            if (TryGetSlot(frame, ref filter, mode, out int level) == false
                || mode.CanLock(frame, filter.Entity, level) == false)
            {
                if (block->Lock == LockState.Locking)
                {
                    ResetLock(frame, filter.Entity, block);
                    block->Lock = LockState.Free;
                }

                return;
            }

            if (block->Lock == LockState.Free)
            {
                block->Lock = LockState.Locking;
                block->LockProgress = FP._0;
                block->TowerLevel = (byte)level;
                frame.Events.BlockLockStarted(filter.Entity, block->Owner);
            }

            block->LockProgress += frame.DeltaTime / mode.LockSeconds;
            if (block->LockProgress >= FP._1)
            {
                LockBlock(frame, ref filter);
                mode.OnBlockLocked(frame, filter.Entity);
            }
        }

        /// <summary>
        /// Turns hits on locking and locked blocks into lock resets and unlocks. The striker is whichever body is not
        /// the block; its speed toward the block decides, so a block knocked into something doesn't unlock itself.
        /// </summary>
        void ISignalOnCollisionEnter3D.OnCollisionEnter3D(Frame frame, CollisionInfo3D info)
        {
            if (info.IsStatic || TryGetPlayingMode(frame, out GameModeConfig mode) == false)
                return;

            HandleHit(frame, mode, info.Entity, info.Other);
            HandleHit(frame, mode, info.Other, info.Entity);
        }

        /// <summary>
        /// Unlocks the block at <paramref name="entity"/> and then every locked block of the same owner above it.
        /// </summary>
        public static void Unlock(Frame frame, GameModeConfig mode, EntityRef entity, EntityRef striker)
        {
            Block* block = frame.Unsafe.GetPointer<Block>(entity);
            if (block->Lock != LockState.Locked)
                return;

            int level = block->TowerLevel;
            PlayerRef owner = block->Owner;
            ReleaseBlock(frame, entity, block);
            frame.Events.BlockUnlocked(entity, owner, striker);
            mode.OnBlockUnlocked(frame, entity);

            var blocks = frame.Filter<Block>();
            while (blocks.NextUnsafe(out EntityRef other, out Block* above))
            {
                if (above->Owner != owner || above->Lock != LockState.Locked || above->TowerLevel <= level)
                    continue;

                ReleaseBlock(frame, other, above);
                frame.Events.BlockUnlocked(other, owner, striker);
                mode.OnBlockUnlocked(frame, other);
            }
        }

        private void HandleHit(Frame frame, GameModeConfig mode, EntityRef target, EntityRef striker)
        {
            if (frame.Unsafe.TryGetPointer(target, out Block* block) == false || block->Lock == LockState.Free)
                return;

            if (frame.Unsafe.TryGetPointer(striker, out PhysicsBody3D* strikerBody) == false)
                return;

            FPVector3 toTarget = frame.Get<Transform3D>(target).Position - frame.Get<Transform3D>(striker).Position;
            if (toTarget.SqrMagnitude <= FP._0)
                return;

            FP closingSpeed = FPVector3.Dot(strikerBody->Velocity - frame.Get<PhysicsBody3D>(target).Velocity,
                toTarget.Normalized);

            if (block->Lock == LockState.Locking)
            {
                if (closingSpeed >= mode.LockResetSpeed)
                {
                    ResetLock(frame, target, block);
                }

                return;
            }

            if (closingSpeed >= mode.MinUnlockSpeed && closingSpeed * strikerBody->Mass >= mode.UnlockImpulse)
            {
                Unlock(frame, mode, target, striker);
            }
        }

        /// <summary>
        /// Finds the tower level this block would lock at, or returns false when it isn't resting in a valid slot.
        /// </summary>
        private bool TryGetSlot(Frame frame, ref Filter filter, GameModeConfig mode, out int level)
        {
            level = 0;
            Block* block = filter.Block;

            // Only placed blocks lock: picked up since it spawned or was last knocked loose, and let go of now.
            if (block->Handled == false)
                return false;

            if (frame.TryGet(filter.Entity, out Grabbable grabbable) && grabbable.Holder != EntityRef.None)
                return false;

            if (filter.Body->Velocity.SqrMagnitude > mode.RestSpeed * mode.RestSpeed
                || filter.Body->AngularVelocity.SqrMagnitude > mode.RestAngularSpeed * mode.RestAngularSpeed)
                return false;

            if (IsLevel(filter.Transform->Rotation, mode) == false)
                return false;

            if (BlockTower.IsOtherLocking(frame, block->Owner, filter.Entity))
                return false;

            level = BlockTower.CountLocked(frame, block->Owner);
            if (level >= mode.TowerHeight)
                return false;

            if (TryGetSupport(frame, filter.Entity, filter.Transform->Position, mode, out Hit3D support) == false)
                return false;

            if (level == 0)
                return support.IsStatic;

            EntityRef top = BlockTower.GetTop(frame, block->Owner);
            if (support.Entity != top)
                return false;

            FPVector3 offset = filter.Transform->Position - frame.Get<Transform3D>(top).Position;
            offset.Y = FP._0;
            FP maxOffset = mode.BlockSize * mode.MaxStackOffset;
            return offset.SqrMagnitude <= maxOffset * maxOffset;
        }

        /// <summary>
        /// Finds what the block rests on: the nearest hit straight down from its center, within half a block plus a
        /// small gap.
        /// </summary>
        private bool TryGetSupport(Frame frame, EntityRef entity, FPVector3 position, GameModeConfig mode, out Hit3D support)
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

        private static void LockBlock(Frame frame, ref Filter filter)
        {
            Block* block = filter.Block;
            block->Lock = LockState.Locked;
            block->LockProgress = FP._1;
            filter.Body->Velocity = FPVector3.Zero;
            filter.Body->AngularVelocity = FPVector3.Zero;
            filter.Body->IsKinematic = true;
            frame.Events.BlockLocked(filter.Entity, block->Owner, block->TowerLevel);
        }

        private static void ReleaseBlock(Frame frame, EntityRef entity, Block* block)
        {
            block->Lock = LockState.Free;
            block->LockProgress = FP._0;
            // A knocked-loose block must be picked up again before it can lock, so it doesn't relock where it lands.
            block->Handled = false;
            PhysicsBody3D* body = frame.Unsafe.GetPointer<PhysicsBody3D>(entity);
            body->IsKinematic = false;
            body->WakeUp();
        }

        private static void ResetLock(Frame frame, EntityRef entity, Block* block)
        {
            if (block->LockProgress > FP._0)
            {
                frame.Events.BlockLockReset(entity, block->Owner);
            }

            block->LockProgress = FP._0;
        }

        /// <summary>
        /// Returns the match's mode when a match is being played; locking does nothing in the lobby or between rounds.
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
