namespace Quantum
{
    /// <summary>
    /// Stops a carrier from colliding with what it holds, so the held block can sit close without shoving its carrier.
    /// Ported from Friendslop; its ragdoll-limb case returns with knockdown in Phase 3.
    /// </summary>
    public unsafe class GrabbableCollisionSystem : SystemSignalsOnly, ISignalOnCollision3D
    {
        /// <summary>
        /// Ignores the contact when either body is held by the other.
        /// </summary>
        void ISignalOnCollision3D.OnCollision3D(Frame frame, CollisionInfo3D info)
        {
            if (info.IsStatic)
                return;

            if (CarriedBy(frame, info.Entity, info.Other) || CarriedBy(frame, info.Other, info.Entity))
            {
                info.IgnoreCollision = true;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="carried"/> is held by <paramref name="holder"/>.
        /// </summary>
        private bool CarriedBy(Frame frame, EntityRef carried, EntityRef holder)
        {
            return frame.TryGet(carried, out Grabbable grabbable) && grabbable.Holder == holder;
        }
    }
}
