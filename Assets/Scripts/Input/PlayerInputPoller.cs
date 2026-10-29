using Photon.Deterministic;
using Quantum;
using UnityEngine;
using QInput = Quantum.Input;

namespace BlockSauce
{
    /// <summary>
    /// Sends the local player's input to Quantum each tick: move, the orbit camera's absolute angles, and buttons.
    /// Absolute camera angles (as in Friendslop) mean a lost or repeated input never makes the aim drift.
    /// </summary>
    public class PlayerInputPoller : MonoBehaviour
    {
#if UNITY_EDITOR
        /// <summary>
        /// Editor-only test hook: when set, rewrites each tick's input before it is sent. Automated play tests drive
        /// the game through this, because an unfocused editor drops or resets synthetic device input.
        /// </summary>
        public static System.Func<QInput, QInput> EditorInputOverride;
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

            Vector2 move = controls.Move.ReadValue<Vector2>();
            input.Move = new FPVector2(move.x.ToFP(), move.y.ToFP());
            input.Jump = controls.Jump.IsPressed();
            input.Crouch = controls.Crouch.IsPressed();
            input.Run = controls.Run.IsPressed();
            input.Grab = controls.Grab.IsPressed();
            input.Throw = controls.Throw.IsPressed();

#if UNITY_EDITOR
            if (EditorInputOverride != null)
            {
                input = EditorInputOverride(input);
            }
#endif

            callback.SetInput(input, DeterministicInputFlags.Repeatable);
        }
    }
}
