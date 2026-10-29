using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    /// <summary>
    /// A character's grab, hold-and-swing, place and throw tuning.
    /// The held block orbits a pivot inside the character at the swing angle, which the right stick drives.
    /// </summary>
    public class GrabConfig : AssetObject
    {
        /// <summary>
        /// The height of the hold pivot above the character's feet, in meters (Guy's `prop` bone height at game scale).
        /// </summary>
        [Header("Hold Point")]
        public FP PivotHeight = (FP)84 / 100;
        /// <summary>
        /// How far from the pivot the held block's center sits, in meters (Guy's `prop` bone reach at game scale).
        /// </summary>
        public FP HoldRadius = (FP)90 / 100;

        /// <summary>
        /// How far from the pivot, in meters, a block can be to be grabbed.
        /// </summary>
        [Header("Grab")]
        public FP GrabRange = 2;
        /// <summary>
        /// The half-angle, in degrees, of the cone in front of the character a block must be in to be grabbed.
        /// </summary>
        public FP GrabConeHalfAngle = 60;

        /// <summary>
        /// How far the swing can take the block around the character, either side of its facing, in degrees.
        /// </summary>
        [Header("Swing")]
        public FP SwingYawLimit = 150;
        /// <summary>
        /// The lowest swing pitch, in degrees; negative swings the block down.
        /// </summary>
        public FP SwingPitchMin = -25;
        /// <summary>
        /// The highest swing pitch, in degrees.
        /// </summary>
        public FP SwingPitchMax = 60;
        /// <summary>
        /// The swing yaw, in degrees either side of facing, beyond which the body turns to follow the block.
        /// </summary>
        public FP BodyTurnThreshold = 45;
        /// <summary>
        /// How fast the body turns to follow a swing past the threshold, in degrees per second.
        /// </summary>
        public FP BodyTurnSpeed = 360;

        /// <summary>
        /// The gain turning the held block's distance from the hold point into correction speed.
        /// </summary>
        [Header("Hold Response")]
        public FP HoldFollowGain = 20;
        /// <summary>
        /// The top speed of the correction toward the hold point, in m/s, on top of the carrier's own velocity.
        /// Higher lets a fast swing hit harder.
        /// </summary>
        public FP HoldMaxFollowSpeed = 16;
        /// <summary>
        /// How fast the held block can swing around the pivot toward the hold point, in degrees per second.
        /// </summary>
        public FP HoldSwingSpeed = 720;
        /// <summary>
        /// How fast the held block turns toward its hold rotation, in degrees per second.
        /// </summary>
        public FP HoldTurnSpeed = 720;
        /// <summary>
        /// The extra leash length beyond the hold radius, in meters.
        /// </summary>
        public FP LeashSlack = FP._0_50;
        /// <summary>
        /// The closest the held block may come to the pivot, in meters.
        /// </summary>
        public FP LeashMinDistance = (FP)70 / 100;
        /// <summary>
        /// The distance from the pivot, in meters, at which an engaged leash breaks the hold.
        /// </summary>
        public FP GrabBreakDistance = (FP)25 / 10;
        /// <summary>
        /// The share of the hold's push that comes back through the carrier: zero is weightless, one the full reaction.
        /// </summary>
        public FP CarryReactionScale = FP._0_20;

        /// <summary>
        /// The throw speed, in m/s, added to the carrier's velocity.
        /// </summary>
        [Header("Throw")]
        public FP ThrowSpeed = 12;
        /// <summary>
        /// The throw's upward angle, in degrees, above the character's facing.
        /// </summary>
        public FP ThrowPitch = 15;
        /// <summary>
        /// The jumpshot's speed as a multiple of the throw speed.
        /// </summary>
        public FP JumpshotSpeedScale = (FP)13 / 10;
        /// <summary>
        /// The jumpshot's angle, in degrees above the character's facing; negative throws downward.
        /// </summary>
        public FP JumpshotPitch = -10;
    }
}
