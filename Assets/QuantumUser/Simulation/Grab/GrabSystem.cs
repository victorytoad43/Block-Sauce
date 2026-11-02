using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// The joint tags that tell the grab's own joints apart from any other joints on a body.
    /// </summary>
    public enum EGrabJointTag
    {
        /// <summary>
        /// Any joint the grab does not own.
        /// </summary>
        None,
        /// <summary>
        /// The distance joint that tethers a held block to its carrier's hold pivot.
        /// </summary>
        Leash,
    }

    /// <summary>
    /// Lets characters grab, hold and swing, place and throw blocks.
    /// Adapted from Friendslop's GrabSystem: the held block chases a hand point by velocity each tick, swing-limited
    /// and leashed, so a swing physically hits players and towers. Block Sauce replaces Friendslop's look-driven hand
    /// with a swing angle the right stick drives: the hand orbits a pivot inside the character, and the body turns to
    /// follow a swing that goes far enough round. Grab while holding places the block. Throw is hold-to-aim: holding it
    /// raises the block ready and turns the swing input into aim (yaw turns the body, pitch sets the throw angle);
    /// releasing it throws, and releasing while airborne is the jumpshot.
    /// </summary>
    public unsafe class GrabSystem : SystemMainThreadFilter<GrabSystem.Filter>, ISignalDropHeld,
        ISignalOnComponentRemoved<Grabbable>, ISignalOnComponentRemoved<Grabber>
    {
        /// <summary>
        /// The physics layer an aimed block sits on: it collides with nothing (Project Settings › Physics).
        /// </summary>
        private const string AimedBlockLayer = "AimedBlock";

        /// <summary>
        /// The components the grab system updates for each character.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public PhysicsBody3D* PhysicsBody;
            public Player* Player;
            public PlayerLook* PlayerLook;
            public Grabber* Grabber;
        }

        /// <summary>
        /// Grabs on a grab press with empty hands. While holding: a grab press places the block, holding throw aims and
        /// releasing it throws, and otherwise the swing input moves the block around the character.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            if (frame.Has<RagdollOwner>(filter.Entity))
                return;

            Input* input = &filter.Player->Input;
            GrabConfig config = frame.FindAsset(filter.Grabber->Config);

            if (filter.Grabber->Held == EntityRef.None)
            {
                if (input->Grab.WasPressed && IsInControl(frame, filter.Entity))
                {
                    TryGrab(frame, ref filter, config);
                }

                return;
            }

            if (frame.Exists(filter.Grabber->Held) == false)
            {
                filter.Grabber->Held = EntityRef.None;
                return;
            }

            if (input->Grab.WasPressed)
            {
                Place(frame, ref filter, config);
                return;
            }

            if (filter.Grabber->IsAiming)
            {
                if (input->Throw.IsDown == false)
                {
                    Throw(frame, ref filter, config);
                    return;
                }

                UpdateAim(frame, ref filter, config, input->Swing);
            }
            else if (input->Throw.WasPressed)
            {
                filter.Grabber->IsAiming = true;
                filter.Grabber->AimPitch = config.AimPitchStart;
                // The view shows the block overhead while the real one waits in front, so it touches nothing until it
                // leaves the hands (DetachHeld restores its layer). A disabled collider would stop the body moving, and
                // turning a body into a trigger at runtime breaks the broadphase, so it moves to a layer that collides
                // with nothing.
                PhysicsCollider3D* collider = frame.Unsafe.GetPointer<PhysicsCollider3D>(filter.Grabber->Held);
                frame.Unsafe.GetPointer<Grabbable>(filter.Grabber->Held)->SolidLayer = collider->Layer;
                collider->Layer = frame.Layers.GetLayerIndex(AimedBlockLayer);
            }
            else
            {
                UpdateSwing(frame, ref filter, config, input->Swing);
            }

            UpdateHeld(frame, ref filter, config);
        }

        /// <summary>
        /// Takes hold of the nearest free block in the grab cone, switching off its gravity, banking its rotation
        /// relative to the character's facing and attaching the leash. The swing starts wherever the block is.
        /// </summary>
        private void TryGrab(Frame frame, ref Filter filter, GrabConfig config)
        {
            FPVector3 pivot = GetPivot(filter.Transform, config);
            EntityRef target = FindGrabTarget(frame, filter.Entity, pivot, GetBodyYaw(filter.Transform), config);
            if (target == EntityRef.None)
                return;

            Grabbable* grabbable = frame.Unsafe.GetPointer<Grabbable>(target);
            grabbable->Holder = filter.Entity;
            grabbable->LastHolder = filter.Entity;
            filter.Grabber->Held = target;

            PhysicsBody3D* heldBody = frame.Unsafe.GetPointer<PhysicsBody3D>(target);
            heldBody->WakeUp();
            heldBody->AllowSleeping = false;
            heldBody->Velocity = FPVector3.Zero;
            heldBody->AngularVelocity = FPVector3.Zero;
            heldBody->GravityScale = FP._0;
            heldBody->IsKinematic = false;

            Transform3D* heldTransform = frame.Unsafe.GetPointer<Transform3D>(target);
            FPQuaternion bodyYaw = GetBodyYaw(filter.Transform);

            FPVector3 localToBlock = FPQuaternion.Inverse(bodyYaw) * (heldTransform->Position - pivot);
            filter.Grabber->SwingYaw = FPMath.Clamp(FPMath.Atan2(localToBlock.X, localToBlock.Z) * FP.Rad2Deg,
                -config.SwingYawLimit, config.SwingYawLimit);
            FP flatDistance = FPMath.Sqrt(localToBlock.X * localToBlock.X + localToBlock.Z * localToBlock.Z);
            filter.Grabber->SwingPitch = FPMath.Clamp(FPMath.Atan2(localToBlock.Y, flatDistance) * FP.Rad2Deg,
                config.SwingPitchMin, config.SwingPitchMax);

            // The block is held level and square to the character: of its four sideways faces, the one already nearest
            // the character stays toward it, so the grab only straightens it.
            FPQuaternion swingYaw = GetHoldRotation(filter.Transform, filter.Grabber, FPQuaternion.Identity);
            filter.Grabber->HeldRotationOffset = GetQuarterTurn(swingYaw, heldTransform->Rotation);

            AttachLeash(frame, ref filter, config, target);

            frame.Events.Grabbed(filter.Entity, target);
        }

        /// <summary>
        /// Returns the nearest grabbable block nobody holds within grab range of the pivot and inside the cone
        /// in front of the character, or none.
        /// </summary>
        private EntityRef FindGrabTarget(Frame frame, EntityRef grabber, FPVector3 pivot, FPQuaternion bodyYaw, GrabConfig config)
        {
            FPVector3 forward = bodyYaw * FPVector3.Forward;
            FP minCos = FPMath.Cos(config.GrabConeHalfAngle * FP.Deg2Rad);

            HitCollection3D hits = frame.Physics3D.OverlapShape(pivot, FPQuaternion.Identity,
                Shape3D.CreateSphere(config.GrabRange), -1, QueryOptions.HitDynamics | QueryOptions.HitKinematics);

            EntityRef best = EntityRef.None;
            FP bestDistanceSqr = FP.UseableMax;
            for (int i = 0; i < hits.Count; i++)
            {
                EntityRef candidate = hits[i].Entity;
                if (candidate == grabber || candidate == best)
                    continue;

                if (frame.Unsafe.TryGetPointer(candidate, out Grabbable* grabbable) == false
                    || grabbable->Holder != EntityRef.None)
                    continue;

                FPVector3 toCandidate = frame.Get<Transform3D>(candidate).Position - pivot;
                FP distanceSqr = toCandidate.SqrMagnitude;
                if (distanceSqr >= bestDistanceSqr)
                    continue;

                FPVector3 flat = new FPVector3(toCandidate.X, FP._0, toCandidate.Z);
                if (flat.SqrMagnitude > FP.EN4 && FPVector3.Dot(flat.Normalized, forward) < minCos)
                    continue;

                best = candidate;
                bestDistanceSqr = distanceSqr;
            }

            return best;
        }

        /// <summary>
        /// Adds a disabled leash from the block to the carrier's hold pivot, sized to the hold radius plus slack.
        /// Any stale leash is removed first, since only the first leash found is ever enabled.
        /// </summary>
        private void AttachLeash(Frame frame, ref Filter filter, GrabConfig config, EntityRef held)
        {
            if (frame.Has<PhysicsJoints3D>(held) == false)
            {
                frame.Add<PhysicsJoints3D>(held);
            }

            PhysicsJoints3D* joints = frame.Unsafe.GetPointer<PhysicsJoints3D>(held);
            joints->RemoveJoints(frame, (int)EGrabJointTag.Leash);

            Joint3D leash = DistanceJoint3D.CreateJoint(config.LeashMinDistance, config.HoldRadius + config.LeashSlack,
                filter.Entity, FPVector3.Zero, FPVector3.Up * config.PivotHeight);
            leash.UserTag = (int)EGrabJointTag.Leash;
            leash.Enabled = false;

            joints->AddJoint(frame, ref leash);
        }

        /// <summary>
        /// Applies this tick's swing input to the swing angles, and turns the body when the swing goes past the
        /// threshold, moving the swing back by the same amount so the block keeps its place in the world.
        /// </summary>
        private void UpdateSwing(Frame frame, ref Filter filter, GrabConfig config, FPVector2 swing)
        {
            Grabber* grabber = filter.Grabber;
            grabber->SwingYaw = FPMath.Clamp(grabber->SwingYaw + swing.X, -config.SwingYawLimit, config.SwingYawLimit);
            grabber->SwingPitch = FPMath.Clamp(grabber->SwingPitch + swing.Y, config.SwingPitchMin, config.SwingPitchMax);

            FP excess = FPMath.Abs(grabber->SwingYaw) - config.BodyTurnThreshold;
            if (excess <= FP._0)
                return;

            FP turn = FPMath.Min(excess, config.BodyTurnSpeed * frame.DeltaTime) * FPMath.Sign(grabber->SwingYaw);
            grabber->SwingYaw -= turn;
            filter.Transform->Rotation = FPQuaternion.AngleAxis(turn, FPVector3.Up) * GetBodyYaw(filter.Transform);
        }

        /// <summary>
        /// While aiming, horizontal swing input turns the body directly and vertical input raises or lowers the aim, and
        /// the block is held centered and raised, ready to throw.
        /// </summary>
        private void UpdateAim(Frame frame, ref Filter filter, GrabConfig config, FPVector2 swing)
        {
            Grabber* grabber = filter.Grabber;
            grabber->AimPitch = FPMath.Clamp(grabber->AimPitch + swing.Y * config.AimPitchSensitivity,
                config.AimPitchMin, config.AimPitchMax);
            grabber->SwingYaw = FP._0;
            grabber->SwingPitch = config.AimReadyPitch;

            if (swing.X != FP._0)
            {
                filter.Transform->Rotation = FPQuaternion.AngleAxis(swing.X, FPVector3.Up) * GetBodyYaw(filter.Transform);
            }
        }

        /// <summary>
        /// Steers the held block toward the hand point on top of the carrier's velocity and turns it with the swing.
        /// Drops it when an engaged leash stretches too far or level geometry comes between it and the pivot.
        /// </summary>
        private void UpdateHeld(Frame frame, ref Filter filter, GrabConfig config)
        {
            EntityRef held = filter.Grabber->Held;
            Transform3D* heldTransform = frame.Unsafe.GetPointer<Transform3D>(held);
            FPVector3 pivot = GetPivot(filter.Transform, config);
            FPVector3 pivotToHeld = heldTransform->Position - pivot;

            bool leashEngaged = UpdateLeashEngagement(frame, held, config, pivotToHeld);
            if (leashEngaged && pivotToHeld.SqrMagnitude > config.GrabBreakDistance * config.GrabBreakDistance)
            {
                frame.Signals.DropHeld(filter.Entity);
                return;
            }

            FP tautDistance = config.HoldRadius + config.LeashSlack * FP._0_50;
            if (pivotToHeld.SqrMagnitude > tautDistance * tautDistance
                && IsHeldBlockBlocked(frame, pivot, heldTransform->Position))
            {
                frame.Signals.DropHeld(filter.Entity);
                return;
            }

            FPQuaternion swingRotation = GetSwingRotation(filter.Transform, filter.Grabber);
            FPVector3 handPosition = pivot + swingRotation * FPVector3.Forward * config.HoldRadius;
            FPVector3 targetPosition = GetSwingLimitedTarget(frame, config, pivot, pivotToHeld, handPosition);

            PhysicsBody3D* heldBody = frame.Unsafe.GetPointer<PhysicsBody3D>(held);
            FPVector3 carrierVelocity = filter.PhysicsBody->Velocity;
            FPVector3 correction = (targetPosition - heldTransform->Position) * config.HoldFollowGain;
            if (correction.SqrMagnitude > config.HoldMaxFollowSpeed * config.HoldMaxFollowSpeed)
            {
                correction = correction.Normalized * config.HoldMaxFollowSpeed;
            }

            FPVector3 velocity = carrierVelocity + correction;

            FPVector3 pivotNext = pivot + carrierVelocity * frame.DeltaTime;
            FPVector3 predictedFromPivot = heldTransform->Position + velocity * frame.DeltaTime - pivotNext;
            if (predictedFromPivot.SqrMagnitude < config.LeashMinDistance * config.LeashMinDistance)
            {
                if (predictedFromPivot.SqrMagnitude < FP.EN4)
                {
                    predictedFromPivot = handPosition - pivotNext;
                }

                FPVector3 clamped = pivotNext + predictedFromPivot.Normalized * config.LeashMinDistance;
                velocity = (clamped - heldTransform->Position) / frame.DeltaTime;
                FPVector3 clampedCorrection = velocity - carrierVelocity;
                if (clampedCorrection.SqrMagnitude > config.HoldMaxFollowSpeed * config.HoldMaxFollowSpeed)
                {
                    velocity = carrierVelocity + clampedCorrection.Normalized * config.HoldMaxFollowSpeed;
                }
            }

            ApplyCarryVelocity(config, filter.PhysicsBody, heldBody, velocity, pivotToHeld);

            heldTransform->Rotation = FPQuaternion.RotateTowards(heldTransform->Rotation,
                GetHoldRotation(filter.Transform, filter.Grabber, filter.Grabber->HeldRotationOffset),
                config.HoldTurnSpeed * frame.DeltaTime);
            heldBody->AngularVelocity = FPVector3.Zero;
        }

        /// <summary>
        /// Sets the held block down where it is, stopped and level. Over another block it is squared to that block's
        /// faces and, if it was pressed into it, lifted to sit on top, so letting go never leaves two blocks overlapping
        /// for the solver to shove apart.
        /// </summary>
        private void Place(Frame frame, ref Filter filter, GrabConfig config)
        {
            EntityRef held = filter.Grabber->Held;
            PhysicsBody3D* body = frame.Unsafe.GetPointer<PhysicsBody3D>(held);
            body->Velocity = FPVector3.Zero;
            body->AngularVelocity = FPVector3.Zero;

            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(held);
            transform->Rotation = ThrowHelper.GetBodyYaw(transform->Rotation);
            if (config.SettlePlacedOnBlockBelow && TryGetBlockBelow(frame, held, transform->Position, out EntityRef below))
            {
                Transform3D belowTransform = frame.Get<Transform3D>(below);
                FPQuaternion belowYaw = ThrowHelper.GetBodyYaw(belowTransform.Rotation);
                transform->Rotation = belowYaw * GetQuarterTurn(belowYaw, transform->Rotation);

                FP restingY = belowTransform.Position.Y + GetHalfHeight(frame, below) + GetHalfHeight(frame, held);
                if (transform->Position.Y < restingY)
                {
                    transform->Position.Y = restingY;
                }
            }

            Release(frame, filter.Entity, filter.Grabber);
            frame.Events.Placed(filter.Entity, held);
        }

        /// <summary>
        /// Finds a block under the held block's center, close enough that the held block is resting on or pressed into it.
        /// </summary>
        private static bool TryGetBlockBelow(Frame frame, EntityRef held, FPVector3 position, out EntityRef below)
        {
            below = EntityRef.None;
            FP reach = GetHalfHeight(frame, held) * 3 + FP._0_10;
            HitCollection3D hits = frame.Physics3D.RaycastAll(position, FPVector3.Down, reach, -1,
                QueryOptions.HitStatics | QueryOptions.HitDynamics | QueryOptions.HitKinematics);
            hits.SortCastDistance();
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].IsTrigger || hits[i].Entity == held)
                    continue;

                if (hits[i].IsStatic || frame.Has<Block>(hits[i].Entity) == false)
                    return false;

                below = hits[i].Entity;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns half a block's height, from its box collider.
        /// </summary>
        private static FP GetHalfHeight(Frame frame, EntityRef block)
        {
            return frame.Get<PhysicsCollider3D>(block).Shape.Box.Extents.Y;
        }

        /// <summary>
        /// Returns the quarter turn about world up that brings <paramref name="reference"/> closest to the heading of
        /// <paramref name="rotation"/>: which of a cube's four sideways faces points where the reference's forward does.
        /// </summary>
        private static FPQuaternion GetQuarterTurn(FPQuaternion reference, FPQuaternion rotation)
        {
            FPVector3 forward = FPQuaternion.Inverse(reference) * (rotation * FPVector3.Forward);
            FP degrees = FPMath.Atan2(forward.X, forward.Z) * FP.Rad2Deg;
            return FPQuaternion.AngleAxis(FPMath.Round(degrees / 90) * 90, FPVector3.Up);
        }

        /// <summary>
        /// Throws the held block along the aim (see <see cref="ThrowHelper.GetThrowVelocity"/>); thrown while airborne it
        /// is a jumpshot.
        /// </summary>
        private void Throw(Frame frame, ref Filter filter, GrabConfig config)
        {
            EntityRef held = filter.Grabber->Held;
            FPVector3 velocity = ThrowHelper.GetThrowVelocity(frame, filter.Entity, config, out bool isJumpshot);
            frame.Unsafe.GetPointer<PhysicsBody3D>(held)->Velocity = velocity;

            Release(frame, filter.Entity, filter.Grabber);
            StartThrowClear(frame, held, config);
            frame.Events.Thrown(filter.Entity, held, velocity, isJumpshot);
        }

        /// <summary>
        /// Lets a thrown block pass through its thrower and whatever it overlaps right now for a moment, so it leaves the
        /// hands at full speed instead of being pushed out backwards. Its contacts with other bodies are reported every
        /// tick from now on, which <see cref="GrabbableCollisionSystem"/> needs to ignore them.
        /// </summary>
        private void StartThrowClear(Frame frame, EntityRef held, GrabConfig config)
        {
            Grabbable* grabbable = frame.Unsafe.GetPointer<Grabbable>(held);
            grabbable->ThrowClear = FrameTimer.FromSeconds(frame, config.ThrowClearSeconds);

            FixedArray<EntityRef> clearOf = grabbable->ThrowClearOf;
            for (int i = 0; i < clearOf.Length; i++)
            {
                clearOf[i] = EntityRef.None;
            }

            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(held);
            HitCollection3D hits = frame.Physics3D.OverlapShape(transform->Position, transform->Rotation,
                frame.Get<PhysicsCollider3D>(held).Shape, -1, QueryOptions.HitDynamics | QueryOptions.HitKinematics);
            int count = 0;
            for (int i = 0; i < hits.Count && count < clearOf.Length; i++)
            {
                if (hits[i].IsTrigger == false && hits[i].Entity != held)
                {
                    clearOf[count++] = hits[i].Entity;
                }
            }

            frame.Physics3D.SetCallbacks(held, frame.Physics3D.GetCallbacks(held) | CallbackFlags.OnDynamicCollision);
        }

        /// <summary>
        /// Empties the grabber's hands and hands the block back to gravity, keeping whatever velocity it has.
        /// </summary>
        private void Release(Frame frame, EntityRef holder, Grabber* grabber)
        {
            EntityRef held = grabber->Held;
            grabber->Held = EntityRef.None;
            grabber->IsAiming = false;
            if (frame.Exists(held))
            {
                DetachHeld(frame, held);
            }
        }

        /// <summary>
        /// Sets the held block's velocity and slows a carrier driving into it, scaled by the block's mass.
        /// </summary>
        private void ApplyCarryVelocity(GrabConfig config, PhysicsBody3D* carrierBody,
            PhysicsBody3D* heldBody, FPVector3 targetVelocity, FPVector3 pivotToHeld)
        {
            FPVector3 deltaVelocity = targetVelocity - heldBody->Velocity;
            heldBody->Velocity = targetVelocity;

            FPVector3 armDirection = pivotToHeld;
            armDirection.Y = FP._0;
            FP reach = armDirection.Magnitude;
            if (reach <= FP._0)
                return;

            armDirection /= reach;

            FPVector3 flatDelta = deltaVelocity;
            flatDelta.Y = FP._0;
            FP push = FPVector3.Dot(flatDelta, armDirection);
            if (push <= FP._0)
                return;

            FP approach = FPVector3.Dot(carrierBody->Velocity, armDirection);
            if (approach <= FP._0)
                return;

            FP drop = FPMath.Min(push * heldBody->Mass * config.CarryReactionScale, approach);
            carrierBody->Velocity -= armDirection * drop;
        }

        /// <summary>
        /// Enables the leash once the block first sits within its length range, and returns true while it is enabled.
        /// </summary>
        private bool UpdateLeashEngagement(Frame frame, EntityRef held, GrabConfig config, FPVector3 pivotToHeld)
        {
            if (frame.Unsafe.TryGetPointer(held, out PhysicsJoints3D* joints) == false)
                return false;

            Joint3D* buffer = joints->GetJoints(frame, out int count);
            for (int i = 0; i < count; i++)
            {
                if (buffer[i].UserTag != (int)EGrabJointTag.Leash)
                    continue;

                if (buffer[i].Enabled)
                    return true;

                FP min = config.LeashMinDistance;
                FP max = config.HoldRadius + config.LeashSlack;
                FP distanceSqr = pivotToHeld.SqrMagnitude;
                if (distanceSqr >= min * min && distanceSqr <= max * max)
                {
                    buffer[i].Enabled = true;
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Returns the hand position, limited to how far the block can swing around the pivot this tick.
        /// </summary>
        private FPVector3 GetSwingLimitedTarget(Frame frame, GrabConfig config, FPVector3 pivot,
            FPVector3 pivotToHeld, FPVector3 handPosition)
        {
            if (pivotToHeld.SqrMagnitude < FP.EN4)
                return handPosition;

            FPVector3 desired = handPosition - pivot;
            FP desiredDistance = desired.Magnitude;
            FPVector3 currentDirection = pivotToHeld.Normalized;
            FPQuaternion fullSwing = FPQuaternion.FromToRotation(currentDirection, desired / desiredDistance);
            FPQuaternion limitedSwing = FPQuaternion.RotateTowards(FPQuaternion.Identity, fullSwing,
                config.HoldSwingSpeed * frame.DeltaTime);
            return pivot + limitedSwing * currentDirection * desiredDistance;
        }

        /// <summary>
        /// Returns true if level geometry lies between the pivot and the held block.
        /// </summary>
        private bool IsHeldBlockBlocked(Frame frame, FPVector3 origin, FPVector3 blockPosition)
        {
            FPVector3 toBlock = blockPosition - origin;
            FP distance = toBlock.Magnitude;
            if (distance <= FP._0)
                return false;

            return frame.Physics3D.Raycast(origin, toBlock / distance, distance, -1, QueryOptions.HitStatics).HasValue;
        }

        /// <summary>
        /// Returns the hold pivot inside the character.
        /// </summary>
        private static FPVector3 GetPivot(Transform3D* transform, GrabConfig config)
        {
            return transform->Position + FPVector3.Up * config.PivotHeight;
        }

        /// <summary>
        /// Returns the character's facing as a rotation about world up only.
        /// </summary>
        private static FPQuaternion GetBodyYaw(Transform3D* transform)
        {
            return ThrowHelper.GetBodyYaw(transform->Rotation);
        }

        /// <summary>
        /// Returns the rotation from the character's facing to the swing direction; positive pitch swings up.
        /// </summary>
        private static FPQuaternion GetSwingRotation(Transform3D* transform, Grabber* grabber)
        {
            return GetBodyYaw(transform) * FPQuaternion.Euler(-grabber->SwingPitch, grabber->SwingYaw, FP._0);
        }

        /// <summary>
        /// Returns the held block's rotation: level, turned to face the character along the swing, plus the grab's
        /// quarter turn.
        /// </summary>
        private static FPQuaternion GetHoldRotation(Transform3D* transform, Grabber* grabber, FPQuaternion quarterTurn)
        {
            return GetBodyYaw(transform) * FPQuaternion.AngleAxis(grabber->SwingYaw, FPVector3.Up) * quarterTurn;
        }

        /// <summary>
        /// Returns true unless the character is staggered.
        /// </summary>
        private static bool IsInControl(Frame frame, EntityRef entity)
        {
            return frame.TryGet(entity, out MovementData movement) == false || movement.Tier == ControlTier.InControl;
        }

        /// <summary>
        /// Clears the block's holder, disables its leash and hands it back to gravity.
        /// </summary>
        private void DetachHeld(Frame frame, EntityRef held)
        {
            Grabbable* grabbable = frame.Unsafe.GetPointer<Grabbable>(held);
            grabbable->Holder = EntityRef.None;
            frame.Unsafe.GetPointer<PhysicsCollider3D>(held)->Layer = grabbable->SolidLayer;

            DisableLeash(frame, held);

            PhysicsBody3D* body = frame.Unsafe.GetPointer<PhysicsBody3D>(held);
            body->GravityScale = FP._1;
            body->IsKinematic = false;
            body->AllowSleeping = true;
        }

        /// <summary>
        /// Disables the block's leash, if it has one; safe inside physics callbacks.
        /// </summary>
        private void DisableLeash(Frame frame, EntityRef held)
        {
            if (frame.Unsafe.TryGetPointer(held, out PhysicsJoints3D* joints) == false)
                return;

            if (joints->TryGetJoints(frame, out Joint3D* buffer, out int count) == false)
                return;

            for (int i = 0; i < count; i++)
            {
                if (buffer[i].UserTag == (int)EGrabJointTag.Leash)
                {
                    buffer[i].Enabled = false;
                }
            }
        }

        /// <summary>
        /// Makes the holder let go of what it carries, keeping the block's velocity, and raises the Dropped event.
        /// </summary>
        void ISignalDropHeld.DropHeld(Frame frame, EntityRef holder)
        {
            if (frame.Unsafe.TryGetPointer(holder, out Grabber* grabber) == false)
                return;

            EntityRef held = grabber->Held;
            if (held == EntityRef.None)
                return;

            Release(frame, holder, grabber);
            frame.Events.Dropped(holder, held);
        }

        /// <summary>
        /// Empties the holder's hands when a held block stops being grabbable, restoring its gravity unless it is
        /// being destroyed.
        /// </summary>
        void ISignalOnComponentRemoved<Grabbable>.OnRemoved(Frame frame, EntityRef entity, Grabbable* component)
        {
            EntityRef holder = component->Holder;
            if (holder == EntityRef.None)
                return;

            if (frame.Unsafe.TryGetPointer(holder, out Grabber* grabber) && grabber->Held == entity)
            {
                grabber->Held = EntityRef.None;
                frame.Events.Dropped(holder, entity);
            }

            if (frame.DestroyPending(entity))
                return;

            DisableLeash(frame, entity);

            PhysicsBody3D* body = frame.Unsafe.GetPointer<PhysicsBody3D>(entity);
            body->GravityScale = FP._1;
            body->AllowSleeping = true;
        }

        /// <summary>
        /// Releases what a holder carried when it loses its grabber, for example on being destroyed.
        /// </summary>
        void ISignalOnComponentRemoved<Grabber>.OnRemoved(Frame frame, EntityRef entity, Grabber* component)
        {
            EntityRef held = component->Held;
            if (held == EntityRef.None || frame.Exists(held) == false)
                return;

            DetachHeld(frame, held);
        }
    }
}
