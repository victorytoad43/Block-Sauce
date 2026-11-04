using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// The local player's third-person orbit camera. Mouse or right stick turns it around the followed character.
    /// Its absolute yaw and pitch are sent as input every tick, so movement, aim and throws follow the camera.
    /// While the character holds a block, the same look input swings the block, collected as swing degrees for the
    /// input poller, and the camera still moves with it on a gradient: slowly while the block is near the middle of its
    /// swing, speeding up toward the edges, and at full speed once the swing turns the body (sideways, in the
    /// simulation) or reaches the top or bottom of its range (vertically, where further input only tilts the camera).
    /// Aiming a throw works the same way on the throw angle. While aiming it moves over the character's shoulder,
    /// clear of the block raised overhead.
    /// This is view-only state: the simulation never reads it except through that input.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        /// <summary>
        /// The orbit camera in the scene, or null while none is enabled.
        /// </summary>
        public static OrbitCamera Instance { get; private set; }

        [Tooltip("Distance from the pivot to the camera, in meters.")]
        [SerializeField] private float _distance = 4.5f;
        [Tooltip("Height of the orbit pivot above the followed character's origin (its feet), in meters.")]
        [SerializeField] private float _pivotHeight = 1.4f;
        [Tooltip("Degrees of turn per pixel of mouse movement.")]
        [SerializeField] private float _mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full right-stick deflection.")]
        [SerializeField] private float _stickSpeed = 180f;
        [Tooltip("Lowest pitch, in degrees; negative looks up.")]
        [SerializeField] private float _pitchMin = -30f;
        [Tooltip("Highest pitch, in degrees; positive looks down.")]
        [SerializeField] private float _pitchMax = 70f;
        [Tooltip("Swing degrees per degree of look input while holding a block.")]
        [SerializeField] private float _swingSensitivity = 1f;
        [Tooltip("How fast the camera eases round behind the character while holding, per second.")]
        [SerializeField] private float _followSharpness = 6f;
        [Tooltip("While holding, the share of look input that still moves the camera with the block in the middle of "
            + "its swing; it rises smoothly to all of it at the swing's edges.")]
        [Range(0f, 1f)]
        [SerializeField] private float _swingCameraShare = 0.35f;
        [Tooltip("Where the camera moves while aiming a throw, in meters: right of and above its usual place, so the "
            + "block held overhead doesn't hide the aim.")]
        [SerializeField] private Vector2 _aimShoulderOffset = new Vector2(0.9f, 0.3f);
        [Tooltip("How fast the camera moves to and from the shoulder, per second.")]
        [SerializeField] private float _shoulderSharpness = 10f;

        private Transform _target;
        private bool _isHolding;
        private float _followYaw;
        private Vector2 _pendingSwing;
        private float _swingYaw;
        private float _swingTurnThreshold = 45f;
        private float _swingPitch;
        private float _swingPitchMin = -25f;
        private float _swingPitchMax = 60f;
        private float _swingPitchPerInput = 1f;
        private bool _isAiming;
        private float _shoulderWeight;

        /// <summary>
        /// The camera's absolute yaw, in degrees around world up.
        /// </summary>
        public float Yaw { get; private set; }
        /// <summary>
        /// The camera's absolute pitch, in degrees; positive looks down.
        /// </summary>
        public float Pitch { get; private set; } = 15f;

        /// <summary>
        /// Follows <paramref name="target"/>, starting behind it at <paramref name="yaw"/>; null stops following.
        /// </summary>
        public void SetTarget(Transform target, float yaw)
        {
            _target = target;
            Yaw = yaw;
        }

        /// <summary>
        /// Switches look input between turning the camera and swinging a held block, and gives the character's facing
        /// for the camera to follow while holding.
        /// </summary>
        public void SetHolding(bool isHolding, float bodyYaw)
        {
            if (isHolding == false)
            {
                _pendingSwing = Vector2.zero;
                _swingYaw = 0f;
                _isAiming = false;
            }

            _isHolding = isHolding;
            _followYaw = bodyYaw;
        }

        /// <summary>
        /// Gives the held block's swing, in degrees, for the camera to move with: its yaw from the body and the yaw at
        /// which the body starts turning, and the vertical angle that input moves (the swing pitch, or the throw angle
        /// while aiming), its limits, and how many degrees of it one degree of input makes.
        /// </summary>
        public void SetSwing(bool isAiming, float swingYaw, float turnThreshold, float pitch, float pitchMin,
            float pitchMax, float pitchPerInput)
        {
            _isAiming = isAiming;
            _swingYaw = swingYaw;
            _swingTurnThreshold = Mathf.Max(turnThreshold, 1f);
            _swingPitch = pitch;
            _swingPitchMin = pitchMin;
            _swingPitchMax = pitchMax;
            _swingPitchPerInput = Mathf.Max(pitchPerInput, 0.01f);
        }

        /// <summary>
        /// Returns the swing collected since the last call, in degrees (x: around the character, y: up), and clears it.
        /// </summary>
        public Vector2 ConsumeSwing()
        {
            Vector2 swing = _pendingSwing;
            _pendingSwing = Vector2.zero;
            return swing;
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            UpdateCursorLock();

            BlockSauceControls controls = BlockSauceControls.Instance;
            Vector2 turn = controls.LookStick.ReadValue<Vector2>() * (_stickSpeed * Time.deltaTime);
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                turn += controls.LookMouse.ReadValue<Vector2>() * _mouseSensitivity;
            }

            if (_isHolding)
            {
                UpdateHoldingCamera(turn * _swingSensitivity);
                return;
            }

            Yaw = Mathf.Repeat(Yaw + turn.x + 180f, 360f) - 180f;
            Pitch = Mathf.Clamp(Pitch - turn.y, _pitchMin, _pitchMax);
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            _shoulderWeight = Mathf.Lerp(_shoulderWeight, _isAiming ? 1f : 0f,
                1f - Mathf.Exp(-_shoulderSharpness * Time.deltaTime));
            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 shoulder = Quaternion.Euler(0f, Yaw, 0f) * new Vector3(_aimShoulderOffset.x, _aimShoulderOffset.y, 0f);
            Vector3 pivot = _target.position + Vector3.up * _pivotHeight + shoulder * _shoulderWeight;
            transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * _distance, rotation);
        }

        /// <summary>
        /// Collects <paramref name="swing"/> (look input, in degrees) for the simulation and moves the camera with the
        /// block. Sideways the camera sits behind the body, turned toward the swing by <see cref="GetSwingCameraYaw"/>.
        /// Vertically it tilts by a share of the input that grows toward the ends of the range, and by all of any input
        /// past an end, which the simulation would clamp away; that input is kept back from the swing.
        /// </summary>
        private void UpdateHoldingCamera(Vector2 swing)
        {
            // Where the vertical angle lands once the swing not yet sent arrives.
            float landed = Mathf.Clamp(_swingPitch + _pendingSwing.y * _swingPitchPerInput, _swingPitchMin, _swingPitchMax);
            float target = landed + swing.y * _swingPitchPerInput;
            float overflow = (target - Mathf.Clamp(target, _swingPitchMin, _swingPitchMax)) / _swingPitchPerInput;
            float used = swing.y - overflow;
            float middle = (_swingPitchMin + _swingPitchMax) * 0.5f;
            float halfRange = Mathf.Max((_swingPitchMax - _swingPitchMin) * 0.5f, 1f);
            float fromMiddle = Mathf.Clamp((landed - middle) / halfRange, -1f, 1f);
            float tilt = used * Mathf.Lerp(_swingCameraShare, 1f, fromMiddle * fromMiddle) + overflow;
            Pitch = Mathf.Clamp(Pitch - tilt / _swingSensitivity, _pitchMin, _pitchMax);
            swing.y = used;

            _pendingSwing += swing;
            float swingYaw = Mathf.Clamp(_swingYaw + _pendingSwing.x, -180f, 180f);
            float t = 1f - Mathf.Exp(-_followSharpness * Time.deltaTime);
            Yaw = Mathf.Repeat(Mathf.LerpAngle(Yaw, _followYaw + GetSwingCameraYaw(swingYaw), t) + 180f, 360f) - 180f;
        }

        /// <summary>
        /// How far the camera turns from behind the body toward a swing of <paramref name="swingYaw"/> degrees. Its rate
        /// rises smoothly from the middle share at the center to all of the swing at the body-turn threshold, where the
        /// body starts turning and the camera turns with it; past the threshold it keeps that rate.
        /// </summary>
        private float GetSwingCameraYaw(float swingYaw)
        {
            float threshold = _swingTurnThreshold;
            float share = _swingCameraShare;
            float inside = Mathf.Min(Mathf.Abs(swingYaw), threshold);
            float yaw = share * inside + (1f - share) * inside * inside * inside / (3f * threshold * threshold);
            return Mathf.Sign(swingYaw) * (yaw + Mathf.Max(Mathf.Abs(swingYaw) - threshold, 0f));
        }

        private bool IsSpectating()
        {
            return TryGetComponent(out SpectatorDirector director) && director.IsDirecting;
        }

        /// <summary>
        /// Locks the cursor on click and frees it on Escape, so the mouse can still reach the connection menu. While the
        /// spectator director has the camera, the cursor stays free for its controls.
        /// </summary>
        private void UpdateCursorLock()
        {
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (_target != null && mouse != null && mouse.leftButton.wasPressedThisFrame &&
                     Cursor.lockState != CursorLockMode.Locked && IsSpectating() == false)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
