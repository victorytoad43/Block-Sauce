using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockSauce
{
    /// <summary>
    /// Placeholder match HUD (A-006), drawn with IMGUI: the match state and countdown, each player's tower (3 slots:
    /// empty, locking with progress, locked), the winner, and the start prompt in the lobby. Enter or the gamepad's
    /// Start button sends the start command. View only: it reads the predicted frame and sends commands, the only way
    /// the view talks to the simulation besides input.
    /// </summary>
    public class MatchHud : MonoBehaviour
    {
        [Tooltip("The player colors.")]
        [SerializeField] private PlayerPalette _palette;

        private GUIStyle _big;
        private GUIStyle _small;
        private Texture2D _white;

        private void Update()
        {
            QuantumGame game = QuantumRunner.Default?.Game;
            Frame frame = game?.Frames.Predicted;
            if (frame == null || frame.TryGetSingleton(out Match match) == false)
                return;

            bool startPressed = (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (match.State == MatchState.Lobby && startPressed)
            {
                game.AddCommand(new CommandStartMatch());
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev shortcut: drop your next block onto your tower.
            if (match.State == MatchState.Playing && Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                game.AddCommand(new CommandDevStackTower());
            }
#endif
        }

        private void OnGUI()
        {
            Frame frame = QuantumRunner.Default?.Game?.Frames.Predicted;
            if (frame == null || frame.TryGetSingleton(out Match match) == false)
                return;

            EnsureStyles();
            GameModeConfig mode = frame.FindAsset(match.Mode);

            string headline = match.State switch
            {
                MatchState.Lobby => "Lobby  ·  press Enter / Start to begin a match",
                MatchState.Countdown => $"Get ready… {Mathf.CeilToInt(match.Timer.RemainingSeconds(frame).AsFloat)}",
                MatchState.Playing => "Stack & Lock: lock 3 blocks into one tower",
                MatchState.RoundEnd => $"Player {(int)match.Winner + 1} wins!",
                MatchState.Results => $"Results  ·  Player {(int)match.Winner + 1} won  ·  back to lobby in {Mathf.CeilToInt(match.Timer.RemainingSeconds(frame).AsFloat)}",
                _ => "",
            };
            GUI.Label(new Rect(0, 16, Screen.width, 40), headline, _big);

            if (match.State == MatchState.Lobby || mode == null)
                return;

            float y = 70;
            var players = frame.Filter<Player>();
            while (players.Next(out EntityRef _, out Player player))
            {
                DrawTowerRow(frame, mode, player.Ref, y, player.Ref == match.Winner && match.State != MatchState.Playing);
                y += 28;
            }
        }

        private void DrawTowerRow(Frame frame, GameModeConfig mode, PlayerRef player, float y, bool isWinner)
        {
            Color color = _palette != null ? _palette.GetColor((int)player) : Color.white;
            DrawRect(new Rect(16, y, 20, 20), color);
            GUI.Label(new Rect(42, y, 90, 22), $"Player {(int)player + 1}{(isWinner ? "  ★" : "")}", _small);

            for (int level = 0; level < mode.TowerHeight; level++)
            {
                var slot = new Rect(140 + level * 30, y, 24, 20);
                DrawRect(slot, new Color(0, 0, 0, 0.4f));
                GetSlot(frame, player, level, out bool locked, out float progress);
                if (locked)
                {
                    DrawRect(slot, color);
                }
                else if (progress > 0f)
                {
                    DrawRect(new Rect(slot.x, slot.y + slot.height * (1f - progress), slot.width, slot.height * progress),
                        Color.Lerp(color, Color.black, 0.5f));
                }
            }
        }

        /// <summary>
        /// Reads one tower slot: locked, or locking with its progress, or empty.
        /// </summary>
        private static void GetSlot(Frame frame, PlayerRef player, int level, out bool locked, out float progress)
        {
            locked = false;
            progress = 0f;
            var blocks = frame.Filter<Block>();
            while (blocks.Next(out EntityRef _, out Block block))
            {
                if (block.Owner != player || block.TowerLevel != level)
                    continue;

                if (block.Lock == LockState.Locked)
                {
                    locked = true;
                    return;
                }

                if (block.Lock == LockState.Locking)
                {
                    progress = block.LockProgress.AsFloat;
                }
            }
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _white);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (_big != null)
                return;

            _white = Texture2D.whiteTexture;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        }
    }
}
