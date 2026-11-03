using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockSauce
{
    /// <summary>
    /// Placeholder match HUD (A-006), drawn with IMGUI: the match state and countdown, each player's best stack (3 slots:
    /// empty, settling with progress, counted), the winner, and the start prompt in the lobby. Enter or the gamepad's
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

            // Dev shortcut: coat your held block, or all your blocks, with Stack sauce (F8) or Slip sauce (Shift+F8).
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                bool slip = Keyboard.current.shiftKey.isPressed;
                game.AddCommand(new CommandDevSauce { Type = slip ? SauceType.Slip : SauceType.Stack });
            }

            // Dev shortcut until the room screen: add a bot in the lobby (F7), or remove one (Shift+F7).
            if (match.State == MatchState.Lobby && Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame)
            {
                game.AddCommand(Keyboard.current.shiftKey.isPressed ? new CommandRemoveBot() : new CommandAddBot());
            }

            // Dev shortcut: an all-bot match. In the lobby, F6 hands your character to a bot and starts the match (bots
            // fill the other slots); you watch your bot. F6 again, at any time, takes your character back.
            if (Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame)
            {
                bool startMatch = match.State == MatchState.Lobby && IsLocalCharacterBot(frame, game) == false;
                game.AddCommand(new CommandDevBotSelf());
                if (startMatch)
                {
                    game.AddCommand(new CommandStartMatch());
                }
            }

            // Dev shortcut: knock yourself down.
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                game.AddCommand(new CommandDevKnockdown());
            }
#endif
        }

        private static bool IsLocalCharacterBot(Frame frame, QuantumGame game)
        {
            foreach (PlayerRef player in game.GetLocalPlayers())
            {
                if (PlayerSystem.TryGetCharacter(frame, player, out EntityRef character) && frame.Has<Bot>(character))
                    return true;
            }

            return false;
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
                MatchState.Playing => "Stack & Lock: stack all 3 of your blocks into one tower",
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

            EntityRef towerBase = BlockTower.GetBestBase(frame, player);
            for (int level = 0; level < mode.TowerHeight; level++)
            {
                var slot = new Rect(140 + level * 30, y, 24, 20);
                DrawRect(slot, new Color(0, 0, 0, 0.4f));
                GetSlot(frame, towerBase, level, out bool counted, out float progress);
                if (counted)
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
        /// Reads one slot of the player's best stack: counted, or settling with its progress, or empty.
        /// </summary>
        private static void GetSlot(Frame frame, EntityRef towerBase, int level, out bool counted, out float progress)
        {
            counted = false;
            progress = 0f;
            if (BlockTower.TryGetAtLevel(frame, towerBase, level, out Block block) == false)
                return;

            counted = block.Stack == StackState.Counted;
            progress = block.Stack == StackState.Settling ? block.StackProgress.AsFloat : 0f;
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
