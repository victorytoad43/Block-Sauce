using System;
using System.Threading;
using System.Threading.Tasks;
using Photon.Realtime;
using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// Starts and stops the Quantum game for <see cref="GameFlow"/>: an offline demo game behind the menu, or an online
    /// room. Based on the SDK's QuantumStartUIConnection (it keeps that component's settings: server settings, session and
    /// runtime config, the local player, the app version and the Multiplayer Play Mode switch), with what the menu needs
    /// on top: joining a room by code without ever creating one (a typo shouldn't open an empty room), joining any open
    /// room, hosting under a chosen code, and starting the offline game with nobody in it and the demo match on.
    /// </summary>
    public class GameConnection : QuantumStartUIConnection
    {
        /// <summary>
        /// What to start.
        /// </summary>
        public struct Request
        {
            /// <summary>Online room, or the offline demo game.</summary>
            public bool Online;
            /// <summary>The room to host or join; empty joins any open room.</summary>
            public string RoomName;
            /// <summary>Only join an existing room; never create one.</summary>
            public bool JoinOnly;
            public string PlayerName;
            /// <summary>A fixed region code, or empty for the best one.</summary>
            public string Region;
            /// <summary>Called if the running game shuts down with an error (a lost connection, say).</summary>
            public Action<string> OnError;
        }

        /// <summary>
        /// Seconds the game may take to start before it counts as failed.
        /// </summary>
        private const float StartTimeoutSeconds = 30f;

        private RealtimeClient _client;
        private QuantumRunner _runner;
        private CancellationTokenSource _cancellation;

        public override string RoomName => _client?.CurrentRoom?.Name;
        public override string Region => _client?.CurrentRegion;
        public override int Ping => (int)(_client?.RealtimePeer?.Stats.RoundtripTime ?? 0);

        /// <summary>
        /// Whether Multiplayer Play Mode clones should follow this editor into rooms.
        /// </summary>
        public bool MppmEnabled => EnableMultiplayerPlayMode;

        /// <summary>
        /// Connects (online) and starts the game. Throws on failure, after cleaning up.
        /// </summary>
        public async Task StartAsync(Request request)
        {
            _cancellation = AsyncSetup.CreateLinkedSource(CancellationToken.None);
            try
            {
                if (request.Online)
                {
                    PhotonServerSettings serverSettings = ServerSettings;
                    if (serverSettings == null)
                    {
                        PhotonServerSettings.TryGetGlobal(out serverSettings);
                    }

                    var arguments = new MatchmakingArguments
                    {
                        PhotonSettings = new AppSettings(serverSettings.AppSettings)
                        {
                            AppVersion = AppVersion,
                            FixedRegion = string.IsNullOrEmpty(request.Region) ? null : request.Region,
                        },
                        EmptyRoomTtlInSeconds = serverSettings.EmptyRoomTtlInSeconds,
                        EnableCrc = serverSettings.EnableCrc,
                        PlayerTtlInSeconds = serverSettings.PlayerTtlInSeconds,
                        MaxPlayers = Quantum.Input.MAX_COUNT,
                        RoomName = request.RoomName,
                        CanOnlyJoin = request.JoinOnly,
                        PluginName = "QuantumPlugin",
                        AuthValues = new AuthenticationValues($"{request.PlayerName}({Guid.NewGuid():N})"),
                        AsyncConfig = new AsyncConfig
                        {
                            TaskFactory = AsyncConfig.CreateUnityTaskFactory(),
                            CancellationToken = _cancellation.Token,
                        },
                    };

                    _client = await MatchmakingExtensions.ConnectToRoomAsync(arguments);
                }

                var sessionArguments = new SessionRunner.Arguments();
                RuntimeConfig runtimeConfig = JsonUtility.FromJson<RuntimeConfig>(JsonUtility.ToJson(RuntimeConfig));
                runtimeConfig.DemoMatch = request.Online == false;
                if (request.Online)
                {
                    sessionArguments.InitForMultiplayer(runtimeConfig, _client, _client.UserId);
                }
                else
                {
                    sessionArguments.InitForLocal(runtimeConfig);
                }

                sessionArguments.SessionConfig = SessionConfig != null ? SessionConfig.Config : sessionArguments.SessionConfig;
                sessionArguments.CancellationToken = _cancellation.Token;
                sessionArguments.RecordingFlags = RecordingFlags;
                sessionArguments.InstantReplaySettings = InstantReplayConfig;
                sessionArguments.DeltaTimeType = DeltaTimeType;
                // The first start loads every asset and can hitch for longer than Quantum's 10 s default.
                sessionArguments.StartGameTimeoutInSeconds = StartTimeoutSeconds;
                Action<string> onError = request.OnError;
                sessionArguments.ShutdownCallback = args =>
                {
                    if (args.IsError && AsyncConfig.Global.IsCancellationRequested == false)
                    {
                        onError?.Invoke(args.Message);
                    }
                };

                _runner = (QuantumRunner)await SessionRunner.StartAsync(sessionArguments);

                // Online, this client plays; the offline demo has nobody in it, so bots take every slot.
                if (request.Online && Players.Length > 0)
                {
                    RuntimePlayer player = JsonUtility.FromJson<RuntimePlayer>(JsonUtility.ToJson(Players[0]));
                    player.PlayerNickname = request.PlayerName;
                    _runner.Game.AddPlayer(0, player);
                }
            }
            catch
            {
                await StopAsync();
                throw;
            }
        }

        /// <summary>
        /// Shuts the game down and leaves the room, if any.
        /// </summary>
        public async Task StopAsync()
        {
            if (_cancellation != null)
            {
                _cancellation.Cancel();
                _cancellation.Dispose();
                _cancellation = null;
            }

            if (_runner != null)
            {
                QuantumRunner runner = _runner;
                _runner = null;
                await runner.ShutdownAsync();
            }

            if (_client != null)
            {
                RealtimeClient client = _client;
                _client = null;
                await client.DisconnectAsync();
            }
        }

        /// <summary>
        /// The SDK menu's entry point, kept for anything that still calls it: an online join or create by name.
        /// </summary>
        public override Task ConnectAsync(StartParameter startParameter)
        {
            return StartAsync(new Request
            {
                Online = startParameter.IsOnline,
                RoomName = startParameter.RoomName,
                PlayerName = startParameter.PlayerName,
                Region = startParameter.Region,
                OnError = startParameter.OnConnectionError,
            });
        }

        public override Task DisconnectAsync()
        {
            return StopAsync();
        }
    }
}
