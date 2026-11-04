using System;
using Photon.Deterministic;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace BlockSauce
{
    /// <summary>
    /// The room screen (docs/PLAN.md, Phase 6, step 7; a draft until Jackson's room art, A-002): the room code to share,
    /// every player slot in its color (players, bots, open slots; you and the host marked), and for the host the room's
    /// controls: start the match, add and remove a bot, and whether bots fill the empty slots at the start. Everyone gets
    /// Play in Lobby (close the screen and walk around), Settings and Leave Room, which <see cref="MainMenu"/> wires up.
    /// View only: it reads the predicted frame and sends commands. Built by Block Sauce › Build Main Menu.
    /// </summary>
    public class RoomScreen : MonoBehaviour
    {
        [Serializable]
        public struct Row
        {
            public GameObject Root;
            public Image Swatch;
            public TMP_Text Name;
            public TMP_Text Tag;
        }

        [SerializeField] private PlayerPalette _palette;
        [SerializeField] private TMP_Text _codeText;
        [SerializeField] private Row[] _rows;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _addBotButton;
        [SerializeField] private Button _removeBotButton;
        [SerializeField] private Toggle _fillToggle;
        [SerializeField] private GameObject _fillRow;
        [SerializeField] private TMP_Text _waitingText;

        private enum SlotKind
        {
            Open,
            Player,
            Bot,
        }

        // What each row shows now, so text is only rebuilt when it changes.
        private int[] _shown = Array.Empty<int>();
        private string _shownCode;
        private bool _shownHost;

        /// <summary>
        /// Whether the local client is the room's host: it holds the lowest slot of everyone connected. Every client
        /// works it out the same way from the frame, so the host's leaving hands the controls to the next player.
        /// </summary>
        public static bool IsLocalHost(QuantumGame game)
        {
            Frame verified = game?.Frames.Verified;
            if (verified == null)
                return false;

            for (int slot = 0; slot < verified.MaxPlayerCount; slot++)
            {
                if (IsConnected(verified, slot))
                    return game.PlayerIsLocal(slot);
            }

            return false;
        }

        /// <summary>
        /// Whether a client is connected to the slot, read from the verified frame: on predicted frames the other
        /// clients' slots look empty, since their input (and its flags) is only a guess until the server confirms it.
        /// </summary>
        public static bool IsConnected(QuantumGame game, PlayerRef slot)
        {
            Frame verified = game?.Frames.Verified;
            return verified != null && IsConnected(verified, slot);
        }

        /// <summary>
        /// The button the screen selects first: Start Match for the host, otherwise <paramref name="fallback"/>.
        /// </summary>
        public Selectable FirstSelectable(Selectable fallback)
        {
            return _startButton != null && _startButton.gameObject.activeInHierarchy ? _startButton : fallback;
        }

        // Only meaningful on a verified frame.
        private static bool IsConnected(Frame frame, PlayerRef slot)
        {
            return (frame.GetPlayerInputFlags(slot) & DeterministicInputFlags.PlayerNotPresent) == 0;
        }

        private void Awake()
        {
            _startButton.onClick.AddListener(() => Send(new CommandStartMatch()));
            _addBotButton.onClick.AddListener(() => Send(new CommandAddBot()));
            _removeBotButton.onClick.AddListener(() => Send(new CommandRemoveBot()));
            _fillToggle.onValueChanged.AddListener(fill => Send(new CommandSetBotFill { Fill = fill }));
        }

        private void OnEnable()
        {
            _shown = new int[_rows.Length];
            for (int i = 0; i < _shown.Length; i++)
            {
                _shown[i] = -1;
            }

            _shownCode = null;
            Refresh(force: true);
        }

        private void Update()
        {
            Refresh(force: false);
        }

        private static void Send(DeterministicCommand command)
        {
            QuantumRunner.Default?.Game?.AddCommand(command);
        }

        private void Refresh(bool force)
        {
            // The verified frame throughout: it is the one that says who is connected (see IsConnected), and it keeps
            // the rows and the controls in step with each other.
            QuantumGame game = QuantumRunner.Default?.Game;
            Frame frame = game?.Frames.Verified;
            if (frame == null || frame.TryGetSingleton(out Match match) == false)
                return;

            string code = GameFlow.Instance != null ? GameFlow.Instance.RoomCode : string.Empty;
            if (code != _shownCode)
            {
                _shownCode = code;
                _codeText.text = $"ROOM  {code}";
            }

            int humans = 0;
            int bots = 0;
            for (int i = 0; i < _rows.Length; i++)
            {
                bool inGame = i < frame.MaxPlayerCount;
                SlotKind kind = inGame ? GetSlotKind(frame, i) : SlotKind.Open;
                humans += kind == SlotKind.Player ? 1 : 0;
                bots += kind == SlotKind.Bot ? 1 : 0;
                bool local = inGame && game.PlayerIsLocal(i);
                bool host = inGame && IsConnected(frame, i) && IsLowestConnected(frame, i);
                // One number for what the row shows, to skip rebuilding it when nothing changed.
                int state = (int)kind | (local ? 4 : 0) | (host ? 8 : 0) | (inGame ? 16 : 0);
                if (force == false && _shown[i] == state)
                    continue;

                _shown[i] = state;
                ShowRow(_rows[i], i, kind, local, host, inGame);
            }

            bool isHost = IsLocalHost(game);
            if (force || isHost != _shownHost)
            {
                _shownHost = isHost;
                _startButton.gameObject.SetActive(isHost);
                _addBotButton.gameObject.SetActive(isHost);
                _removeBotButton.gameObject.SetActive(isHost);
                _fillRow.SetActive(isHost);
                _waitingText.gameObject.SetActive(isHost == false);
                if (isHost && force == false && EventSystem.current != null)
                {
                    // Becoming host (on the room's first frames, or when the host leaves): Start Match first.
                    EventSystem.current.SetSelectedGameObject(_startButton.gameObject);
                }
                if (isHost == false && EventSystem.current != null
                    && EventSystem.current.currentSelectedGameObject != null
                    && EventSystem.current.currentSelectedGameObject.activeInHierarchy == false)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }

            bool lobby = match.State == MatchState.Lobby;
            _startButton.interactable = lobby;
            _addBotButton.interactable = lobby && humans + bots < frame.MaxPlayerCount;
            _removeBotButton.interactable = lobby && bots > 0;
            _fillToggle.interactable = lobby;
            _fillToggle.SetIsOnWithoutNotify(match.LeaveSlotsEmpty == false);
        }

        private static bool IsLowestConnected(Frame frame, int slot)
        {
            for (int other = 0; other < slot; other++)
            {
                if (IsConnected(frame, other))
                    return false;
            }

            return true;
        }

        private static SlotKind GetSlotKind(Frame frame, PlayerRef slot)
        {
            if (PlayerSystem.TryGetCharacter(frame, slot, out EntityRef character))
                return frame.Has<Bot>(character) && IsConnected(frame, slot) == false ? SlotKind.Bot : SlotKind.Player;

            return IsConnected(frame, slot) ? SlotKind.Player : SlotKind.Open;
        }

        private void ShowRow(Row row, int slot, SlotKind kind, bool local, bool host, bool inGame)
        {
            row.Root.SetActive(inGame);
            if (inGame == false)
                return;

            Color color = _palette != null ? _palette.GetColor(slot) : Color.white;
            bool open = kind == SlotKind.Open;
            row.Swatch.color = open ? new Color(color.r, color.g, color.b, 0.25f) : color;
            row.Name.text = open ? "OPEN" : kind == SlotKind.Bot ? $"BOT {slot + 1}" : $"PLAYER {slot + 1}";
            row.Name.alpha = open ? 0.45f : 1f;

            string tag = kind == SlotKind.Bot ? "BOT" : string.Empty;
            if (local)
            {
                tag = host ? "YOU · HOST" : "YOU";
            }
            else if (host)
            {
                tag = "HOST";
            }

            row.Tag.text = tag;
        }
    }
}
