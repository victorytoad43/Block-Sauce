using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// The local player's third-person orbit camera. Mouse or right stick turns it around the followed character.
    /// Its absolute yaw and pitch are sent as input every tick, so movement, aim and throws follow the camera.
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

        private Transform _target;

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
