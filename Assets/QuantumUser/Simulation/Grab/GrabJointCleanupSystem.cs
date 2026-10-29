namespace Quantum
{
    /// <summary>
    /// Removes a released block's leash joint, and its joints component once empty. Ported from Friendslop.
    /// </summary>
    public unsafe class GrabJointCleanupSystem : SystemMainThreadFilter<GrabJointCleanupSystem.Filter>
    {
        /// <summary>
        /// The components the cleanup reads on each grabbable body with joints.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public Grabbable* Grabbable;
            public PhysicsJoints3D* Joints;
        }

        /// <summary>
        /// Removes the leash from a body nobody holds, and the joints component if nothing else is left on it.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            if (filter.Grabbable->Holder != EntityRef.None)
                return;

            if (filter.Joints->RemoveJoints(frame, (int)EGrabJointTag.Leash) == false)
                return;

            if (filter.Joints->Count == 0)
            {
                frame.Remove<PhysicsJoints3D>(filter.Entity);
            }
        }
    }
}
