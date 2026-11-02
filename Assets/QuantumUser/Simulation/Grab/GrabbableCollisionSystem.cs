namespace Quantum
{
    /// <summary>
    /// Stops a carrier from colliding with what it holds, so the held block can sit close without shoving its carrier,
    /// and a just-thrown block from colliding with its thrower or with what it was thrown out of. Ported from Friendslop.
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
        /// Returns true if <paramref name="carried"/> is held by <paramref name="holder"/>, or was just thrown by it or
        /// out of it.
        /// </summary>
        private bool CarriedBy(Frame frame, EntityRef carried, EntityRef holder)
        {
            if (frame.TryGet(carried, out Grabbable grabbable) == false)
                return false;

            if (grabbable.Holder == holder)
                return true;

            if (grabbable.ThrowClear.IsRunning(frame) == false)
                return false;

            if (grabbable.LastHolder == holder)
                return true;

            FixedArray<EntityRef> clearOf = grabbable.ThrowClearOf;
            for (int i = 0; i < clearOf.Length; i++)
            {
                if (clearOf[i] == holder)
                    return true;
            }

            return false;
        }
    }
}
