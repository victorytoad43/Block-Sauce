using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// A hands-off spectator camera (docs/PLAN.md, Phase 6): a director that watches the match and points the camera at
    /// whatever is most interesting, without any input. Each frame it scores the candidates: a tower close to complete
    /// (more for more counted blocks, more again while one settles), a block in flight after a throw, a knockdown, a
    /// scramble of players around a vat, and, always, a wide shot of everyone. It holds each shot for a few seconds and
    /// switches when another subject is clearly more interesting or the shot has run its course: an ease when the new
    /// subject is near, a cut when it is across the map. Framing is from above and to the side, drifting slowly round
    /// the subject, kept inside the arena.
    /// That is the Auto mode. A viewer can also take over (see <see cref="SpectatorControls"/>): Follow keeps the camera
    /// on one player, Overview holds the wide shot; Auto hands back to the director.
    /// While enabled it overrides the camera's transform after the orbit camera has placed it. View only: it reads the
    /// predicted frame and simulation events and never writes back.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SpectatorDirector : MonoBehaviour
    {
        /// <summary>
        /// Who chooses the shot: the director, or the viewer following a player or holding the overview.
        /// </summary>
        public enum ViewMode
        {
            Auto,
            Follow,
            Overview,
        }

        private enum ShotKind
        {
            Follow,
            Wide,
            Tower,
            Throw,
            Knockdown,
            Scramble,
        }

        private struct Shot
        {
            public ShotKind Kind;
            public EntityRef Subject;
            public EntityRef Top;
            public PlayerRef Player;
            public Vector3 Focus;
            public float Score;
            public float Distance;
        }

        [Header("Holding shots")]
        [Tooltip("Seconds between rescoring what is worth watching; the current shot still follows its subject every "
            + "frame.")]
        [SerializeField] private float _rescoreSeconds = 0.1f;
        [Tooltip("Seconds a shot is held before the director may switch away from it.")]
        [SerializeField] private float _minShotSeconds = 3f;
        [Tooltip("Seconds after which a shot gives way to anything else worth watching.")]
        [SerializeField] private float _maxShotSeconds = 7f;
        [Tooltip("How much more interesting another subject must be to replace the current shot early.")]
        [SerializeField] private float _switchMargin = 2f;
        [Tooltip("A new subject closer than this to the current focus, in meters, is eased to; further away, cut to.")]
        [SerializeField] private float _cutDistance = 8f;

        [Header("Framing")]
        [Tooltip("Degrees the camera looks down.")]
        [SerializeField] private float _pitch = 35f;
        [Tooltip("Degrees per second the camera drifts round its subject.")]
        [SerializeField] private float _driftSpeed = 5f;
        [Tooltip("Camera distance, in meters: a close shot (knockdown, throw), a tower, a scramble, and the wide shot at "
            + "its shortest (it pulls back to fit everyone).")]
        [SerializeField] private float _closeDistance = 6f;
        [SerializeField] private float _towerDistance = 8f;
        [SerializeField] private float _scrambleDistance = 9f;
        [SerializeField] private float _wideDistance = 14f;
        [Tooltip("Camera distance, in meters, while following a player.")]
        [SerializeField] private float _followDistance = 7f;
        [Tooltip("Furthest the wide shot pulls back, in meters.")]
        [SerializeField] private float _maxWideDistance = 26f;
        [Tooltip("Seconds the camera takes to settle on a moving focus, and to ease between shots.")]
        [SerializeField] private float _followSmoothTime = 0.6f;
        [SerializeField] private float _easeSeconds = 1.2f;
        [Tooltip("Half the size of the area, in meters around the origin, the camera stays inside.")]
        [SerializeField] private float _arenaHalfSize = 18f;
        [Tooltip("Lowest the camera goes, in meters.")]
        [SerializeField] private float _minHeight = 3f;

        [Header("Interest")]
        [Tooltip("Seconds a throw or knockdown stays worth watching.")]
        [SerializeField] private float _throwSeconds = 2f;
        [SerializeField] private float _knockdownSeconds = 3f;
        [Tooltip("Meters from a vat's center within which players count as a scramble.")]
        [SerializeField] private float _scrambleRadius = 3f;

        private readonly List<Shot> _candidates = new List<Shot>();
        private readonly Dictionary<EntityRef, float> _throws = new Dictionary<EntityRef, float>();
        private readonly Dictionary<EntityRef, float> _knockdowns = new Dictionary<EntityRef, float>();
        private QuantumEntityViewUpdater _views;
        private readonly List<PlayerRef> _players = new List<PlayerRef>();
        private ViewMode _mode;
        private PlayerRef _followed = PlayerRef.None;
        private Shot _shot;
        private bool _hasShot;
        private float _nextRescore;
        private string _description = "Auto";
        private float _shotStarted;
        private float _yaw;
        private Vector3 _focus;
        private Vector3 _focusVelocity;
        private float _distance;
        private float _distanceVelocity;
        private float _easeStarted = -100f;
        private Vector3 _easeFromPosition;
        private Quaternion _easeFromRotation;

        /// <summary>
        /// Starts or stops directing; stopping hands the camera back to whatever placed it before.
        /// </summary>
        public bool IsDirecting
        {
            get => enabled;
            set => enabled = value;
        }

        /// <summary>
        /// The kind and subject of the current shot, for tests and debugging.
        /// </summary>
        public string CurrentShot => _hasShot ? $"{_shot.Kind} {_shot.Subject} score {_shot.Score:F1}" : "none";

        /// <summary>
        /// Who chooses the shot now.
        /// </summary>
        public ViewMode Mode => _mode;

        /// <summary>
        /// The player the camera follows in Follow mode, or none.
        /// </summary>
        public PlayerRef FollowedPlayer => _mode == ViewMode.Follow ? _followed : PlayerRef.None;

        /// <summary>
        /// What the camera shows, in a few words for the spectator controls: the mode and, where there is one, whose.
        /// </summary>
        public string Describe() => _description;

        private string BuildDescription()
        {
            switch (_mode)
            {
                case ViewMode.Follow:
                    return $"Following {GetPlayerName(_followed)}";
                case ViewMode.Overview:
                    return "Overview";
            }

            if (_hasShot == false)
                return "Auto";

            switch (_shot.Kind)
            {
                case ShotKind.Tower:
                    return $"Auto · {GetPlayerName(_shot.Player)}'s tower";
                case ShotKind.Throw:
                    return "Auto · a throw";
                case ShotKind.Knockdown:
                    return "Auto · a knockdown";
                case ShotKind.Scramble:
                    return "Auto · a scramble at the vat";
                default:
                    return "Auto · everyone";
            }
        }

        /// <summary>
        /// Hands the shot back to the director.
        /// </summary>
        public void SetAuto()
        {
            _mode = ViewMode.Auto;
            _nextRescore = 0f;
        }

        /// <summary>
        /// Holds the wide shot of everyone until another mode is chosen.
        /// </summary>
        public void ShowOverview()
        {
            _mode = ViewMode.Overview;
            _nextRescore = 0f;
        }

        /// <summary>
        /// Follows the next player in slot order (<paramref name="direction"/> 1) or the previous one (-1), wrapping
        /// round; from Auto or Overview it starts from whoever the shot was about.
        /// </summary>
        public void FollowNext(int direction)
        {
            Frame frame = QuantumRunner.Default?.Game?.Frames.Predicted;
            if (frame == null)
                return;

            GatherPlayers(frame);
            if (_players.Count == 0)
                return;

            PlayerRef from = _mode == ViewMode.Follow ? _followed : _hasShot ? _shot.Player : PlayerRef.None;
            int index = _players.IndexOf(from);
            index = index < 0 ? (direction > 0 ? 0 : _players.Count - 1)
                : (index + direction + _players.Count) % _players.Count;
            _followed = _players[index];
            _mode = ViewMode.Follow;
            _nextRescore = 0f;
        }

        /// <summary>
        /// A player's name as the HUD shows it.
        /// </summary>
        public static string GetPlayerName(PlayerRef player)
        {
            return player.IsValid ? $"Player {(int)player + 1}" : "nobody";
        }

        private void OnEnable()
        {
            _hasShot = false;
            _nextRescore = 0f;
            QuantumEvent.Subscribe(this, (EventThrown e) => _throws[e.Held] = Time.time + _throwSeconds);
            QuantumEvent.Subscribe(this, (EventRagdollChanged e) =>
            {
                if (e.Active)
                {
                    _knockdowns[e.Entity] = Time.time + _knockdownSeconds;
                }
            });
        }

        private void OnDisable()
        {
            QuantumEvent.UnsubscribeListener(this);
            _throws.Clear();
            _knockdowns.Clear();
        }

        private void LateUpdate()
        {
            Frame frame = QuantumRunner.Default?.Game?.Frames.Predicted;
            if (frame == null)
                return;

            if (_views == null)
            {
                _views = FindAnyObjectByType<QuantumEntityViewUpdater>();
            }

            if (Time.time < _nextRescore)
            {
                if (_hasShot)
                {
                    RefreshFocus(frame, ref _shot);
                    PlaceCamera(_shot);
                }

                return;
            }

            _nextRescore = Time.time + _rescoreSeconds;
            GatherCandidates(frame);
            if (_candidates.Count == 0)
                return;

            switch (_mode)
            {
                case ViewMode.Follow:
                    if (TryGetFollowShot(frame, out Shot follow))
                    {
                        KeepOrStart(follow);
                    }
                    else
                    {
                        // The player left: back to the director.
                        _mode = ViewMode.Auto;
                        ChooseShot();
                    }
                    break;
                case ViewMode.Overview:
                    foreach (Shot candidate in _candidates)
                    {
                        if (candidate.Kind == ShotKind.Wide)
                        {
                            KeepOrStart(candidate);
                            break;
                        }
                    }
                    break;
                default:
                    ChooseShot();
                    break;
            }

            _description = BuildDescription();
            PlaceCamera(_shot);
        }

        /// <summary>
        /// Moves a shot's focus with its subject, between rescores.
        /// </summary>
        private void RefreshFocus(Frame frame, ref Shot shot)
        {
            switch (shot.Kind)
            {
                case ShotKind.Wide:
                    GetEveryone(frame, out shot.Focus, out shot.Distance);
                    break;
                case ShotKind.Tower:
                    if (frame.Exists(shot.Subject) && frame.Exists(shot.Top))
                    {
                        shot.Focus = Vector3.Lerp(GetPosition(frame, shot.Subject), GetPosition(frame, shot.Top), 0.5f);
                    }
                    break;
                case ShotKind.Follow:
                    if (frame.Exists(shot.Subject))
                    {
                        shot.Focus = GetPosition(frame, shot.Subject) + Vector3.up;
                    }
                    break;
                case ShotKind.Throw:
                case ShotKind.Knockdown:
                    if (frame.Exists(shot.Subject))
                    {
                        shot.Focus = GetPosition(frame, shot.Subject);
                    }
                    break;
            }
        }

        /// <summary>
        /// The middle of everyone, and how far back the wide shot must be to fit them; false when nobody is playing.
        /// </summary>
        private bool GetEveryone(Frame frame, out Vector3 center, out float distance)
        {
            Bounds everyone = default;
            int players = 0;
            var characters = frame.Filter<Player, Transform3D>();
            while (characters.Next(out EntityRef entity, out Player _, out Transform3D _))
            {
                Vector3 position = GetPosition(frame, entity);
                if (players == 0)
                {
                    everyone = new Bounds(position, Vector3.zero);
                }
                else
                {
                    everyone.Encapsulate(position);
                }

                players++;
            }

            center = everyone.center;
            distance = Mathf.Clamp(everyone.extents.magnitude * 1.6f, _wideDistance, _maxWideDistance);
            return players > 0;
        }

        /// <summary>
        /// Scores everything worth watching (every <see cref="_rescoreSeconds"/>).
        /// </summary>
        private void GatherCandidates(Frame frame)
        {
            _candidates.Clear();
            float now = Time.time;

            // The wide shot: everyone in view.
            if (GetEveryone(frame, out Vector3 everyoneCenter, out float everyoneDistance))
            {
                _candidates.Add(new Shot
                {
                    Kind = ShotKind.Wide,
                    Focus = everyoneCenter,
                    Score = 1f,
                    Distance = everyoneDistance,
                });
            }

            // Towers: the more counted, the better; one settling toward a win is the best of all.
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                EntityRef towerBase = BlockTower.GetBestBase(frame, player);
                if (towerBase == EntityRef.None)
                    continue;

                EntityRef top = BlockTower.GetTop(frame, towerBase);
                int counted = BlockTower.CountBest(frame, player);
                Block topBlock = frame.Get<Block>(top);
                bool settling = topBlock.Stack == StackState.Settling;
                int height = topBlock.StackLevel != BlockStackSystem.NotStacked ? topBlock.StackLevel + 1 : 1;
                float score = 1f + counted * 1.5f + (settling ? 1.5f * height : 0f);
                _candidates.Add(new Shot
                {
                    Kind = ShotKind.Tower,
                    Subject = towerBase,
                    Top = top,
                    Player = player,
                    Focus = Vector3.Lerp(GetPosition(frame, towerBase), GetPosition(frame, top), 0.5f),
                    Score = score,
                    Distance = _towerDistance,
                });
            }

            // Throws: the block while it is still flying.
            foreach (KeyValuePair<EntityRef, float> thrown in _throws)
            {
                if (thrown.Value < now || frame.Exists(thrown.Key) == false)
                    continue;

                _candidates.Add(new Shot
                {
                    Kind = ShotKind.Throw,
                    Subject = thrown.Key,
                    Focus = GetPosition(frame, thrown.Key),
                    Score = 6f,
                    Distance = _closeDistance,
                });
            }

            // Knockdowns: the body on the ground.
            foreach (KeyValuePair<EntityRef, float> knocked in _knockdowns)
            {
                if (knocked.Value < now || frame.Exists(knocked.Key) == false)
                    continue;

                _candidates.Add(new Shot
                {
                    Kind = ShotKind.Knockdown,
                    Subject = knocked.Key,
                    Player = frame.TryGet(knocked.Key, out Player knockedPlayer) ? knockedPlayer.Ref : PlayerRef.None,
                    Focus = GetPosition(frame, knocked.Key),
                    Score = 5f,
                    Distance = _closeDistance,
                });
            }

            // Scrambles: two or more players around a vat.
            var vats = frame.Filter<SauceVat, Transform3D>();
            while (vats.Next(out EntityRef vat, out SauceVat _, out Transform3D vatTransform))
            {
                Vector3 center = vatTransform.Position.ToUnityVector3();
                int near = 0;
                var characters = frame.Filter<Player, Transform3D>();
                while (characters.Next(out EntityRef entity, out Player _, out Transform3D _))
                {
                    Vector3 offset = GetPosition(frame, entity) - center;
                    offset.y = 0f;
                    if (offset.magnitude < _scrambleRadius)
                    {
                        near++;
                    }
                }

                if (near >= 2)
                {
                    _candidates.Add(new Shot
                    {
                        Kind = ShotKind.Scramble,
                        Subject = vat,
                        Focus = center + Vector3.up * 0.5f,
                        Score = 1f + near,
                        Distance = _scrambleDistance,
                    });
                }
            }

            PruneExpired(_throws, now);
            PruneExpired(_knockdowns, now);
        }

        /// <summary>
        /// Keeps the current shot while it is fresh; then moves on when something else is clearly more interesting, when
        /// its subject is gone, or, once it has run long, to the next best thing.
        /// </summary>
        private void ChooseShot()
        {
            bool currentAlive = false;
            bool hasOther = false;
            Shot best = default;
            foreach (Shot candidate in _candidates)
            {
                if (_hasShot && candidate.Kind == _shot.Kind && candidate.Subject == _shot.Subject)
                {
                    // Keep following the subject, with its current score.
                    _shot = candidate;
                    currentAlive = true;
                }
                else if (hasOther == false || candidate.Score > best.Score)
                {
                    best = candidate;
                    hasOther = true;
                }
            }

            if (hasOther == false)
                return;

            float held = Time.time - _shotStarted;
            bool switchNow = _hasShot == false
                || (currentAlive == false && held >= Mathf.Min(_minShotSeconds, 1.5f))
                || (held >= _minShotSeconds && best.Score > _shot.Score + _switchMargin)
                || held >= _maxShotSeconds;
            if (switchNow)
            {
                StartShot(best);
            }
        }

        /// <summary>
        /// Keeps following <paramref name="shot"/> when it is the current shot; otherwise starts it.
        /// </summary>
        private void KeepOrStart(Shot shot)
        {
            if (_hasShot && shot.Kind == _shot.Kind && shot.Subject == _shot.Subject)
            {
                _shot = shot;
            }
            else
            {
                StartShot(shot);
            }
        }

        private bool TryGetFollowShot(Frame frame, out Shot shot)
        {
            shot = default;
            if (PlayerSystem.TryGetCharacter(frame, _followed, out EntityRef character) == false)
                return false;

            shot = new Shot
            {
                Kind = ShotKind.Follow,
                Subject = character,
                Player = _followed,
                Focus = GetPosition(frame, character) + Vector3.up,
                Distance = _followDistance,
            };
            return true;
        }

        /// <summary>
        /// Lists the player slots with a character in them, in slot order.
        /// </summary>
        private void GatherPlayers(Frame frame)
        {
            _players.Clear();
            for (int player = 0; player < frame.MaxPlayerCount; player++)
            {
                if (PlayerSystem.TryGetCharacter(frame, player, out EntityRef _))
                {
                    _players.Add(player);
                }
            }
        }

        private void StartShot(Shot shot)
        {
            bool first = _hasShot == false;
            bool cut = first || Vector3.Distance(shot.Focus, _focus) > _cutDistance;

            // Look from the arena's middle side of the subject, a little off to one side, so walls stay behind it.
            Vector3 toCenter = -shot.Focus;
            toCenter.y = 0f;
            float centerYaw = toCenter.sqrMagnitude > 1f ? Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg : _yaw;
            float side = Random.value < 0.5f ? -1f : 1f;
            float cameraYaw = centerYaw + side * Random.Range(20f, 50f);

            if (cut)
            {
                _yaw = cameraYaw + 180f;
                _focus = shot.Focus;
                _focusVelocity = Vector3.zero;
                _distance = shot.Distance;
                _distanceVelocity = 0f;
                _easeStarted = -100f;
            }
            else
            {
                // Ease: blend from the current view over a moment, keeping the orbit's side.
                _easeStarted = Time.time;
                _easeFromPosition = transform.position;
                _easeFromRotation = transform.rotation;
            }

            _shot = shot;
            _hasShot = true;
            _shotStarted = Time.time;
        }

        /// <summary>
        /// Places the camera on the current shot: following its focus, drifting round it, pulled in from the walls.
        /// </summary>
        private void PlaceCamera(Shot shot)
        {
            float dt = Time.deltaTime;
            _focus = Vector3.SmoothDamp(_focus, shot.Focus, ref _focusVelocity, _followSmoothTime);
            _distance = Mathf.SmoothDamp(_distance, shot.Distance, ref _distanceVelocity, _followSmoothTime);
            _yaw = Mathf.Repeat(_yaw + _driftSpeed * dt, 360f);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 position = _focus - rotation * Vector3.forward * _distance;
            position.x = Mathf.Clamp(position.x, -_arenaHalfSize, _arenaHalfSize);
            position.z = Mathf.Clamp(position.z, -_arenaHalfSize, _arenaHalfSize);
            position.y = Mathf.Max(position.y, _minHeight);
            rotation = Quaternion.LookRotation(_focus - position, Vector3.up);

            float ease = Mathf.Clamp01((Time.time - _easeStarted) / _easeSeconds);
            if (ease < 1f)
            {
                float t = Mathf.SmoothStep(0f, 1f, ease);
                position = Vector3.Lerp(_easeFromPosition, position, t);
                rotation = Quaternion.Slerp(_easeFromRotation, rotation, t);
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// Where an entity is drawn: its view's position (interpolated between ticks), or its simulation position when
        /// it has no view.
        /// </summary>
        private Vector3 GetPosition(Frame frame, EntityRef entity)
        {
            if (_views != null)
            {
                QuantumEntityView view = _views.GetView(entity);
                if (view != null)
                    return view.transform.position;
            }

            return frame.TryGet(entity, out Transform3D transform3D) ? transform3D.Position.ToUnityVector3() : _focus;
        }

        private static readonly List<EntityRef> Expired = new List<EntityRef>();

        private static void PruneExpired(Dictionary<EntityRef, float> timers, float now)
        {
            Expired.Clear();
            foreach (KeyValuePair<EntityRef, float> timer in timers)
            {
                if (timer.Value < now)
                {
                    Expired.Add(timer.Key);
                }
            }

            foreach (EntityRef entity in Expired)
            {
                timers.Remove(entity);
            }
        }
    }
}
