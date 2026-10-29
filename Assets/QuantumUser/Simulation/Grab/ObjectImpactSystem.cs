using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Raises ObjectImpact when a free body hits something hard enough, for impact sounds and effects.
    /// Ported from Friendslop. Fires on verified frames only, so a mispredicted impact never plays twice.
    /// </summary>
    public unsafe class ObjectImpactSystem : SystemSignalsOnly, ISignalOnCollisionEnter3D
    {
        /// <summary>
        /// The speed into the contact, in m/s, below which a touch is too soft to count as an impact.
        /// </summary>
        private readonly FP MinImpactSpeed = 2;

        /// <summary>
        /// Raises the impact event for a body that is not being held and hits at or above the minimum speed.
        /// </summary>
        void ISignalOnCollisionEnter3D.OnCollisionEnter3D(Frame frame, CollisionInfo3D info)
        {
            if (frame.IsPredicted)
                return;

            if (frame.Has<Grabbable>(info.Entity) == false)
                return;

            if (frame.Unsafe.TryGetPointer(info.Entity, out PhysicsBody3D* body) == false)
                return;

            if (frame.Get<Grabbable>(info.Entity).Holder != EntityRef.None)
                return;

            FP intoSpeed = -FPVector3.Dot(body->Velocity, info.ContactNormal);
            if (intoSpeed < MinImpactSpeed)
                return;

            FPVector3 position = frame.Get<Transform3D>(info.Entity).Position;
            frame.Events.ObjectImpact(info.Entity, position, intoSpeed);
        }
    }
}
