using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// The one path every hit on a character goes through. Anything that knocks a character (a thrown or swung block,
    /// a body check, later explosions and sauces) raises ApplyKnockback; this system applies the impulse and decides
    /// whether the hit pushes, staggers or knocks the character down; staggered or downed, it drops what it holds. Collisions between a character and a
    /// moving body are turned into knockback here too: the collision callback only records the tick's strongest hit,
    /// and <see cref="Update"/> applies it after the physics step. A knockdown adds components (the ragdoll), and adding
    /// components moves an entity's data, which the physics engine must not see happen in the middle of its callbacks.
    /// </summary>
    public unsafe class KnockbackSystem : SystemMainThreadFilter<KnockbackSystem.Filter>, ISignalApplyKnockback, ISignalOnCollisionEnter3D
    {
        /// <summary>
        /// The components the system reads on each character that can be knocked.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public Knockbackable* Knockbackable;
        }

        /// <summary>
        /// Applies the strongest collision hit a character took this tick, outside the physics callbacks.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            if (filter.Knockbackable->PendingImpulse == FPVector3.Zero)
                return;

            FPVector3 impulse = filter.Knockbackable->PendingImpulse;
            EntityRef source = filter.Knockbackable->PendingSource;
            filter.Knockbackable->PendingImpulse = FPVector3.Zero;
            filter.Knockbackable->PendingSource = EntityRef.None;
            frame.Signals.ApplyKnockback(filter.Entity, impulse, source);
        }

        /// <summary>
        /// Applies the impulse to the target and staggers it when the resulting speed change is large enough.
        /// </summary>
        void ISignalApplyKnockback.ApplyKnockback(Frame frame, EntityRef target, FPVector3 impulse, EntityRef source)
        {
            if (frame.Unsafe.TryGetPointer(target, out PhysicsBody3D* body) == false)
                return;

            if (frame.Unsafe.TryGetPointer(target, out Knockbackable* knockbackable) == false)
                return;

            if (frame.Has<RagdollOwner>(target))
                return;

            KnockbackConfig config = frame.FindAsset(knockbackable->Config);
            bool wasStaggered = frame.TryGet(target, out MovementData movement) && movement.Tier == ControlTier.Staggered;

            FP speedChange = impulse.Magnitude / body->Mass;
            bool staggered = speedChange >= config.StaggerSpeedChange;
            bool knockedDown = speedChange >= config.KnockdownSpeedChange
                || (config.BodyCheckKnocksDownStaggered && wasStaggered && frame.Has<Player>(source));

            if (knockedDown && frame.Has<Ragdollable>(target))
            {
                // The ragdoll spawns moving at the body's velocity, so the hit goes straight into the velocity rather
                // than through an impulse the solver would only apply on the next step. Downed, it drops what it holds.
                body->Velocity += impulse / body->Mass;
                frame.Events.KnockedBack(target, source, impulse, true);
                frame.Signals.SetRagdoll(target, true);
                return;
            }

            body->WakeUp();
            body->AddLinearImpulse(impulse);

            if (staggered)
            {
                FP seconds = FPMath.Clamp(speedChange * config.StaggerSecondsPerSpeed,
                    config.MinStaggerSeconds, config.MaxStaggerSeconds);
                frame.Signals.Stagger(target, seconds);
                frame.Signals.DropHeld(target);
            }

            frame.Events.KnockedBack(target, source, impulse, staggered);
        }

        /// <summary>
        /// Turns a moving body striking a character into knockback: the striker's speed toward the character, scaled by
        /// how heavy the striker is relative to the character, becomes a speed change along the striker's motion, lifted
        /// a little by the up bias. Another character must be running into this one faster than the body-check speed.
        /// </summary>
        void ISignalOnCollisionEnter3D.OnCollisionEnter3D(Frame frame, CollisionInfo3D info)
        {
            if (info.IsStatic)
                return;

            EntityRef target = info.Entity;
            EntityRef striker = info.Other;

            if (frame.Unsafe.TryGetPointer(target, out Knockbackable* knockbackable) == false)
                return;

            // What a character carries never hits it: the contact is ignored (GrabbableCollisionSystem), but the enter
            // callback still arrives, early even, for the speculative contact of a block held close.
            if (frame.TryGet(striker, out Grabbable grabbable) && grabbable.Holder == target)
                return;

            if (frame.Unsafe.TryGetPointer(target, out PhysicsBody3D* targetBody) == false
                || frame.Unsafe.TryGetPointer(striker, out PhysicsBody3D* strikerBody) == false)
                return;

            KnockbackConfig config = frame.FindAsset(knockbackable->Config);

            FPVector3 toTarget = frame.Get<Transform3D>(target).Position - frame.Get<Transform3D>(striker).Position;
            toTarget.Y = FP._0;
            if (toTarget.SqrMagnitude <= FP._0)
                return;

            toTarget = toTarget.Normalized;

            // The striker counts only by its own speed toward the target. A block or character standing still is not
            // a hit when someone runs into it, and in a body check only the runner knocks the other one.
            bool isBodyCheck = frame.Has<Player>(striker);
            FP closingSpeed = FPVector3.Dot(strikerBody->Velocity, toTarget);

            FP minSpeed = isBodyCheck ? config.MinBodyCheckSpeed : config.MinHitSpeed;
            if (closingSpeed < minSpeed)
                return;

            FP massShare = strikerBody->Mass / (strikerBody->Mass + targetBody->Mass);
            FP speedChange = FPMath.Min(closingSpeed * massShare * config.HitScale, config.MaxHitSpeedChange);

            FPVector3 direction = strikerBody->Velocity;
            direction.Y = FP._0;
            direction = direction.SqrMagnitude > FP._0 ? direction.Normalized : toTarget;

            FPVector3 impulse = (direction + FPVector3.Up * config.UpBias) * (speedChange * targetBody->Mass);
            if (impulse.SqrMagnitude > knockbackable->PendingImpulse.SqrMagnitude)
            {
                knockbackable->PendingImpulse = impulse;
                knockbackable->PendingSource = striker;
            }
        }
    }
}
