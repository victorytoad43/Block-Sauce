using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// The throw velocity calculation, shared by the grab system (which throws) and the view's aim preview (which only
    /// reads it), so the trajectory line always shows the throw the simulation will make.
    /// </summary>
    public static unsafe class ThrowHelper
    {
        /// <summary>
        /// Returns the velocity a throw by <paramref name="holder"/> would give its block right now: the carrier's velocity
        /// plus the throw speed along the character's facing raised by the aim pitch. Thrown while airborne it is a
        /// jumpshot, which aims the same way and throws faster.
        /// </summary>
        public static FPVector3 GetThrowVelocity(Frame frame, EntityRef holder, GrabConfig config, out bool isJumpshot)
        {
            isJumpshot = frame.TryGet(holder, out GroundedTracker grounded) && grounded.IsGrounded == false;

            Grabber grabber = frame.Get<Grabber>(holder);
            Transform3D transform = frame.Get<Transform3D>(holder);
            FPVector3 direction = GetBodyYaw(transform.Rotation) * FPQuaternion.Euler(-grabber.AimPitch, FP._0, FP._0)
                * FPVector3.Forward;
            FP speed = isJumpshot ? config.ThrowSpeed * config.JumpshotSpeedScale : config.ThrowSpeed;

            return frame.Get<PhysicsBody3D>(holder).Velocity + direction * speed;
        }

        /// <summary>
        /// Returns a rotation's heading as a rotation about world up only.
        /// </summary>
        public static FPQuaternion GetBodyYaw(FPQuaternion rotation)
        {
            FPVector3 forward = rotation * FPVector3.Forward;
            return FPQuaternion.AngleAxis(FPMath.Atan2(forward.X, forward.Z) * FP.Rad2Deg, FPVector3.Up);
        }
    }
}
