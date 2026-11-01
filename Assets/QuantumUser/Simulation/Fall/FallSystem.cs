using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Puts back anything that falls out of the arena: a character returns to its last safe ground, a downed body is
    /// carried back whole by its hips, any other limb just stops, and a block drops back in above its owner's spawn
    /// point (the arena center if nobody owns it), so a match can always still be won. Ported from Friendslop's
    /// FallSystem without its enemies, plus the blocks.
    /// </summary>
    public unsafe class FallSystem : SystemMainThreadFilter<FallSystem.Filter>
    {
        /// <summary>
        /// How far above its last safe ground, in meters, a fallen character reappears.
        /// </summary>
        private readonly FP RespawnLift = FP._1;

        /// <summary>
        /// How far above the spawn point, in meters, a fallen block is dropped back in.
        /// </summary>
        private readonly FP BlockDropHeight = 3;

        /// <summary>
        /// The components the fall system checks on every physics body.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public PhysicsBody3D* PhysicsBody;
        }

        /// <summary>
        /// Puts the body back when it drops below the fall height. A downed character is skipped: its hips carry the
        /// whole body back instead.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            if (filter.Transform->Position.Y > frame.RuntimeConfig.FallThresholdY)
                return;

            if (frame.TryGet(filter.Entity, out RagdollOwner ragdoll) && frame.Exists(ragdoll.Anchor))
                return;

            if (frame.TryGet(filter.Entity, out Block block))
            {
                ReturnBlock(frame, ref filter, block);
                return;
            }

            bool isLimb = frame.TryGet(filter.Entity, out RagdollLimb limb);
            if (frame.Unsafe.TryGetPointer(filter.Entity, out GroundedTracker* tracker) == false)
            {
                if (isLimb)
                {
                    ResetVelocities(filter.PhysicsBody);
                }

                return;
            }

            FPVector3 respawn = tracker->LastGroundedPosition + FPVector3.Up * RespawnLift;
            FPVector3 offset = respawn - filter.Transform->Position;
            filter.Transform->Position = respawn;
            ResetVelocities(filter.PhysicsBody);

            if (isLimb)
            {
                MoveBodyWith(frame, limb.Owner, filter.Entity, offset);
                return;
            }

            frame.Events.FallRespawned(filter.Entity);
        }

        /// <summary>
        /// Drops a fallen block back in above its owner's spawn point, or the arena center, at rest and upright.
        /// </summary>
        private void ReturnBlock(Frame frame, ref Filter filter, Block block)
        {
            FPVector3 position = FPVector3.Zero;
            if (block.Owner.IsValid && PlayerSystem.TryGetSpawn(frame, block.Owner, out Transform3D spawn))
            {
                position = spawn.Position;
            }

            filter.Transform->Position = position + FPVector3.Up * BlockDropHeight;
            filter.Transform->Rotation = FPQuaternion.Identity;
            ResetVelocities(filter.PhysicsBody);
            filter.PhysicsBody->WakeUp();
            frame.Events.FallRespawned(filter.Entity);
        }

        /// <summary>
        /// Moves every other piece of the owner's body by the same offset as the piece that was put back.
        /// </summary>
        private static void MoveBodyWith(Frame frame, EntityRef owner, EntityRef moved, FPVector3 offset)
        {
            if (frame.TryGet(owner, out RagdollOwner ragdoll) == false || frame.Exists(ragdoll.Body) == false)
                return;

            foreach ((EntityRef piece, int _) in frame.GetEntityGroupIterator(ragdoll.Body))
            {
                if (piece == moved)
                    continue;

                frame.Unsafe.GetPointer<Transform3D>(piece)->Position += offset;
                if (frame.Unsafe.TryGetPointer(piece, out PhysicsBody3D* body))
                {
                    ResetVelocities(body);
                }
            }

            frame.Events.FallRespawned(owner);
        }

        /// <summary>
        /// Stops a body dead.
        /// </summary>
        private static void ResetVelocities(PhysicsBody3D* body)
        {
            body->Velocity = FPVector3.Zero;
            body->AngularVelocity = FPVector3.Zero;
        }
    }
}
