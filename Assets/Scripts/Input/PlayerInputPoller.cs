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
        private void OnEnable()
        {
            QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
        }

        /// <summary>
        /// Builds this tick's input and submits it. Swing stays zero until holding blocks exists (Phase 1),
        /// when the right stick switches from turning the camera to swinging the held block.
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
            }

            Vector2 move = controls.Move.ReadValue<Vector2>();
            input.Move = new FPVector2(move.x.ToFP(), move.y.ToFP());
            input.Jump = controls.Jump.IsPressed();
            input.Crouch = controls.Crouch.IsPressed();
            input.Run = controls.Run.IsPressed();
            input.Grab = controls.Grab.IsPressed();
            input.Throw = controls.Throw.IsPressed();

            callback.SetInput(input, DeterministicInputFlags.Repeatable);
        }
    }
}
