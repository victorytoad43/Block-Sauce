using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Decides whether an entity stands on ground, and banks the last safe spot for it to respawn on after a fall.
    /// Grounding comes from physics contacts rather than rays, so it holds on top of dynamic blocks and across seams.
    /// Ported from Friendslop's GroundedTrackerSystem; the ragdoll check returns with knockdown in Phase 3.
    /// </summary>
    public unsafe class GroundedTrackerSystem : SystemMainThreadFilter<GroundedTrackerSystem.Filter>,
        ISignalOnCollision3D
    {
        /// <summary>
        /// The four horizontal directions the edge probes step out along from a candidate respawn point.
        /// </summary>
        private readonly FPVector3[] BorderCheckDirections =
        {
            FPVector3.Forward, FPVector3.Back, FPVector3.Left, FPVector3.Right
        };

        /// <summary>
        /// The components the grounded tracker system updates for each tracked entity.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public GroundedTracker* GroundedTracker;
        }

        /// <summary>
        /// Sets the grounded state from recent ground contacts, and banks respawn points spaced apart and clear of edges.
        /// The state freezes while the body sleeps, since a sleeping body reports no contacts.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            GroundedTracker* tracker = filter.GroundedTracker;
            GroundedTrackerConfig config = frame.FindAsset(tracker->Config);

            if (frame.Unsafe.TryGetPointer(filter.Entity, out PhysicsBody3D* body) && body->IsSleeping)
            {
                if (tracker->IsGrounded)
                {
                    tracker->LastGroundContactFrame = frame.Number;
                }

                return;
            }

            bool grounded = frame.Number - tracker->LastGroundContactFrame <= config.GroundContactGraceTicks;
            tracker->IsGrounded = grounded;

            if (grounded == false)
                return;

            FPVector3 position = filter.Transform->Position;
            FPVector3 delta = position - tracker->LastGroundedPosition;
            delta.Y = FP._0;

            if (delta.SqrMagnitude <= config.MinRebankDistance * config.MinRebankDistance)
                return;

            if (IsAwayFromEdges(frame, position, config))
            {
                tracker->LastGroundedPosition = position;
            }
        }

        /// <summary>
        /// Stamps this tick as a ground contact when the contact normal points up enough, keeping the most upward normal.
        /// </summary>
        void ISignalOnCollision3D.OnCollision3D(Frame frame, CollisionInfo3D info)
        {
            if (frame.Unsafe.TryGetPointer(info.Entity, out GroundedTracker* tracker) == false)
                return;

            GroundedTrackerConfig config = frame.FindAsset(tracker->Config);
            if (info.ContactNormal.Y < config.GroundNormalThreshold)
                return;

            bool firstContactThisTick = tracker->LastGroundContactFrame != frame.Number;
            if (firstContactThisTick || info.ContactNormal.Y > tracker->GroundNormal.Y)
            {
                tracker->GroundNormal = info.ContactNormal;
            }

            tracker->LastGroundContactFrame = frame.Number;
        }

        /// <summary>
        /// Returns true when probes on a ring around <paramref name="center"/> all hit level geometry, or the radius is 0.
        /// </summary>
        private bool IsAwayFromEdges(Frame frame, FPVector3 center, GroundedTrackerConfig config)
        {
            if (config.BorderCheckRadius <= FP._0)
                return true;

            for (int i = 0; i < BorderCheckDirections.Length; i++)
            {
                FPVector3 origin = center + BorderCheckDirections[i] * config.BorderCheckRadius + FPVector3.Up;

                bool hasGround = frame.Physics3D.Raycast(origin, FPVector3.Down, config.EdgeProbeLength,
                    -1, QueryOptions.HitStatics).HasValue;

                if (config.DrawRays)
                {
                    ColorRGBA color = hasGround ? ColorRGBA.Green : ColorRGBA.Red;
                    Draw.Ray(origin, FPVector3.Down * config.EdgeProbeLength, color);
                }

                if (hasGround == false)
                    return false;
            }

            return true;
        }
    }
}
