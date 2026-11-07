using System;
using System.Collections.Generic;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlockSauce
{
    /// <summary>
    /// The match HUD (docs/PLAN.md, Phase 6, step 7; a draft until Jackson's HUD art, A-006), in uGUI: the lobby's prompt,
    /// the countdown, GO!, each player's best tower as three slots (empty, settling with its progress, counted), the
    /// winner, and the results (everyone ranked by counted blocks, and the time until the room screen comes back). In
    /// the lobby the host starts the match with Enter (or the gamepad's Start, also over the room screen). Shown only in a
    /// room with no menu up. View only: it reads the predicted frame and sends the start command.
    /// Built by Block Sauce › Build Main Menu (PH_MatchHud); the references below are set there.
    /// </summary>
    public class MatchHud : MonoBehaviour
    {
        [Serializable]
        public struct TowerRow
        {
            public GameObject Root;
            public Image Swatch;
            public TMP_Text Name;
            public Image[] Slots;
        }

        [Serializable]
        public struct ResultRow
        {
            public GameObject Root;
            public Image Swatch;
            public TMP_Text Name;
            public TMP_Text Count;
        }

        [Tooltip("The player colors.")]
        [SerializeField] private PlayerPalette _palette;
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _headline;
        [SerializeField] private TMP_Text _headlineText;
        [SerializeField] private TMP_Text _bigText;
        [SerializeField] private TMP_Text _promptText;
        [SerializeField] private GameObject _towerPanel;
        [SerializeField] private TowerRow[] _towerRows;
        [SerializeField] private GameObject _resultsPanel;
        [SerializeField] private TMP_Text _resultsWinner;
        [SerializeField] private ResultRow[] _resultRows;
        [SerializeField] private TMP_Text _resultsFooter;
        [Tooltip("How long GO! shows once the match starts, in seconds.")]
        [SerializeField] private float _goSeconds = 1f;

        private struct Standing
        {
            public PlayerRef Player;
            public int Counted;
            public float Progress;
        }

        private readonly List<Standing> _standings = new List<Standing>();
        private MatchState _lastState = (MatchState)(-1);
        private float _playingSince;
        private int _shownSeconds = -1;
        private string _shownBig;
        private bool _shownHost;
        private float _nextResultsRefresh;
        private int[] _towerKeys = Array.Empty<int>();
        private int _shownClock = -1;
        private bool _shownSuddenDeath;
        private float _suddenDeathBigUntil;
        private const string PlayingHeadline = "STACK ALL 3 BLOCKS INTO ONE TOWER";
        private const float SuddenDeathBigSeconds = 2.5f;

        private void Update()
        {
            QuantumGame game = QuantumRunner.Default?.Game;
            Frame frame = game?.Frames.Predicted;
            if (_root == null)
                return;

            GameFlow flow = GameFlow.Instance;
            bool inRoom = flow == null || flow.State == GameFlow.FlowState.InRoom;
            if (frame == null || frame.TryGetSingleton(out Match match) == false || inRoom == false)
            {
                _root.SetActive(false);
                _lastState = (MatchState)(-1);
                return;
            }

            bool isHost = RoomScreen.IsLocalHost(game);
            if (match.State == MatchState.Lobby && isHost && StartPressed())
            {
                game.AddCommand(new CommandStartMatch());
            }

            if (match.State != _lastState)
            {
                EnterState(match.State, isHost);
            }

            bool visible = flow == null || flow.IsPaused == false;
            _root.SetActive(visible);
            if (visible == false)
                return;

            GameModeConfig mode = frame.FindAsset(match.Mode);
            switch (match.State)
            {
                case MatchState.Lobby:
                    if (isHost != _shownHost)
                    {
                        ShowPrompt(isHost);
                    }

                    break;
                case MatchState.Countdown:
                    ShowSeconds(match.Timer.RemainingSeconds(frame).AsFloat, s => s.ToString());
                    UpdateTowers(frame, mode);
                    break;
                case MatchState.Playing:
                    UpdateClock(frame, match, mode);
                    ShowBig(Time.time - _playingSince < _goSeconds ? "GO!"
                        : Time.time < _suddenDeathBigUntil ? "SUDDEN DEATH!" : string.Empty);
                    UpdateTowers(frame, mode);
                    break;
                case MatchState.RoundEnd:
                    UpdateTowers(frame, mode);
                    break;
                case MatchState.Results:
                    ShowSeconds(match.Timer.RemainingSeconds(frame).AsFloat, null);
                    if (Time.unscaledTime >= _nextResultsRefresh)
                    {
                        _nextResultsRefresh = Time.unscaledTime + 0.5f;
                        ShowResults(frame, mode, match.Winner);
                    }

                    break;
            }
        }

        private static bool StartPressed()
        {
            // Enter only while playing (over the room screen it presses the selected button); Start always.
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (GameFlow.AllowsGameInput && keyboard != null && (keyboard.enterKey.wasPressedThisFrame
                       || keyboard.numpadEnterKey.wasPressedThisFrame))
                || (gamepad != null && gamepad.startButton.wasPressedThisFrame);
        }

        /// <summary>
        /// Sets up the panels for a new match state.
        /// </summary>
        private void EnterState(MatchState state, bool isHost)
        {
            _lastState = state;
            _shownSeconds = -1;
            _shownBig = null;
            _nextResultsRefresh = 0f;

            bool towers = state == MatchState.Countdown || state == MatchState.Playing || state == MatchState.RoundEnd;
            _towerPanel.SetActive(towers);
            _resultsPanel.SetActive(state == MatchState.Results);
            // The prompt's bar is its parent.
            _promptText.transform.parent.gameObject.SetActive(state == MatchState.Lobby);
            _headline.SetActive(state == MatchState.Countdown || state == MatchState.Playing);
            _bigText.color = Color.white;
            ShowBig(string.Empty);
            for (int i = 0; i < _towerKeys.Length; i++)
            {
                _towerKeys[i] = -1;
            }

            switch (state)
            {
                case MatchState.Lobby:
                    ShowPrompt(isHost);
                    break;
                case MatchState.Countdown:
                    _headlineText.text = "GET READY";
                    break;
                case MatchState.Playing:
                    _playingSince = Time.time;
                    _shownClock = -1;
                    _shownSuddenDeath = false;
                    _suddenDeathBigUntil = 0f;
                    _headlineText.text = PlayingHeadline;
                    break;
                case MatchState.RoundEnd:
                    ShowWinner();
                    break;
            }
        }

        private void ShowWinner()
        {
            Frame frame = QuantumRunner.Default?.Game?.Frames.Predicted;
            if (frame == null || frame.TryGetSingleton(out Match match) == false)
                return;

            PlayerRef winner = match.Winner;
            if (winner.IsValid)
            {
                _bigText.color = GetColor(winner);
                ShowBig($"{GetName(frame, winner)} WINS!");
            }
            else
            {
                ShowBig(match.SuddenDeath ? "DRAW" : "NO WINNER");
            }
        }

        /// <summary>
        /// The match clock in the headline (A-006 draft: the headline bar, top center); in sudden death the headline
        /// says so, with sudden death's own clock, and SUDDEN DEATH! shows big for a moment as it starts.
        /// </summary>
        private void UpdateClock(Frame frame, Match match, GameModeConfig mode)
        {
            if (mode == null || mode.MatchSeconds <= 0)
                return;

            if (match.SuddenDeath && _shownSuddenDeath == false)
            {
                _shownSuddenDeath = true;
                _shownClock = -1;
                _suddenDeathBigUntil = Time.time + SuddenDeathBigSeconds;
            }

            int seconds = Mathf.Max(0, Mathf.CeilToInt(match.Timer.RemainingSeconds(frame).AsFloat));
            if (seconds == _shownClock)
                return;

            _shownClock = seconds;
            string clock = $"{seconds / 60}:{seconds % 60:00}";
            _headlineText.text = match.SuddenDeath
                ? $"SUDDEN DEATH {clock}   NEXT BLOCK WINS"
                : $"{clock}   {PlayingHeadline}";
        }

        private void ShowPrompt(bool isHost)
        {
            _shownHost = isHost;
            _promptText.text = isHost
                ? "ENTER / START  START MATCH        ESC / SELECT  ROOM"
                : "WAITING FOR THE HOST TO START        ESC / SELECT  ROOM";
        }

        private void ShowBig(string text)
        {
            if (text == _shownBig)
                return;

            _shownBig = text;
            _bigText.text = text;
            _bigText.gameObject.SetActive(string.IsNullOrEmpty(text) == false);
        }

        /// <summary>
        /// Shows whole seconds left: in the big text (the countdown) or in the results' footer.
        /// </summary>
        private void ShowSeconds(float remaining, Func<int, string> big)
        {
            int seconds = Mathf.Max(1, Mathf.CeilToInt(remaining));
            if (seconds == _shownSeconds)
                return;

            _shownSeconds = seconds;
            if (big != null)
            {
                ShowBig(big(seconds));
            }
            else
            {
                _resultsFooter.text = $"BACK TO THE ROOM IN {seconds}";
            }
        }

        private void UpdateTowers(Frame frame, GameModeConfig mode)
        {
            if (_towerKeys.Length != _towerRows.Length)
            {
                _towerKeys = new int[_towerRows.Length];
                for (int i = 0; i < _towerKeys.Length; i++)
                {
                    _towerKeys[i] = -1;
                }
            }

            // One row per player, in slot order.
            int row = 0;
            for (int index = 0; index < frame.MaxPlayerCount && row < _towerRows.Length; index++)
            {
                PlayerRef player = index;
                if (PlayerSystem.TryGetCharacter(frame, player, out EntityRef _) == false)
                    continue;

                TowerRow tower = _towerRows[row++];
                tower.Root.SetActive(true);
                Color color = GetColor(player);
                tower.Swatch.color = color;
                // Names are rebuilt only when the row's player or bot status changes.
                int key = index * 2 + (IsBot(frame, player) ? 1 : 0);
                if (_towerKeys[row - 1] != key)
                {
                    _towerKeys[row - 1] = key;
                    tower.Name.text = GetName(frame, player);
                }

                EntityRef towerBase = BlockTower.GetBestBase(frame, player);
                int height = mode != null ? mode.TowerHeight : tower.Slots.Length;
                for (int level = 0; level < tower.Slots.Length; level++)
                {
                    Image slot = tower.Slots[level];
                    slot.transform.parent.gameObject.SetActive(level < height);
                    GetSlot(frame, towerBase, level, out bool counted, out float progress);
                    // The fill grows from the bottom of the slot; set only on change, to keep the canvas still.
                    float fill = counted ? 1f : progress;
                    RectTransform rect = slot.rectTransform;
                    if (Mathf.Abs(rect.anchorMax.y - fill) > 0.01f || (fill == 0f) != (rect.anchorMax.y == 0f))
                    {
                        rect.anchorMax = new Vector2(1f, fill);
                    }

                    Color shade = counted ? color : Color.Lerp(color, Color.black, 0.5f);
                    if (slot.color != shade)
                    {
                        slot.color = shade;
                    }
                }
            }

            for (; row < _towerRows.Length; row++)
            {
                _towerRows[row].Root.SetActive(false);
            }
        }

        private void ShowResults(Frame frame, GameModeConfig mode, PlayerRef winner)
        {
            _standings.Clear();
            var players = frame.Filter<Player>();
            while (players.Next(out EntityRef _, out Player player))
            {
                EntityRef towerBase = BlockTower.GetBestBase(frame, player.Ref);
                var standing = new Standing { Player = player.Ref };
                int height = mode != null ? mode.TowerHeight : 3;
                for (int level = 0; level < height; level++)
                {
                    GetSlot(frame, towerBase, level, out bool counted, out float progress);
                    standing.Counted += counted ? 1 : 0;
                    standing.Progress += progress;
                }

                _standings.Add(standing);
            }

            // The winner first, then by counted blocks, then by blocks still settling, then by slot.
            _standings.Sort((a, b) =>
            {
                if (a.Player == winner != (b.Player == winner))
                    return a.Player == winner ? -1 : 1;
                if (a.Counted != b.Counted)
                    return b.Counted.CompareTo(a.Counted);
                if (Mathf.Approximately(a.Progress, b.Progress) == false)
                    return b.Progress.CompareTo(a.Progress);
                return ((int)a.Player).CompareTo((int)b.Player);
            });

            bool draw = frame.TryGetSingleton(out Match match) && match.SuddenDeath;
            _resultsWinner.text = winner.IsValid ? $"{GetName(frame, winner)} WINS!" : draw ? "DRAW" : "NO WINNER";
            _resultsWinner.color = winner.IsValid ? GetColor(winner) : Color.white;
            int towerHeight = mode != null ? mode.TowerHeight : 3;
            for (int i = 0; i < _resultRows.Length; i++)
            {
                ResultRow row = _resultRows[i];
                row.Root.SetActive(i < _standings.Count);
                if (i >= _standings.Count)
                    continue;

                Standing standing = _standings[i];
                row.Swatch.color = GetColor(standing.Player);
                row.Name.text = $"{i + 1}.  {GetName(frame, standing.Player)}";
                row.Count.text = $"{standing.Counted} / {towerHeight}";
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

        private Color GetColor(PlayerRef player)
        {
            return _palette != null ? _palette.GetColor((int)player) : Color.white;
        }

        /// <summary>
        /// A player's name on the HUD: PLAYER n, or BOT n for a slot the simulation drives.
        /// </summary>
        private static string GetName(Frame frame, PlayerRef player)
        {
            return IsBot(frame, player) ? $"BOT {(int)player + 1}" : $"PLAYER {(int)player + 1}";
        }

        /// <summary>
        /// Whether the slot is a bot's: driven by the simulation with nobody connected to it.
        /// </summary>
        private static bool IsBot(Frame frame, PlayerRef player)
        {
            return PlayerSystem.TryGetCharacter(frame, player, out EntityRef character) && frame.Has<Bot>(character)
                && RoomScreen.IsConnected(QuantumRunner.Default?.Game, player) == false;
        }
    }
}
