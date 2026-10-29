using Photon.Deterministic;
using Quantum;
using UnityEngine;
using QInput = Quantum.Input;

namespace BlockSauce
{
    /// <summary>
    /// Development builds only, for multi-client tests launched from a script:
    /// "-autojoin ROOM" connects straight to that online room, skipping the menu, and "-autoplay" drives the local
    /// player with simple random input (wander, run, grab, swing, throw, jumpshot) through the input poller's test hook.
    /// The autoplay only produces input, so every client still simulates the same deterministic game.
    /// Does nothing in the editor or in release builds.
    /// </summary>
    public class DevLauncher : MonoBehaviour
    {
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        private const float PressSeconds = 0.1f;

        private QInput _autoInput;
        private float _nextDecision;
        private float _throwAt = -1f;
        private float _grabUntil, _throwUntil, _jumpUntil;

        private async void Start()
        {
            string room = GetArgument("-autojoin");
            if (room == null)
                return;

            if (HasArgument("-autoplay"))
            {
                PlayerInputPoller.TestInputOverride = input =>
                {
                    input.Move = _autoInput.Move;
                    input.Swing = _autoInput.Swing;
                    input.Jump = _autoInput.Jump;
                    input.Run = _autoInput.Run;
                    input.Grab = _autoInput.Grab;
                    input.Throw = _autoInput.Throw;
                    input.Crouch = false;
                    return input;
                };
            }

            var ui = FindAnyObjectByType<QuantumStartUI>();
            if (ui != null)
            {
                ui.SetPanelVisibility(false);
            }

            var connection = FindAnyObjectByType<QuantumStartUIConnectionBase>();
            await connection.ConnectAsync(new QuantumStartUIConnectionBase.StartParameter
            {
                IsOnline = true,
                IsVisible = true,
                PlayerName = "Dev" + Random.Range(1000, 9999),
                RoomName = room,
                Region = GetArgument("-region") ?? string.Empty,
            });
        }

        /// <summary>
        /// Picks new random input every 0.4–1.5 s: a move direction (sometimes running), and now and then a grab press,
        /// a swing, a jump, or a throw (a jump followed by a throw makes a jumpshot).
        /// </summary>
        private void Update()
        {
            if (PlayerInputPoller.TestInputOverride == null)
                return;

            float now = Time.time;
            if (_throwAt > 0f && now >= _throwAt)
            {
                _throwUntil = now + PressSeconds;
                _throwAt = -1f;
            }

            // Presses are held briefly so the 64 Hz input poll never misses one between frames.
            _autoInput.Grab = now < _grabUntil;
            _autoInput.Throw = now < _throwUntil;
            _autoInput.Jump = now < _jumpUntil;

            if (now < _nextDecision)
                return;

            _nextDecision = now + Random.Range(0.4f, 1.5f);
            Vector2 move = Random.insideUnitCircle.normalized;
            _autoInput.Move = new FPVector2(move.x.ToFP(), move.y.ToFP());
            _autoInput.Run = Random.value < 0.5f;
            _autoInput.Swing = new FPVector2(Random.Range(-4f, 4f).ToFP(), Random.Range(-1f, 1f).ToFP());

            float roll = Random.value;
            if (roll < 0.35f)
            {
                _grabUntil = now + PressSeconds;
            }
            else if (roll < 0.5f)
            {
                _throwUntil = now + PressSeconds;
            }
            else if (roll < 0.6f)
            {
                _jumpUntil = now + PressSeconds;
                _throwAt = now + 0.3f;
            }
        }

        private static string GetArgument(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }

            return null;
        }

        private static bool HasArgument(string name)
        {
            return System.Array.IndexOf(System.Environment.GetCommandLineArgs(), name) >= 0;
        }
#endif
    }
}
