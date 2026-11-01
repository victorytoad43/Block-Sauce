using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// Swaps a knocked-down character for a spawned ragdoll body until its knockdown runs out, then stands it up where
    /// its hips came to rest. The hidden character rides the hips, so the camera and anything anchored to it keep
    /// working. Adapted from Friendslop's RagdollSystem: every limb inherits the character's velocity, a still body
    /// goes to sleep, carrying a body is a mode toggle, and the get-up looks for ground and a clear spot to stand in.
    /// </summary>
    public unsafe class RagdollSystem : SystemMainThreadFilter<RagdollSystem.Filter>, ISignalSetRagdoll,
        ISignalOnComponentRemoved<RagdollOwner>
    {
        /// <summary>
        /// The height, in meters, above the hips the get-up's ground probe starts, so it finds ground the hips sank into.
        /// </summary>
        private readonly FP GroundProbeLift = FP._0_50;

        /// <summary>
        /// The number of directions searched on each ring around the hips for a clear spot to stand in.
        /// </summary>
        private const int ClearSpotDirections = 8;

        /// <summary>
        /// The components the ragdoll system updates for each knocked-down character.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public RagdollOwner* RagdollOwner;
            public Transform3D* Transform;
        }

        /// <summary>
        /// Carries the hidden character along with the hips, keeps a carried body down, applies struggles, puts a
        /// still body to sleep, and gets the character up when the knockdown runs out.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            RagdollConfig config = GetConfig(frame, filter.Entity);

            if (frame.Unsafe.TryGetPointer(filter.RagdollOwner->Anchor, out Transform3D* anchorTransform))
            {
                filter.Transform->Position = anchorTransform->Position;
            }

            if (HoldDownWhileCarried(frame, ref filter, config))
                return;

            TryStruggle(frame, ref filter, config, &filter.RagdollOwner->Timer);
            UpdateRest(frame, filter.RagdollOwner, config);

            if (filter.RagdollOwner->Timer.IsRunning(frame))
                return;

            DisableRagdoll(frame, filter.Entity);
        }

        /// <summary>
        /// Knocks a ragdollable character down or gets it back up, ignoring a request that matches its current state.
        /// </summary>
        void ISignalSetRagdoll.SetRagdoll(Frame frame, EntityRef entity, QBoolean active)
        {
            bool isRagdolled = frame.Has<RagdollOwner>(entity);
            if (active == isRagdolled)
                return;

            if (active)
            {
                if (frame.Has<Ragdollable>(entity))
                {
                    EnableRagdoll(frame, entity);
                }
            }
            else
            {
                DisableRagdoll(frame, entity);
            }
        }

        /// <summary>
        /// Destroys the body whenever a character stops being down, including when the character itself is destroyed.
        /// </summary>
        void ISignalOnComponentRemoved<RagdollOwner>.OnRemoved(Frame frame, EntityRef entity, RagdollOwner* component)
        {
            DestroyBody(frame, component->Body);
        }

        /// <summary>
        /// While any limb is held: runs the carry timer (started on the first carry of this knockdown), which Jump
        /// presses shorten, and keeps the knockdown at the carried release delay. When the carry timer runs out the
        /// body breaks free: every carrier drops it and the knockdown runs on from there. Returns true while carried.
        /// </summary>
        private bool HoldDownWhileCarried(Frame frame, ref Filter filter, RagdollConfig config)
        {
            RagdollOwner* ragdoll = filter.RagdollOwner;
            if (IsCarried(frame, ragdoll->Body) == false)
                return false;

            if (ragdoll->CarryTimer.IsSet == false)
            {
                ragdoll->CarryTimer = FrameTimer.FromSeconds(frame, config.CarryHoldSeconds);
            }

            TryStruggle(frame, ref filter, config, &ragdoll->CarryTimer);

            if (ragdoll->CarryTimer.IsRunning(frame) == false)
            {
                ReleaseCarriers(frame, ragdoll->Body);
                frame.Events.BrokeFree(filter.Entity);
                return false;
            }

            ragdoll->Timer = FrameTimer.FromSeconds(frame, config.CarriedReleaseDelay);
            ragdoll->StillTicks = 0;
            return true;
        }

        /// <summary>
        /// Makes everyone holding a limb of the body let go.
        /// </summary>
        private static void ReleaseCarriers(Frame frame, EntityRef root)
        {
            foreach ((EntityRef limb, int _) in frame.GetEntityGroupIterator(root))
            {
                if (frame.TryGet(limb, out Grabbable grabbable) && grabbable.Holder != EntityRef.None)
                {
                    frame.Signals.DropHeld(grabbable.Holder);
                }
            }
        }

        /// <summary>
        /// Takes the struggle recovery off a timer (the knockdown, or the carry while carried) on a Jump press, then
        /// ignores presses for the cooldown.
        /// </summary>
        private void TryStruggle(Frame frame, ref Filter filter, RagdollConfig config, FrameTimer* timer)
        {
            RagdollOwner* ragdoll = filter.RagdollOwner;

            if (config.StruggleRecovery <= FP._0 || ragdoll->StruggleCooldownTimer.IsRunning(frame))
                return;

            if (frame.Unsafe.TryGetPointer(filter.Entity, out Player* player) == false || player->Input.Jump.WasPressed == false)
                return;

            FP remaining = timer->RemainingSeconds(frame);
            FP shortened = FPMath.Max(remaining - config.StruggleRecovery, config.MinStruggleRemainder);

            *timer = FrameTimer.FromSeconds(frame, shortened);
            ragdoll->StruggleCooldownTimer = FrameTimer.FromSeconds(frame, config.StruggleCooldown);

            frame.Events.Struggled(filter.Entity);
        }

        /// <summary>
        /// Puts every limb to sleep once the whole body has stayed under the rest speeds for the rest time; any limb
        /// moving again (a hit, a block landing on it) wakes the body and restarts the count.
        /// </summary>
        private void UpdateRest(Frame frame, RagdollOwner* ragdoll, RagdollConfig config)
        {
            if (frame.Exists(ragdoll->Body) == false)
                return;

            if (IsBodyStill(frame, ragdoll->Body, config) == false)
            {
                ragdoll->StillTicks = 0;
                ragdoll->Asleep = false;
                return;
            }

            if (ragdoll->Asleep)
                return;

            ragdoll->StillTicks++;
            if (ragdoll->StillTicks * frame.DeltaTime < config.RestSeconds)
                return;

            foreach ((EntityRef limb, int _) in frame.GetEntityGroupIterator(ragdoll->Body))
            {
                if (frame.Unsafe.TryGetPointer(limb, out PhysicsBody3D* body))
                {
                    body->Sleep();
                }
            }

            ragdoll->Asleep = true;
        }

        /// <summary>
        /// Returns true when every limb is asleep or under the rest speeds.
        /// </summary>
        private static bool IsBodyStill(Frame frame, EntityRef root, RagdollConfig config)
        {
            FP restSpeedSqr = config.RestSpeed * config.RestSpeed;
            FP restAngularSqr = config.RestAngularSpeed * config.RestAngularSpeed;

            foreach ((EntityRef limb, int _) in frame.GetEntityGroupIterator(root))
            {
                if (frame.Unsafe.TryGetPointer(limb, out PhysicsBody3D* body) == false || body->IsSleeping)
                    continue;

                if (body->Velocity.SqrMagnitude > restSpeedSqr || body->AngularVelocity.SqrMagnitude > restAngularSqr)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Takes the character out of physics, drops what it holds and spawns its ragdoll moving at its velocity, tagging
        /// every piece back to it. Starts the knockdown and raises RagdollChanged.
        /// </summary>
        private void EnableRagdoll(Frame frame, EntityRef entity)
        {
            RagdollConfig config = GetConfig(frame, entity);
            Transform3D characterTransform = frame.Get<Transform3D>(entity);

            frame.Signals.DropHeld(entity);

            PhysicsBody3D* characterBody = frame.Unsafe.GetPointer<PhysicsBody3D>(entity);
            FPVector3 velocity = characterBody->Velocity;
            characterBody->Velocity = FPVector3.Zero;
            characterBody->AngularVelocity = FPVector3.Zero;
            characterBody->Enabled = false;
            frame.Unsafe.GetPointer<PhysicsCollider3D>(entity)->Enabled = false;

            EntityRef body = SpawnBody(frame, entity, config, characterTransform, velocity, out EntityRef anchor);

            frame.Add(entity, new RagdollOwner
            {
                Timer = FrameTimer.FromSeconds(frame, config.Duration),
                Body = body,
                Anchor = anchor,
            });

            TrackGroundFromAnchor(frame, entity, anchor);

            frame.Events.RagdollChanged(entity, body, true);
        }

        /// <summary>
        /// Gives the hips the character's ground tracking, so a body that falls out of the arena is put back.
        /// </summary>
        private static void TrackGroundFromAnchor(Frame frame, EntityRef entity, EntityRef anchor)
        {
            if (anchor == EntityRef.None || frame.Unsafe.TryGetPointer(entity, out GroundedTracker* tracker) == false)
                return;

            frame.Add(anchor, new GroundedTracker
            {
                Config = tracker->Config,
                LastGroundedPosition = tracker->LastGroundedPosition,
            });
        }

        /// <summary>
        /// Stands the character up on the ground under its hips, moved to the nearest clear spot if a block or wall is
        /// in the way, destroys the body and restores its upright physics. Raises RagdollChanged.
        /// </summary>
        private void DisableRagdoll(Frame frame, EntityRef entity)
        {
            RagdollConfig config = GetConfig(frame, entity);
            RagdollOwner* ragdoll = frame.Unsafe.GetPointer<RagdollOwner>(entity);
            EntityRef body = ragdoll->Body;
            Transform3D* characterTransform = frame.Unsafe.GetPointer<Transform3D>(entity);

            // Stand facing the way the body lay, flattened to a yaw, so the get-up doesn't spin the character around.
            characterTransform->Rotation = GetLyingYaw(frame, ragdoll->Anchor, characterTransform->Rotation);

            if (frame.Unsafe.TryGetPointer(ragdoll->Anchor, out Transform3D* anchorTransform))
            {
                FPVector3 target = anchorTransform->Position
                    + characterTransform->Rotation * FPVector3.Forward * config.GetUpForwardOffset;
                characterTransform->Position = FindStandingSpot(frame, entity, target, config);
            }

            // Removing the owner destroys the body (see OnRemoved).
            frame.Remove<RagdollOwner>(entity);

            PhysicsBody3D* characterBody = frame.Unsafe.GetPointer<PhysicsBody3D>(entity);
            characterBody->Enabled = true;
            characterBody->WakeUp();
            characterBody->Velocity = FPVector3.Zero;
            characterBody->AngularVelocity = FPVector3.Zero;
            characterBody->RotationFreeze = config.UprightFreeze;
            frame.Unsafe.GetPointer<PhysicsCollider3D>(entity)->Enabled = true;

            frame.Events.RagdollChanged(entity, body, false);
        }

        /// <summary>
        /// Returns the feet position for getting up: on the ground under the hips if the character fits there, or else
        /// on the ground at the first clear spot on rings around them. Falls back to the hips' own spot.
        /// </summary>
        private FPVector3 FindStandingSpot(Frame frame, EntityRef entity, FPVector3 hips, RagdollConfig config)
        {
            FPVector3 fallback = hips;
            if (TryGroundAt(frame, entity, hips, config, out FPVector3 feet))
            {
                fallback = feet;
                if (FitsAt(frame, entity, feet))
                    return feet;
            }

            for (int ring = 0; ring < config.ClearSpotRadii.Length; ring++)
            {
                for (int i = 0; i < ClearSpotDirections; i++)
                {
                    FPQuaternion turn = FPQuaternion.AngleAxis(i * (FP)360 / ClearSpotDirections, FPVector3.Up);
                    FPVector3 probe = hips + turn * FPVector3.Forward * config.ClearSpotRadii[ring];

                    if (TryGroundAt(frame, entity, probe, config, out FPVector3 candidate) && FitsAt(frame, entity, candidate))
                        return candidate;
                }
            }

            return fallback;
        }

        /// <summary>
        /// Finds the highest surface under a point that is not part of this character's own body.
        /// </summary>
        private bool TryGroundAt(Frame frame, EntityRef entity, FPVector3 point, RagdollConfig config, out FPVector3 ground)
        {
            FPVector3 origin = point + FPVector3.Up * GroundProbeLift;
            HitCollection3D hits = frame.Physics3D.RaycastAll(origin, FPVector3.Down, config.GroundProbeDistance + GroundProbeLift,
                -1, QueryOptions.HitAll | QueryOptions.ComputeDetailedInfo);
            hits.SortCastDistance();

            for (int i = 0; i < hits.Count; i++)
            {
                if (IsOwnBody(frame, entity, hits[i].Entity))
                    continue;

                ground = hits[i].Point;
                return true;
            }

            ground = point;
            return false;
        }

        /// <summary>
        /// Returns true when the character's own collider placed with its feet at <paramref name="feet"/> overlaps
        /// nothing but its own body.
        /// </summary>
        private bool FitsAt(Frame frame, EntityRef entity, FPVector3 feet)
        {
            PhysicsCollider3D collider = frame.Get<PhysicsCollider3D>(entity);
            HitCollection3D hits = frame.Physics3D.OverlapShape(feet + FPVector3.Up * FP._0_02, FPQuaternion.Identity,
                collider.Shape, -1, QueryOptions.HitAll);

            for (int i = 0; i < hits.Count; i++)
            {
                if (IsOwnBody(frame, entity, hits[i].Entity) == false)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true for the character itself and for limbs of its own ragdoll.
        /// </summary>
        private static bool IsOwnBody(Frame frame, EntityRef entity, EntityRef hit)
        {
            return hit == entity || (frame.TryGet(hit, out RagdollLimb limb) && limb.Owner == entity);
        }

        /// <summary>
        /// Returns the yaw the hips face while lying, or the current rotation when the hips point straight up or down.
        /// </summary>
        private static FPQuaternion GetLyingYaw(Frame frame, EntityRef anchor, FPQuaternion fallback)
        {
            if (frame.TryGet(anchor, out Transform3D hips) == false)
                return fallback;

            // Lying down, the hips' up axis runs along the body toward the head: that is the way the character faces
            // once it stands.
            FPVector3 along = hips.Rotation * FPVector3.Up;
            along.Y = FP._0;
            if (along.SqrMagnitude < FP.EN2)
                return fallback;

            return FPQuaternion.LookRotation(along.Normalized, FPVector3.Up);
        }

        /// <summary>
        /// Creates the ragdoll with its root at the character's feet and every limb moving at the character's velocity
        /// plus a random tumble, tags every piece back to the character, and makes the limbs grabbable when the config
        /// allows carrying bodies. Returns the root, or None without a prototype; the anchor is the hips.
        /// </summary>
        private EntityRef SpawnBody(Frame frame, EntityRef entity, RagdollConfig config, Transform3D target,
            FPVector3 velocity, out EntityRef anchor)
        {
            anchor = EntityRef.None;

            if (config.RagdollPrototype == default)
                return EntityRef.None;

            EntityRef root = frame.Create(config.RagdollPrototype);
            Transform3D rootTransform = frame.Get<Transform3D>(root);
            FPQuaternion rotationDelta = target.Rotation * FPQuaternion.Inverse(rootTransform.Rotation);

            foreach ((EntityRef limb, int _) in frame.GetEntityGroupIterator(root))
            {
                if (limb != root)
                {
                    Place(frame, limb, rootTransform.Position, target.Position, rotationDelta, velocity, config.SpinImpulse);
                }

                frame.Add(limb, new RagdollLimb { Owner = entity });

                if (frame.Has<PhysicsBody3D>(limb) == false)
                    continue;

                if (config.AllowGrabbingBodies)
                {
                    frame.Add(limb, new Grabbable());
                }

                if (anchor == EntityRef.None)
                {
                    anchor = limb;
                }
            }

            Place(frame, root, rootTransform.Position, target.Position, rotationDelta, velocity, config.SpinImpulse);
            if (frame.Has<RagdollLimb>(root) == false)
            {
                frame.Add(root, new RagdollLimb { Owner = entity });
            }

            if (anchor == EntityRef.None)
            {
                anchor = root;
            }

            return root;
        }

        /// <summary>
        /// Moves an entity rigidly from the prototype's pose onto the character's pose; a body also takes the
        /// character's velocity and its own random tumble.
        /// </summary>
        private static void Place(Frame frame, EntityRef entity, FPVector3 pivot, FPVector3 destination,
            FPQuaternion rotationDelta, FPVector3 velocity, FP spin)
        {
            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(entity);
            transform->Position = destination + rotationDelta * (transform->Position - pivot);
            transform->Rotation = rotationDelta * transform->Rotation;

            if (frame.Unsafe.TryGetPointer(entity, out PhysicsBody3D* body) == false)
                return;

            FPVector3 tumble = new FPVector3(frame.RNG->Next(-FP._1, FP._1), frame.RNG->Next(-FP._1, FP._1),
                frame.RNG->Next(-FP._1, FP._1));
            body->Velocity = velocity;
            body->AngularVelocity = tumble.SqrMagnitude > FP.EN4 ? tumble.Normalized * spin : FPVector3.Zero;
        }

        /// <summary>
        /// Returns true while any limb of the body is held by someone.
        /// </summary>
        public static bool IsCarried(Frame frame, EntityRef root)
        {
            if (frame.Exists(root) == false)
                return false;

            foreach ((EntityRef limb, int _) in frame.GetEntityGroupIterator(root))
            {
                if (frame.TryGet(limb, out Grabbable grabbable) && grabbable.Holder != EntityRef.None)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Destroys a ragdoll's root and each of its limbs, doing nothing if the root is already gone. The limbs go one
        /// by one because destroying the root alone leaves them behind.
        /// </summary>
        public static void DestroyBody(Frame frame, EntityRef root)
        {
            if (frame.Exists(root) == false)
                return;

            foreach ((EntityRef limb, int _) in frame.GetEntityGroupIterator(root))
            {
                if (limb != root)
                {
                    frame.Destroy(limb);
                }
            }

            frame.Destroy(root);
        }

        /// <summary>
        /// Returns the ragdoll config of a ragdollable character.
        /// </summary>
        private static RagdollConfig GetConfig(Frame frame, EntityRef entity)
        {
            return frame.FindAsset(frame.Get<Ragdollable>(entity).Config);
        }
    }
}
