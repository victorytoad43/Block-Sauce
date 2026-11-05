using Quantum;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace BlockSauce
{
    /// <summary>
    /// Plays the game's sounds from simulation events (view only): character and block sounds where they happen, match
    /// moments flat, a click on every button, and music for the menu and for rooms. Clips come from a
    /// <see cref="SoundBank"/>; the master volume is the settings' volume (<see cref="GameSettings"/>, through the
    /// listener).
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        [SerializeField] private SoundBank _bank;
        [Tooltip("How many sounds can play at once; the oldest is cut off past that.")]
        [SerializeField] private int _voices = 16;
        [Tooltip("How much world sounds fade with distance: 0 is flat everywhere, 1 is fully 3D.")]
        [Range(0f, 1f)]
        [SerializeField] private float _spatialBlend = 0.6f;
        [Tooltip("The impact speed, in m/s, at which an impact plays at full volume.")]
        [SerializeField] private float _loudImpactSpeed = 8f;

        private AudioSource[] _sources;
        private int _next;
        private AudioSource _music;

        private void Awake()
        {
            _sources = new AudioSource[_voices];
            for (int i = 0; i < _voices; i++)
            {
                var voice = new GameObject($"Voice{i}").AddComponent<AudioSource>();
                voice.transform.SetParent(transform, false);
                voice.playOnAwake = false;
                voice.minDistance = 6f;
                voice.maxDistance = 60f;
                _sources[i] = voice;
            }

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
        }

        private void Start()
        {
            AudioListener.volume = GameSettings.Volume;
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                button.onClick.AddListener(() => PlayFlat(_bank.Click, 0.6f));
            }
        }

        private void OnEnable()
        {
            if (_bank == null)
                return;

            QuantumEvent.Subscribe(this, (EventJumped e) => PlayAt(_bank.Jump, e.Game, e.Entity, 0.5f));
            QuantumEvent.Subscribe(this, (EventKnockedBack e) => PlayAt(_bank.Knockdown, e.Game, e.Entity, 1f));
            QuantumEvent.Subscribe(this, (EventFallRespawned e) => PlayAt(_bank.FallRespawn, e.Game, e.Entity, 0.7f));
            QuantumEvent.Subscribe(this, (EventGrabbed e) => PlayAt(_bank.Grab, e.Game, e.Held, 0.6f));
            QuantumEvent.Subscribe(this, (EventPlaced e) => PlayAt(_bank.Place, e.Game, e.Held, 0.7f));
            QuantumEvent.Subscribe(this, (EventDropped e) => PlayAt(_bank.Drop, e.Game, e.Held, 0.5f));
            QuantumEvent.Subscribe(this, (EventThrown e) => PlayAt(_bank.Throw, e.Game, e.Held, 0.8f));
            QuantumEvent.Subscribe(this, (EventObjectImpact e) =>
                Play(_bank.Impact, e.Position.ToUnityVector3(), Mathf.Clamp01(e.Speed.AsFloat / _loudImpactSpeed)));
            QuantumEvent.Subscribe(this, (EventBlockSettleStarted e) => PlayAt(_bank.SettleStarted, e.Game, e.Block, 0.4f));
            QuantumEvent.Subscribe(this, (EventBlockCounted e) => PlayAt(_bank.Counted, e.Game, e.Block, 0.9f));
            QuantumEvent.Subscribe(this, (EventBlockUncounted e) => PlayAt(_bank.Uncounted, e.Game, e.Block, 0.8f));
            QuantumEvent.Subscribe(this, (EventMatchStateChanged e) => OnMatchState(e.State));
        }

        private void OnDisable()
        {
            QuantumEvent.UnsubscribeListener(this);
        }

        private void Update()
        {
            if (_bank == null || GameFlow.Instance == null)
                return;

            AudioClip wanted = GameFlow.Instance.State == GameFlow.FlowState.InRoom ? _bank.MatchMusic : _bank.MenuMusic;
            if (_music.clip != wanted)
            {
                _music.clip = wanted;
                _music.volume = _bank.MusicVolume;
                if (wanted != null)
                {
                    _music.Play();
                }
            }
        }

        private void OnMatchState(MatchState state)
        {
            switch (state)
            {
                case MatchState.Countdown: PlayFlat(_bank.Countdown, 0.8f); break;
                case MatchState.Playing: PlayFlat(_bank.MatchStart, 0.9f); break;
                case MatchState.RoundEnd: PlayFlat(_bank.RoundEnd, 1f); break;
                case MatchState.Results: PlayFlat(_bank.Results, 0.7f); break;
            }
        }

        private void PlayAt(AudioClip clip, QuantumGame game, EntityRef entity, float volume)
        {
            Frame frame = game?.Frames.Predicted;
            if (clip == null || frame == null || frame.TryGet(entity, out Transform3D transform) == false)
                return;

            Play(clip, transform.Position.ToUnityVector3(), volume);
        }

        private void Play(AudioClip clip, Vector3 position, float volume)
        {
            AudioSource voice = NextVoice(clip);
            if (voice == null)
                return;

            voice.transform.position = position;
            voice.spatialBlend = _spatialBlend;
            voice.volume = volume;
            voice.Play();
        }

        private void PlayFlat(AudioClip clip, float volume)
        {
            AudioSource voice = NextVoice(clip);
            if (voice == null)
                return;

            voice.spatialBlend = 0f;
            voice.volume = volume;
            voice.Play();
        }

        private AudioSource NextVoice(AudioClip clip)
        {
            if (clip == null || _sources == null)
                return null;

            AudioSource voice = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            voice.Stop();
            voice.clip = clip;
            voice.pitch = Random.Range(0.95f, 1.05f);
            return voice;
        }
    }
}
