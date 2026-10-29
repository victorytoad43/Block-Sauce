using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// The on-foot movement tuning for walking, running, turning, jumping, falling and stepping up.
    /// Defaults start from Friendslop's shipped values; Phase 1 tunes them for Block Sauce.
    /// </summary>
    public class MovementConfig : AssetObject
    {
        /// <summary>
        /// The top horizontal speed while walking, in m/s.
        /// </summary>
        [Header("Movement")]
        public FP WalkSpeedMax = 3;
        /// <summary>
        /// The top horizontal speed while running, in m/s.
        /// </summary>
        public FP RunSpeedMax = 6;

        /// <summary>
        /// How hard the character pushes toward its target horizontal velocity, as force per m/s of difference.
        /// </summary>
        public FP Acceleration = 5;
        /// <summary>
        /// How hard the character brakes without steering, as force per m/s of horizontal speed.
        /// </summary>
        public FP Deceleration = 5;
        /// <summary>
        /// How fast the character turns to face its move direction, in degrees per second.
        /// </summary>
        public FP TurnSpeed = 720;
        /// <summary>
        /// The upward impulse applied when the character jumps.
        /// </summary>
        public FP JumpForce = 5;

        /// <summary>
        /// The gravity multiplier while falling, so jumps come down faster than they go up.
        /// </summary>
        public FP FallGravityScale = FP._2 + FP._0_50;

        /// <summary>
        /// The capsule's radius, in meters. Must match the player prototype's collider.
        /// </summary>
        [Header("Capsule")]
        public FP CapsuleRadius = (FP)35 / 100;
        /// <summary>
        /// The standing capsule height, in meters (Guy at game scale). Must match the player prototype's collider.
        /// </summary>
        public FP StandingHeight = (FP)166 / 100;
        /// <summary>
        /// The capsule height while crouching or sliding, in meters.
        /// </summary>
        public FP CrouchHeight = (FP)110 / 100;

        /// <summary>
        /// The top horizontal speed while crouching, in m/s.
        /// </summary>
        [Header("Crouch and Slide")]
        public FP CrouchSpeedMax = (FP)3 / 2;
        /// <summary>
        /// The horizontal speed, in m/s, a running character needs before crouch starts a slide.
        /// </summary>
        public FP SlideMinSpeed = 4;
        /// <summary>
        /// The horizontal speed a slide starts at, in m/s; a faster character keeps its own speed.
        /// </summary>
        public FP SlideSpeed = (FP)15 / 2;
        /// <summary>
        /// How long a slide lasts, in seconds, unless it slows below crouch speed first.
        /// </summary>
        public FP SlideDuration = FP._0_75;
        /// <summary>
        /// How hard a slide brakes, as force per m/s of horizontal speed; low so the slide carries.
        /// </summary>
        public FP SlideDeceleration = (FP)8 / 10;

        /// <summary>
        /// How hard a staggered character brakes, as force per m/s; low so the hit's momentum carries the body.
        /// </summary>
        [Header("Stagger")]
        public FP StaggerDeceleration = 1;

        /// <summary>
        /// The tallest ledge, in meters, the step-up lifts the character onto, and the height of the knee probe.
        /// </summary>
        [Header("Step Up")]
        public FP MaxStepHeight = FP._0_50;
        /// <summary>
        /// How far ahead, in meters, the step-up probes look for a ledge.
        /// </summary>
        public FP StepCheckForward = FP._0_50;
        /// <summary>
        /// The height, in meters, of the foot probe above the character's origin.
        /// </summary>
        public FP StepFootOffset = FP._0_10;
        /// <summary>
        /// The upward speed given per meter of step height, so taller steps lift faster.
        /// </summary>
        public FP StepUpVelocityFactor = 6;
        /// <summary>
        /// If true, draws the step-up probes as debug rays.
        /// </summary>
        public bool DebugStepUp;
    }
}
