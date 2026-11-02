using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// Drives characters on foot from their input: turning, walking, running, jumping, slope footing and step-ups.
    /// Characters are dynamic physics bodies steered by forces, so pushes and collisions stay physical.
    /// Adapted from Friendslop's MovementSystem: movement is relative to the orbit camera's yaw, and the character
    /// turns toward where it moves instead of snapping to the camera. On top of that it runs crouching, sliding and
    /// the control tiers: a staggered character loses steering and slides on its momentum until the stagger ends.
    /// </summary>
    public unsafe class MovementSystem : SystemMainThreadFilter<MovementSystem.Filter>, ISignalStagger
    {
        /// <summary>
        /// How far above the crouched head, in meters, the headroom probe starts, so it clears the character's own collider.
        /// </summary>
        private readonly FP HeadroomProbeOffset = FP._0_02;

        /// <summary>
        /// The vertical velocity, in m/s, below which the character counts as falling rather than cresting a jump.
        /// </summary>
        private readonly FP FallVelocityThreshold = -FP._0_30;

        /// <summary>
        /// The highest normal Y a face ahead may have and still count as a step; flatter faces are ramps.
        /// </summary>
        private readonly FP MaxStepFaceNormalY = FP._0_20;

        /// <summary>
        /// The move direction length below which it is too small to tell where the character is heading.
        /// </summary>
        private readonly FP MinStepDirection = FP._0_10;

        /// <summary>
        /// The components the movement system updates for each character.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public PhysicsBody3D* PhysicsBody;
            public MovementData* MovementData;
            public GroundedTracker* GroundedTracker;
            public PlayerLook* PlayerLook;
        }

        /// <summary>
        /// Runs the character for this tick in its current state: staggered, sliding, or in control.
        /// In control it turns toward its move direction and walks, runs or crouches, jumps, and can start a slide.
        /// While grounded it also holds the character on slopes and, given move input, steps it up low ledges.
        /// A character no player controls gets no input and only brakes, falls and holds on slopes.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            // Knocked down, the ragdoll is the body; the character waits, hidden, until it gets up.
            if (frame.Has<RagdollOwner>(filter.Entity))
                return;

            MovementConfig config = frame.FindAsset(filter.MovementData->Config);
            MovementData* data = filter.MovementData;
            bool grounded = filter.GroundedTracker->IsGrounded;
            Input input = frame.TryGet(filter.Entity, out Player player) ? player.Input : default;

            if (data->Tier == ControlTier.Staggered && data->StaggerTimer.IsRunning(frame) == false)
            {
                data->Tier = ControlTier.InControl;
            }

            ApplyFallGravity(ref filter, config);

            if (data->Tier == ControlTier.Staggered)
            {
                Brake(frame, filter.PhysicsBody, config.StaggerDeceleration);
                return;
            }

            if (data->IsSliding)
            {
                UpdateSlide(frame, ref filter, config, input, grounded);
                return;
            }

            FPVector2 moveInput = FPVector2.ClampMagnitude(input.Move, FP._1);
            bool hasInput = moveInput.SqrMagnitude > FP._0;

            FPQuaternion cameraYaw = FPQuaternion.AngleAxis(filter.PlayerLook->Yaw, FPVector3.Up);
            FPVector3 moveDirection = cameraYaw * new FPVector3(moveInput.X, FP._0, moveInput.Y);

            if (grounded && input.Crouch.WasPressed && input.Run.IsDown && TryStartSlide(frame, ref filter, config))
                return;

            UpdateCrouch(frame, ref filter, config, input.Crouch.IsDown);

            // While holding a block the swing owns the facing, so left and right on the stick strafe.
            bool isHolding = frame.TryGet(filter.Entity, out Grabber grabber) && grabber.Held != EntityRef.None;
            if (hasInput && isHolding == false)
            {
                TurnToward(frame, ref filter, config, moveDirection);
            }

            FP maxSpeed = data->IsCrouching ? config.CrouchSpeedMax
                : input.Run.IsDown ? config.RunSpeedMax : config.WalkSpeedMax;
            DriveHorizontalVelocity(frame, ref filter, config, moveDirection, hasInput, maxSpeed);

            if (data->IsCrouching == false)
            {
                TryJump(frame, ref filter, config, grounded, input.Jump.WasPressed);
            }

            if (grounded)
            {
                HoldOnSlope(frame, ref filter);
            }

            if (grounded && hasInput)
            {
                TryStepUp(frame, ref filter, config, moveDirection);
            }
        }

        /// <summary>
        /// Starts a slide along the current horizontal velocity when the character runs fast enough, and returns true.
        /// The slide keeps the character's speed or lifts it to the slide speed, and crouches the capsule.
        /// </summary>
        private bool TryStartSlide(Frame frame, ref Filter filter, MovementConfig config)
        {
            PhysicsBody3D* body = filter.PhysicsBody;
            FPVector2 horizontal = GetHorizontalVelocity(body);
            FP speed = horizontal.Magnitude;
            if (speed < config.SlideMinSpeed)
                return false;

            FPVector2 direction = horizontal / speed;
            FP slideSpeed = FPMath.Max(speed, config.SlideSpeed);
            body->Velocity = new FPVector3(direction.X * slideSpeed, body->Velocity.Y, direction.Y * slideSpeed);

            MovementData* data = filter.MovementData;
            data->IsSliding = true;
            data->SlideDirection = direction;
            data->SlideTimer = FrameTimer.FromSeconds(frame, config.SlideDuration);
            SetCrouched(frame, ref filter, config, true);

            filter.Transform->Rotation = FPQuaternion.LookRotation(new FPVector3(direction.X, FP._0, direction.Y));
            frame.Events.SlideStarted(filter.Entity);
            return true;
        }

        /// <summary>
        /// Carries the slide on low braking without steering, until its timer ends or it slows to crouch speed.
        /// Jumping out of a slide stands the character up and jumps; otherwise it ends crouched if crouch is held.
        /// </summary>
        private void UpdateSlide(Frame frame, ref Filter filter, MovementConfig config, Input input, bool grounded)
        {
            MovementData* data = filter.MovementData;
            Brake(frame, filter.PhysicsBody, config.SlideDeceleration);

            if (grounded)
            {
                HoldOnSlope(frame, ref filter);
            }

            bool slowedDown = GetHorizontalVelocity(filter.PhysicsBody).SqrMagnitude
                < config.CrouchSpeedMax * config.CrouchSpeedMax;
            bool jumping = input.Jump.WasPressed && grounded && HasHeadroom(frame, ref filter, config);

            if (data->SlideTimer.IsRunning(frame) && slowedDown == false && jumping == false)
                return;

            data->IsSliding = false;
            UpdateCrouch(frame, ref filter, config, input.Crouch.IsDown && jumping == false);

            if (jumping && data->IsCrouching == false)
            {
                TryJump(frame, ref filter, config, grounded, true);
            }
        }

        /// <summary>
        /// Crouches while crouch is held, and stands back up once it is released and there is room overhead.
        /// </summary>
        private void UpdateCrouch(Frame frame, ref Filter filter, MovementConfig config, bool crouchHeld)
        {
            if (crouchHeld)
            {
                SetCrouched(frame, ref filter, config, true);
            }
            else if (filter.MovementData->IsCrouching && HasHeadroom(frame, ref filter, config))
            {
                SetCrouched(frame, ref filter, config, false);
            }
        }

        /// <summary>
        /// Resizes the capsule to the crouched or standing height, keeping its bottom at the character's feet.
        /// </summary>
        private void SetCrouched(Frame frame, ref Filter filter, MovementConfig config, bool crouched)
        {
            if (filter.MovementData->IsCrouching == crouched)
                return;

            filter.MovementData->IsCrouching = crouched;

            if (frame.Unsafe.TryGetPointer(filter.Entity, out PhysicsCollider3D* collider) == false)
                return;

            FP height = crouched ? config.CrouchHeight : config.StandingHeight;
            FP halfHeight = height / 2;
            collider->Shape = Shape3D.CreateCapsule(config.CapsuleRadius, halfHeight - config.CapsuleRadius,
                FPVector3.Up * halfHeight);
        }

        /// <summary>
        /// Returns true when nothing solid sits between the crouched head and the standing head height.
        /// </summary>
        private bool HasHeadroom(Frame frame, ref Filter filter, MovementConfig config)
        {
            FPVector3 origin = filter.Transform->Position + FPVector3.Up * (config.CrouchHeight + HeadroomProbeOffset);
            FP distance = config.StandingHeight - config.CrouchHeight;
            return frame.Physics3D.Raycast(origin, FPVector3.Up, distance, -1,
                QueryOptions.HitStatics | QueryOptions.HitDynamics | QueryOptions.HitKinematics).HasValue == false;
        }

        /// <summary>
        /// Brakes the horizontal velocity with the given deceleration, down to the physics sleep tolerance, and stops
        /// what is left below it. The stop sets the velocity rather than adding a force, so it never wakes the body: a
        /// resting character still sleeps, and one that can't (a carrier, kept awake by the block it holds) doesn't creep.
        /// </summary>
        private void Brake(Frame frame, PhysicsBody3D* body, FP deceleration)
        {
            FPVector2 horizontalVelocity = GetHorizontalVelocity(body);
            FP sleepTolerance = frame.SimulationConfig.Physics.LinearSleepTolerance;
            if (horizontalVelocity.SqrMagnitude <= sleepTolerance * sleepTolerance)
            {
                if (body->IsSleeping == false && horizontalVelocity != FPVector2.Zero)
                {
                    body->Velocity = new FPVector3(FP._0, body->Velocity.Y, FP._0);
                }

                return;
            }

            FPVector2 brakeForce = -horizontalVelocity * deceleration;
            body->AddForce(new FPVector3(brakeForce.X, FP._0, brakeForce.Y));
        }

        /// <summary>
        /// Staggers the character for at least <paramref name="duration"/> seconds: it loses steering and ends any slide.
        /// A longer stagger already running is kept.
        /// </summary>
        void ISignalStagger.Stagger(Frame frame, EntityRef entity, FP duration)
        {
            if (frame.Unsafe.TryGetPointer(entity, out MovementData* data) == false)
                return;

            FrameTimer timer = FrameTimer.FromSeconds(frame, duration);
            if (data->Tier != ControlTier.Staggered || timer.TargetFrame > data->StaggerTimer.TargetFrame)
            {
                data->StaggerTimer = timer;
            }

            data->Tier = ControlTier.Staggered;
            data->IsSliding = false;
        }

        /// <summary>
        /// Rotates the character about the vertical axis toward <paramref name="moveDirection"/> at the configured turn speed.
        /// </summary>
        private void TurnToward(Frame frame, ref Filter filter, MovementConfig config, FPVector3 moveDirection)
        {
            if (moveDirection.SqrMagnitude < MinStepDirection * MinStepDirection)
                return;

            FPQuaternion target = FPQuaternion.LookRotation(moveDirection);
            filter.Transform->Rotation = FPQuaternion.RotateTowards(filter.Transform->Rotation, target,
                config.TurnSpeed * frame.DeltaTime);
        }

        /// <summary>
        /// Pushes the horizontal velocity toward the requested move at up to <paramref name="maxSpeed"/>, or brakes
        /// without input. Top speed is enforced only while there is move input, so knockback and other impulses are
        /// not cut short. Braking stops below the physics sleep tolerance: the brake only decays speed toward zero and
        /// never reaches it, and any force wakes the body, so braking all the way down would keep a resting character
        /// from ever sleeping.
        /// </summary>
        private void DriveHorizontalVelocity(Frame frame, ref Filter filter, MovementConfig config,
            FPVector3 moveDirection, bool hasInput, FP maxSpeed)
        {
            PhysicsBody3D* body = filter.PhysicsBody;
            FPVector2 horizontalVelocity = GetHorizontalVelocity(body);

            if (hasInput)
            {
                FPVector2 targetVelocity = new FPVector2(moveDirection.X, moveDirection.Z) * maxSpeed;
                FPVector2 accelerationForce = (targetVelocity - horizontalVelocity) * config.Acceleration;
                body->AddForce(new FPVector3(accelerationForce.X, FP._0, accelerationForce.Y));
            }
            else
            {
                Brake(frame, body, config.Deceleration);
            }

            if (hasInput)
            {
                ClampHorizontalSpeed(body, maxSpeed);
            }
        }

        /// <summary>
        /// Caps the horizontal speed at <paramref name="maxSpeed"/>, keeping the vertical velocity.
        /// </summary>
        private void ClampHorizontalSpeed(PhysicsBody3D* body, FP maxSpeed)
        {
            FPVector2 horizontalVelocity = GetHorizontalVelocity(body);
            if (horizontalVelocity.SqrMagnitude <= maxSpeed * maxSpeed)
                return;

            FPVector2 clamped = horizontalVelocity.Normalized * maxSpeed;
            body->Velocity = new FPVector3(clamped.X, body->Velocity.Y, clamped.Y);
        }

        /// <summary>
        /// Returns the body's velocity on the ground plane, with world X and Z as the vector's X and Y.
        /// </summary>
        private FPVector2 GetHorizontalVelocity(PhysicsBody3D* body)
        {
            return new FPVector2(body->Velocity.X, body->Velocity.Z);
        }

        /// <summary>
        /// Applies the jump impulse and raises the jump event when a grounded character presses jump this tick.
        /// </summary>
        private void TryJump(Frame frame, ref Filter filter, MovementConfig config, bool grounded, bool jumpPressed)
        {
            if (grounded == false)
                return;

            if (jumpPressed == false)
                return;

            filter.PhysicsBody->AddLinearImpulse(FPVector3.Up * config.JumpForce);
            frame.Events.Jumped(filter.Entity);
        }

        /// <summary>
        /// Switches to the fall gravity scale while the character is falling, and back to normal gravity otherwise.
        /// </summary>
        private void ApplyFallGravity(ref Filter filter, MovementConfig config)
        {
            PhysicsBody3D* body = filter.PhysicsBody;
            body->GravityScale = body->Velocity.Y < FallVelocityThreshold ? config.FallGravityScale : FP._1;
        }

        /// <summary>
        /// Cancels the part of gravity that runs along the ground, so a grounded character does not slide down slopes.
        /// Skipped while the body sleeps, since sleep already holds it in place and the force would wake it.
        /// </summary>
        private void HoldOnSlope(Frame frame, ref Filter filter)
        {
            PhysicsBody3D* body = filter.PhysicsBody;
            if (body->IsSleeping)
                return;

            FPVector3 normal = filter.GroundedTracker->GroundNormal;
            if (normal.SqrMagnitude <= FP._0)
                return;

            normal = normal.Normalized;

            FPVector3 gravity = frame.SimulationConfig.Physics.Gravity * body->GravityScale;
            FPVector3 alongSurface = gravity - normal * FPVector3.Dot(gravity, normal);

            if (alongSurface.SqrMagnitude <= FP._0)
                return;

            body->AddForce(-alongSurface * body->Mass);
        }

        /// <summary>
        /// Lifts the character onto a low ledge ahead by raising its upward velocity, never teleporting it.
        /// It needs a steep face at the feet, clear space at knee height and a ledge top within the max step height.
        /// </summary>
        private void TryStepUp(Frame frame, ref Filter filter, MovementConfig config, FPVector3 moveDirection)
        {
            if (moveDirection.SqrMagnitude < MinStepDirection * MinStepDirection)
                return;

            FPVector3 position = filter.Transform->Position;
            FPVector3 direction = moveDirection.Normalized;

            FP reach = config.StepCheckForward;
            FP maxStep = config.MaxStepHeight;

            FPVector3 footOrigin = position + FPVector3.Up * config.StepFootOffset;
            FPVector3 kneeOrigin = position + FPVector3.Up * maxStep;
            FPVector3 probeOrigin = kneeOrigin + direction * reach;

            if (config.DebugStepUp)
            {
                Draw.Ray(footOrigin, direction * reach, ColorRGBA.Red);
                Draw.Ray(kneeOrigin, direction * reach, ColorRGBA.Green);
                Draw.Ray(probeOrigin, FPVector3.Down * maxStep, ColorRGBA.Blue);
            }

            Hit3D? footHit = frame.Physics3D.Raycast(footOrigin, direction, reach, -1,
                QueryOptions.HitStatics | QueryOptions.ComputeDetailedInfo);

            if (footHit.HasValue == false)
                return;

            if (footHit.Value.Normal.Y > MaxStepFaceNormalY)
                return;

            Hit3D? kneeHit = frame.Physics3D.Raycast(kneeOrigin, direction, reach, -1, QueryOptions.HitStatics);
            if (kneeHit.HasValue)
                return;

            Hit3D? surfaceHit = frame.Physics3D.Raycast(probeOrigin, FPVector3.Down, maxStep, -1,
                QueryOptions.HitStatics | QueryOptions.ComputeDetailedInfo);

            if (surfaceHit.HasValue == false)
                return;

            FP stepSize = surfaceHit.Value.Point.Y - position.Y;
            if (stepSize <= FP._0 || stepSize > maxStep)
                return;

            FP stepUpVelocity = stepSize * config.StepUpVelocityFactor;
            FPVector3 velocity = filter.PhysicsBody->Velocity;
            if (velocity.Y < stepUpVelocity)
            {
                filter.PhysicsBody->Velocity = new FPVector3(velocity.X, stepUpVelocity, velocity.Z);
            }
        }
    }
}
