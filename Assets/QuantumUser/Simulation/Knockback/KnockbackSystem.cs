using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// The one path every hit on a character goes through. Anything that knocks a character (a thrown or swung block,
    /// a body check, later explosions and sauces) raises ApplyKnockback; this system applies the impulse and decides
    /// whether the hit staggers; a staggered character drops what it holds. Collisions between a character and a
    /// moving body are turned into knockback here too.
    /// </summary>
    public unsafe class KnockbackSystem : SystemSignalsOnly, ISignalApplyKnockback, ISignalOnCollisionEnter3D
    {
        /// <summary>
        /// Applies the impulse to the target and staggers it when the resulting speed change is large enough.
        /// </summary>
        void ISignalApplyKnockback.ApplyKnockback(Frame frame, EntityRef target, FPVector3 impulse, EntityRef source)
        {
            if (frame.Unsafe.TryGetPointer(target, out PhysicsBody3D* body) == false)
                return;

            if (frame.Unsafe.TryGetPointer(target, out Knockbackable* knockbackable) == false)
                return;

            KnockbackConfig config = frame.FindAsset(knockbackable->Config);

            body->WakeUp();
            body->AddLinearImpulse(impulse);

            FP speedChange = impulse.Magnitude / body->Mass;
            bool staggered = speedChange >= config.StaggerSpeedChange;
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
        /// Turns a moving body striking a character into knockback: the closing speed, scaled by how heavy the striker is
        /// relative to the character, becomes a speed change along the striker's motion, lifted a little by the up bias.
        /// Another character must be running into this one faster than the body-check speed to count.
        /// </summary>
        void ISignalOnCollisionEnter3D.OnCollisionEnter3D(Frame frame, CollisionInfo3D info)
        {
            if (info.IsStatic)
                return;

            EntityRef target = info.Entity;
            EntityRef striker = info.Other;

            if (frame.Unsafe.TryGetPointer(target, out Knockbackable* knockbackable) == false)
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

            // A character striker counts only by its own speed toward the target, so in a body check the runner knocks
            // the other character and is not knocked back by the same contact.
            bool isBodyCheck = frame.Has<Player>(striker);
            FPVector3 relativeVelocity = strikerBody->Velocity - targetBody->Velocity;
            FP closingSpeed = isBodyCheck
                ? FPVector3.Dot(strikerBody->Velocity, toTarget)
                : FPVector3.Dot(relativeVelocity, toTarget);

            FP minSpeed = isBodyCheck ? config.MinBodyCheckSpeed : config.MinHitSpeed;
            if (closingSpeed < minSpeed)
                return;

            FP massShare = strikerBody->Mass / (strikerBody->Mass + targetBody->Mass);
            FP speedChange = FPMath.Min(closingSpeed * massShare * config.HitScale, config.MaxHitSpeedChange);

            FPVector3 direction = relativeVelocity;
            direction.Y = FP._0;
            direction = direction.SqrMagnitude > FP._0 ? direction.Normalized : toTarget;

            FPVector3 impulse = (direction + FPVector3.Up * config.UpBias) * (speedChange * targetBody->Mass);
            frame.Signals.ApplyKnockback(target, impulse, striker);
        }
    }
}
