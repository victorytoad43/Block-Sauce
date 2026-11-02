using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// The local player's third-person orbit camera. Mouse or right stick turns it around the followed character.
    /// Its absolute yaw and pitch are sent as input every tick, so movement, aim and throws follow the camera.
    /// While the character holds a block, the same look input swings the block instead: the camera stops turning,
    /// eases round behind the character, and collects the look movement as swing degrees for the input poller.
    /// Sideways, a swing far enough round turns the body (in the simulation) and the camera follows it. Vertically,
    /// once the block reaches the top or bottom of its swing, further up or down input tilts the camera instead.
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

        private Transform _target;
        private bool _isHolding;
        private float _followYaw;
        private Vector2 _pendingSwing;
        private bool _canTiltWithSwing;
        private float _swingPitch;
        private float _swingPitchMin;
        private float _swingPitchMax;

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
                _canTiltWithSwing = false;
            }

            _isHolding = isHolding;
            _followYaw = bodyYaw;
        }

        /// <summary>
        /// Gives the held block's swing pitch and its limits, in degrees, so vertical input past a limit can tilt the
        /// camera; <paramref name="canTilt"/> is false while that input means something else (aiming a throw).
        /// </summary>
        public void SetSwingPitch(bool canTilt, float swingPitch, float swingPitchMin, float swingPitchMax)
        {
            _canTiltWithSwing = canTilt;
            _swingPitch = swingPitch;
            _swingPitchMin = swingPitchMin;
            _swingPitchMax = swingPitchMax;
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
                Vector2 swing = turn * _swingSensitivity;
                if (_canTiltWithSwing)
                {
                    // Where the block's pitch lands once the swing not yet sent arrives; anything past its limit
                    // would be clamped away by the simulation, so it tilts the camera instead.
                    float landed = Mathf.Clamp(_swingPitch + _pendingSwing.y, _swingPitchMin, _swingPitchMax);
                    float target = landed + swing.y;
                    float overflow = target - Mathf.Clamp(target, _swingPitchMin, _swingPitchMax);
                    swing.y -= overflow;
                    Pitch = Mathf.Clamp(Pitch - overflow / _swingSensitivity, _pitchMin, _pitchMax);
                }

                _pendingSwing += swing;
                float t = 1f - Mathf.Exp(-_followSharpness * Time.deltaTime);
                Yaw = Mathf.Repeat(Mathf.LerpAngle(Yaw, _followYaw, t) + 180f, 360f) - 180f;
                return;
            }

            Yaw = Mathf.Repeat(Yaw + turn.x + 180f, 360f) - 180f;
            Pitch = Mathf.Clamp(Pitch - turn.y, _pitchMin, _pitchMax);
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 pivot = _target.position + Vector3.up * _pivotHeight;
            transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * _distance, rotation);
        }

        /// <summary>
        /// Locks the cursor on click and frees it on Escape, so the mouse can still reach the connection menu.
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
                     Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
