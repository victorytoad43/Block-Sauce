using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// Drives characters on foot from their input: turning, walking, running, jumping, slope footing and step-ups.
    /// Characters are dynamic physics bodies steered by forces, so pushes and collisions stay physical.
    /// Adapted from Friendslop's MovementSystem: movement is relative to the orbit camera's yaw, and the character
    /// turns toward where it moves instead of snapping to the camera. Crouch, slide and control tiers come in Phase 1.
    /// </summary>
    public unsafe class MovementSystem : SystemMainThreadFilter<MovementSystem.Filter>
    {
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
        /// Turns the character toward its move direction and drives its walking, running, jumping and fall gravity.
        /// While grounded it also holds the character on slopes and, given move input, steps it up low ledges.
        /// A character no player controls gets no input and only brakes, falls and holds on slopes.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            MovementConfig config = frame.FindAsset(filter.MovementData->Config);
            bool grounded = filter.GroundedTracker->IsGrounded;
            Input input = frame.TryGet(filter.Entity, out Player player) ? player.Input : default;

            FPVector2 moveInput = FPVector2.ClampMagnitude(input.Move, FP._1);
            bool hasInput = moveInput.SqrMagnitude > FP._0;

            FPQuaternion cameraYaw = FPQuaternion.AngleAxis(filter.PlayerLook->Yaw, FPVector3.Up);
            FPVector3 moveDirection = cameraYaw * new FPVector3(moveInput.X, FP._0, moveInput.Y);

            if (hasInput)
            {
                TurnToward(frame, ref filter, config, moveDirection);
            }

            DriveHorizontalVelocity(frame, ref filter, config, moveDirection, hasInput, input.Run.IsDown);
            TryJump(frame, ref filter, config, grounded, input.Jump.WasPressed);
            ApplyFallGravity(ref filter, config);

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
        /// Pushes the horizontal velocity toward the requested move, or brakes without input or while steering is locked.
        /// Top speed is enforced only while there is move input, so knockback and other impulses are not cut short.
        /// Braking stops below the physics sleep tolerance: the brake only decays speed toward zero and never reaches it,
        /// and any force wakes the body, so braking all the way down would keep a resting character from ever sleeping.
        /// </summary>
        private void DriveHorizontalVelocity(Frame frame, ref Filter filter, MovementConfig config,
            FPVector3 moveDirection, bool hasInput, bool isRunning)
        {
            PhysicsBody3D* body = filter.PhysicsBody;
            FP maxSpeed = isRunning ? config.RunSpeedMax : config.WalkSpeedMax;
            FPVector2 horizontalVelocity = GetHorizontalVelocity(body);
            FP sleepTolerance = frame.SimulationConfig.Physics.LinearSleepTolerance;

            bool isSteering = hasInput && filter.MovementData->MovementLockTimer.IsRunning(frame) == false;
            if (isSteering)
            {
                FPVector2 targetVelocity = new FPVector2(moveDirection.X, moveDirection.Z) * maxSpeed;
                FPVector2 accelerationForce = (targetVelocity - horizontalVelocity) * config.Acceleration;
                body->AddForce(new FPVector3(accelerationForce.X, FP._0, accelerationForce.Y));
            }
            else if (horizontalVelocity.SqrMagnitude > sleepTolerance * sleepTolerance)
            {
                FPVector2 brakeForce = -horizontalVelocity * config.Deceleration;
                body->AddForce(new FPVector3(brakeForce.X, FP._0, brakeForce.Y));
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
