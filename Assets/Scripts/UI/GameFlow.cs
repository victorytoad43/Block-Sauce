using System;
using System.Threading;
using System.Threading.Tasks;
using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// The game's front-end flow (docs/PLAN.md, Phase 6). It opens on the menu over the demo match: an offline game in
    /// which bots play match after match, watched by the <see cref="SpectatorDirector"/>. Start Game shuts that down and
    /// hosts an online room under a short code; Join Game joins one by its code (never creating one) or any open room;
    /// leaving a room, or losing it, goes back to the menu and the demo. One change runs at a time; a request made
    /// while another is under way waits its turn.
    /// In the editor with Multiplayer Play Mode, the clones follow the main editor into and out of its rooms.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        /// <summary>
        /// Where the flow is.
        /// </summary>
        public enum FlowState
        {
            Starting,
            Menu,
            Connecting,
            InRoom,
        }

        /// <summary>
        /// Letters room codes are made of: consonants only, so codes can't spell words and have no O/0 or I/1 to mix up;
        /// no V either, which the Jaro font draws like a U.
        /// </summary>
        private const string CodeLetters = "BCDFGHJKLMNPQRSTWXZ";
        private const int CodeLength = 5;
        private const int DemoAttempts = 3;

        [Tooltip("Starts and stops the Quantum game.")]
        [SerializeField] private GameConnection _connection;

        private readonly SemaphoreSlim _busy = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The flow in the scene, or null.
        /// </summary>
        public static GameFlow Instance { get; private set; }

        /// <summary>
        /// Called whenever <see cref="State"/>, <see cref="RoomCode"/> or <see cref="Error"/> changes.
        /// </summary>
        public event Action Changed;

        public FlowState State { get; private set; } = FlowState.Starting;

        /// <summary>
        /// The code of the room this client is in, or null.
        /// </summary>
        public string RoomCode { get; private set; }

        /// <summary>
        /// The last failure to show the player (a wrong code, a lost connection), or null.
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Whether the menu has the screen (anything but being in a room): the HUD and the player's input stay out.
        /// </summary>
        public bool IsInMenu => State != FlowState.InRoom;

        /// <summary>
        /// Whether the pause menu is open in a room; the player's input stays out of the game meanwhile.
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Whether game input should reach the simulation: in a room, with the pause menu closed.
        /// </summary>
        public static bool AllowsGameInput => Instance == null || (Instance.IsInMenu == false && Instance.IsPaused == false);

        private void Awake()
        {
            Instance = this;
            GameSettings.Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private async void Start()
        {
            await Run(StartDemo);
        }

        private void OnApplicationQuit()
        {
            GameSettings.Save();
        }

        /// <summary>
        /// Hosts a new online room under a fresh code.
        /// </summary>
        public Task HostRoom()
        {
            return EnterRoom(NewCode(), joinOnly: false);
        }

        /// <summary>
        /// Joins the room with <paramref name="code"/>; it must already exist.
        /// </summary>
        public Task JoinRoom(string code)
        {
            code = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (code.Length == 0)
            {
                SetError("Type the room code first.");
                return Task.CompletedTask;
            }

            return EnterRoom(code, joinOnly: true);
        }

        /// <summary>
        /// Joins any open room, or reports that there is none.
        /// </summary>
        public Task JoinAnyRoom()
        {
            return EnterRoom(string.Empty, joinOnly: true);
        }

        /// <summary>
        /// Joins the room named <paramref name="name"/>, creating it if needed (dev launches and editor clones).
        /// </summary>
        public Task JoinOrCreateRoom(string name)
        {
            return EnterRoom(name, joinOnly: false);
        }

        /// <summary>
        /// Leaves the room and goes back to the menu and the demo match.
        /// </summary>
        public Task LeaveRoom()
        {
            return Run(async () =>
            {
                if (State != FlowState.InRoom)
                    return;

                SendToClones(new GameFlowMppmLeaveCommand());
                SetState(FlowState.Connecting, null);
                await _connection.StopAsync();
                await StartDemo();
            });
        }

        /// <summary>
        /// Clears the error shown in the menu.
        /// </summary>
        public void ClearError()
        {
            SetError(null);
        }

        private Task EnterRoom(string roomName, bool joinOnly)
        {
            return Run(async () =>
            {
                SetError(null);
                SetState(FlowState.Connecting, null);
                await _connection.StopAsync();
                try
                {
                    await _connection.StartAsync(new GameConnection.Request
                    {
                        Online = true,
                        RoomName = roomName,
                        JoinOnly = joinOnly,
                        PlayerName = "Player",
                        OnError = OnRoomLost,
                    });
                }
                catch (Exception exception)
                {
                    SetError(DescribeFailure(exception, roomName, joinOnly));
                    await StartDemo();
                    return;
                }

                SetState(FlowState.InRoom, _connection.RoomName);
                SendToClones(new GameFlowMppmJoinCommand { RoomName = _connection.RoomName });
            });
        }

        /// <summary>
        /// Starts the offline demo match behind the menu, trying again a couple of times if it fails to start; the menu
        /// works without it either way.
        /// </summary>
        private async Task StartDemo()
        {
            SetState(FlowState.Starting, null);
            for (int attempt = 1; attempt <= DemoAttempts; attempt++)
            {
                try
                {
                    await _connection.StartAsync(new GameConnection.Request { Online = false, PlayerName = "Demo" });
                    break;
                }
                catch (Exception exception)
                {
                    if (attempt == DemoAttempts)
                    {
                        Debug.LogException(exception);
                    }
                    else
                    {
                        Debug.LogWarning($"The demo match didn't start ({exception.Message}); trying again.");
                    }
                }
            }

            SetState(FlowState.Menu, null);
        }

        /// <summary>
        /// The running room shut down with an error: back to the menu, saying why.
        /// </summary>
        private async void OnRoomLost(string message)
        {
            await Run(async () =>
            {
                if (State != FlowState.InRoom)
                    return;

                SetError($"Lost the room: {message}");
                await _connection.StopAsync();
                await StartDemo();
            });
        }

        /// <summary>
        /// Runs one change of state at a time.
        /// </summary>
        private async Task Run(Func<Task> change)
        {
            await _busy.WaitAsync();
            try
            {
                await change();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _busy.Release();
            }
        }

        private void SetState(FlowState state, string roomCode)
        {
            State = state;
            RoomCode = roomCode;
            if (state != FlowState.InRoom)
            {
                IsPaused = false;
            }

            Changed?.Invoke();
        }

        private void SetError(string error)
        {
            Error = error;
            Changed?.Invoke();
        }

        private static string DescribeFailure(Exception exception, string roomName, bool joinOnly)
        {
            if (joinOnly && string.IsNullOrEmpty(roomName))
                return "No open rooms right now. Start a game instead?";

            if (joinOnly)
                return $"No room with the code {roomName}.";

            return $"Couldn't connect: {exception.Message}";
        }

        private static string NewCode()
        {
            var random = new System.Random();
            char[] code = new char[CodeLength];
            for (int i = 0; i < code.Length; i++)
            {
                code[i] = CodeLetters[random.Next(CodeLetters.Length)];
            }

            return new string(code);
        }

        private void SendToClones(QuantumMppmCommand command)
        {
#if UNITY_EDITOR
            if (_connection.MppmEnabled)
            {
                QuantumMppm.MainEditor?.Send(command);
            }
#endif
        }
    }

    /// <summary>
    /// Multiplayer Play Mode: tells the editor's clones to join the main editor's room by its code, the same way the
    /// Join Game screen does.
    /// </summary>
    [Serializable]
    public class GameFlowMppmJoinCommand : QuantumMppmCommand
    {
        public string RoomName;

        public override Task ExecuteAsync()
        {
            GameFlow flow = UnityEngine.Object.FindAnyObjectByType<GameFlow>();
            return flow != null ? flow.JoinRoom(RoomName) : Task.CompletedTask;
        }
    }

    /// <summary>
    /// Multiplayer Play Mode: tells the editor's clones to leave their room too.
    /// </summary>
    [Serializable]
    public class GameFlowMppmLeaveCommand : QuantumMppmCommand
    {
        public override Task ExecuteAsync()
        {
            GameFlow flow = UnityEngine.Object.FindAnyObjectByType<GameFlow>();
            return flow != null ? flow.LeaveRoom() : Task.CompletedTask;
        }
    }
}
