using Photon.Deterministic;

namespace Quantum
{
    /// <summary>
    /// Copies each player's absolute orbit-camera angles onto their character, ahead of movement in the same tick.
    /// Movement, aim and throw directions are all relative to this camera yaw, not to the way the character faces.
    /// </summary>
    public unsafe class PlayerLookSystem : SystemMainThreadFilter<PlayerLookSystem.Filter>
    {
        /// <summary>
        /// The pitch limit looking straight up, in degrees.
        /// </summary>
        private readonly FP PitchMin = -90;
        /// <summary>
        /// The pitch limit looking straight down, in degrees.
        /// </summary>
        private readonly FP PitchMax = 90;
        /// <summary>
        /// The largest yaw after wrapping, half a turn in degrees.
        /// </summary>
        private readonly FP YawHalfTurn = 180;
        /// <summary>
        /// A full turn, in degrees, added or removed while wrapping yaw.
        /// </summary>
        private readonly FP YawFullTurn = 360;

        /// <summary>
        /// The components the player look system updates for each player character.
        /// </summary>
        public struct Filter
        {
            public EntityRef Entity;
            public PlayerLook* PlayerLook;
            public Player* Player;
        }

        /// <summary>
        /// Stores this tick's absolute camera angles from input, in degrees, clamping pitch and wrapping yaw.
        /// </summary>
        public override void Update(Frame frame, ref Filter filter)
        {
            InputPitchYaw camera = filter.Player->Input.Camera;

            filter.PlayerLook->Pitch = FPMath.Clamp(camera.Pitch, PitchMin, PitchMax);
            filter.PlayerLook->Yaw = WrapAngle(camera.Yaw);
        }

        /// <summary>
        /// Wraps an angle in degrees into the range -180 to 180.
        /// </summary>
        private FP WrapAngle(FP angle)
        {
            while (angle > YawHalfTurn)
            {
                angle -= YawFullTurn;
            }

            while (angle < -YawHalfTurn)
            {
                angle += YawFullTurn;
            }

            return angle;
        }
    }
}
