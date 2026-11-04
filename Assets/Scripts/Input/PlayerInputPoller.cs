using Photon.Deterministic;
using Quantum;
using UnityEngine;
using QInput = Quantum.Input;

namespace BlockSauce
{
    /// <summary>
    /// Sends the local player's input to Quantum each tick: move, the orbit camera's absolute angles, and buttons.
    /// Absolute camera angles (as in Friendslop) mean a lost or repeated input never makes the aim drift.
    /// Speed: on a gamepad the stick's tilt sets it, from a slow walk up to a full run (Run is always on, and the
    /// simulation scales the run speed by the stick's magnitude); on a keyboard, keys walk and Shift runs.
    /// While a menu has the screen (<see cref="GameFlow.AllowsGameInput"/>) only the camera's angles go out: the
    /// character stands still and presses nothing.
    /// </summary>
    public class PlayerInputPoller : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Test hook for the editor and development builds: when set, rewrites each tick's input before it is sent.
        /// Automated play tests and the dev autoplay drive the game through this; it does not exist in release builds.
        /// </summary>
        public static System.Func<QInput, QInput> TestInputOverride;
#endif

        private void OnEnable()
        {
            QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
        }

        /// <summary>
        /// Builds this tick's input and submits it. While holding a block, the look movement the orbit camera collected
        /// since the last tick goes out as Swing, in degrees.
        /// </summary>
        private void PollInput(CallbackPollInput callback)
        {
            QInput input = default;
            BlockSauceControls controls = BlockSauceControls.Instance;

            OrbitCamera orbit = OrbitCamera.Instance;
            if (orbit != null)
            {
                input.HasCamera = true;
                input.Camera.Pitch = orbit.Pitch.ToFP();
                input.Camera.Yaw = orbit.Yaw.ToFP();

                Vector2 swing = orbit.ConsumeSwing();
                input.Swing = new FPVector2(swing.x.ToFP(), swing.y.ToFP());
            }

            if (GameFlow.AllowsGameInput)
            {
                Vector2 move = controls.Move.ReadValue<Vector2>();
                input.Move = new FPVector2(move.x.ToFP(), move.y.ToFP());
                input.Jump = controls.Jump.IsPressed();
                input.Crouch = controls.Crouch.IsPressed();
                bool analogMove = controls.Move.activeControl?.device is UnityEngine.InputSystem.Gamepad;
                input.Run = analogMove || controls.Run.IsPressed();
                input.Grab = controls.Grab.IsPressed();
                input.Throw = controls.Throw.IsPressed();
            }
            else
            {
                input.Swing = default;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (TestInputOverride != null)
            {
                input = TestInputOverride(input);
            }
#endif

            callback.SetInput(input, DeterministicInputFlags.Repeatable);
        }
    }
}
