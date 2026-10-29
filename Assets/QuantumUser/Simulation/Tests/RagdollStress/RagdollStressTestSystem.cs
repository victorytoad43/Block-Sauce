using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Test-only (Phase 1 stress test): drops a stack of ragdolls above each RagdollStressTest entity once its start
    /// delay has passed, so their cost under rollback can be read before the knockdown feature exists.
    /// Ragdolls are created and posed the way Friendslop's RagdollSystem spawns a body.
    /// </summary>
    public unsafe class RagdollStressTestSystem : SystemMainThreadFilter<RagdollStressTestSystem.Filter>
    {
        /// <summary>
        /// The components the stress test reads on each spawner.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public RagdollStressTest* Test;
        }

        /// <summary>
        /// Spawns the spawner's ragdolls, stacked upward from the drop height with a little sideways jitter, once.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            RagdollStressTest* test = filter.Test;
            if (test->Spawned || frame.Number * frame.DeltaTime < test->StartDelay)
                return;

            test->Spawned = true;

            for (int i = 0; i < test->Count; i++)
            {
                FPVector3 jitter = new FPVector3(frame.RNG->Next(-test->Jitter, test->Jitter), FP._0,
                    frame.RNG->Next(-test->Jitter, test->Jitter));
                FPVector3 feet = filter.Transform->Position + jitter
                    + FPVector3.Up * (test->DropHeight + test->VerticalSpacing * i);
                FPQuaternion facing = FPQuaternion.AngleAxis(frame.RNG->Next(FP._0, 360), FPVector3.Up);
                Spawn(frame, test->Ragdoll, feet, facing, test->Spin);
            }
        }

        /// <summary>
        /// Creates one ragdoll group and moves every limb so the group's root sits at <paramref name="feet"/>.
        /// </summary>
        private void Spawn(Frame frame, AssetRef<EntityPrototype> prototype, FPVector3 feet, FPQuaternion facing, FP spin)
        {
            EntityRef root = frame.Create(prototype);
            Transform3D rootTransform = frame.Get<Transform3D>(root);
            FPQuaternion rotationDelta = facing * FPQuaternion.Inverse(rootTransform.Rotation);

            foreach ((EntityRef limb, int _) in frame.GetEntityGroupIterator(root))
            {
                if (limb != root)
                {
                    Place(frame, limb, rootTransform.Position, feet, rotationDelta, spin);
                }
            }

            Place(frame, root, rootTransform.Position, feet, rotationDelta, spin);
        }

        /// <summary>
        /// Moves and turns one limb from the prototype's pose to the spawn pose, and gives it a random tumble.
        /// </summary>
        private void Place(Frame frame, EntityRef entity, FPVector3 pivot, FPVector3 destination,
            FPQuaternion rotationDelta, FP spin)
        {
            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(entity);
            transform->Position = destination + rotationDelta * (transform->Position - pivot);
            transform->Rotation = rotationDelta * transform->Rotation;

            if (frame.Unsafe.TryGetPointer(entity, out PhysicsBody3D* body) == false)
                return;

            FPVector3 tumble = new FPVector3(frame.RNG->Next(-FP._1, FP._1), frame.RNG->Next(-FP._1, FP._1),
                frame.RNG->Next(-FP._1, FP._1));
            body->AngularVelocity = tumble.Normalized * spin;
        }
    }
}
