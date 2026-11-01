using UnityEngine.InputSystem;

namespace BlockSauce
{
    /// <summary>
    /// The gameplay input actions for keyboard and mouse and for gamepad, built in code so the bindings live in one place.
    /// One shared, enabled instance serves both the input poller and the orbit camera.
    /// Default bindings are placeholders until Phase 1 tunes the controls.
    /// </summary>
    public sealed class BlockSauceControls
    {
        /// <summary>
        /// The shared controls, created and enabled on first use.
        /// </summary>
        public static BlockSauceControls Instance => _instance ??= new BlockSauceControls();
        private static BlockSauceControls _instance;

        /// <summary>
        /// Left stick or WASD: walk and strafe, relative to the camera.
        /// </summary>
        public readonly InputAction Move;
        /// <summary>
        /// Mouse movement, in pixels this frame: orbits the camera, or swings a held block (Phase 1).
        /// </summary>
        public readonly InputAction LookMouse;
        /// <summary>
        /// Right stick, as a rate: orbits the camera, or swings a held block (Phase 1).
        /// </summary>
        public readonly InputAction LookStick;
        public readonly InputAction Jump;
        public readonly InputAction Crouch;
        public readonly InputAction Run;
        public readonly InputAction Grab;
        public readonly InputAction Throw;

        private BlockSauceControls()
        {
            var map = new InputActionMap("Gameplay");

            Move = map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            LookMouse = map.AddAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            LookStick = map.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            Jump = AddButton(map, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Crouch = AddButton(map, "Crouch", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
            // Keyboard only: on a gamepad the stick's tilt sets the speed (see PlayerInputPoller).
            Run = map.AddAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
            Grab = AddButton(map, "Grab", "<Mouse>/leftButton", "<Gamepad>/rightShoulder");
            Throw = AddButton(map, "Throw", "<Mouse>/rightButton", "<Gamepad>/rightTrigger");

            map.Enable();
        }

        private static InputAction AddButton(InputActionMap map, string name, string keyboardMouse, string gamepad)
        {
            InputAction action = map.AddAction(name, InputActionType.Button, keyboardMouse);
            action.AddBinding(gamepad);
            return action;
        }
    }
}
