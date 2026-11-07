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
        /// The height of the hold pivot above the character's feet, in meters: the height of Guy's `prop` bone at the
        /// end of the GrabBlock clip, where his hands hold the block.
        /// </summary>
        [Header("Hold Point")]
        public FP PivotHeight = (FP)77 / 100;
        /// <summary>
        /// How far from the pivot the held block's center sits, in meters: the reach of Guy's `prop` bone at the end
        /// of the GrabBlock clip.
        /// </summary>
        public FP HoldRadius = (FP)95 / 100;

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
        /// Grab takes the block in reach the camera looks at (Jackson, 2026-11-06). Off: the nearest block in the cone.
        /// </summary>
        public bool GrabWhereAimed = true;
        /// <summary>
        /// The height above the character's feet that the camera looks through (the orbit camera's pivot height, 1.4 m),
        /// which with the look's pitch and yaw gives the aim line.
        /// </summary>
        public FP AimPivotHeight = (FP)14 / 10;
        /// <summary>
        /// How far, in meters, a block's center may lie off the aim line and still be the one looked at.
        /// </summary>
        public FP AimMaxMiss = (FP)6 / 10;
        /// <summary>
        /// When no block in reach is near the aim line, grab the nearest block in the cone instead.
        /// </summary>
        public bool AimFallbackToNearest = true;

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
        /// Placing a block over another squares it to that block's faces and lifts it out of the block if it was
        /// pressed in, so it settles flush instead of being shoved off. Off: it is let go exactly where it is held.
        /// </summary>
        [Header("Place")]
        public bool SettlePlacedOnBlockBelow = true;
        /// <summary>
        /// Held over a block, the held block's hand point stops at the height it would rest on that block, so it sits
        /// on a tower instead of being driven into it.
        /// </summary>
        public bool HoldRestsOnBlockBelow = true;

        /// <summary>
        /// The throw speed, in m/s, added to the carrier's velocity.
        /// </summary>
        [Header("Throw")]
        public FP ThrowSpeed = 12;
        /// <summary>
        /// The aim angle a throw starts at when aiming begins, in degrees above the character's facing.
        /// </summary>
        public FP AimPitchStart = 15;
        /// <summary>
        /// The lowest aim angle, in degrees; negative aims downward.
        /// </summary>
        public FP AimPitchMin = -20;
        /// <summary>
        /// The highest aim angle, in degrees.
        /// </summary>
        public FP AimPitchMax = 60;
        /// <summary>
        /// Degrees of aim pitch per degree of vertical swing input while aiming.
        /// </summary>
        public FP AimPitchSensitivity = FP._0_50;
        /// <summary>
        /// The swing pitch, in degrees, the held block is raised to while aiming, ready to throw.
        /// </summary>
        public FP AimReadyPitch = 30;
        /// <summary>
        /// The jumpshot's speed as a multiple of the throw speed; a jumpshot aims like a throw.
        /// </summary>
        public FP JumpshotSpeedScale = (FP)13 / 10;
        /// <summary>
        /// Seconds after a throw during which the block and its thrower pass through each other: the aim point sits
        /// almost against the thrower, so the block would otherwise hit him on the way out.
        /// </summary>
        public FP ThrowClearSeconds = (FP)15 / 100;
    }
}
