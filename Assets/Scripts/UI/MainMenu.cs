using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlockSauce
{
    /// <summary>
    /// The menu screens over the demo match (docs/PLAN.md, Phase 6; Jackson's main menu, A-002): Main (Start Game, Join
    /// Game, Settings), Join (a room code, or any open room), Settings, and, in a room, Pause (resume, settings, leave),
    /// plus a connecting notice, the last error, and the room code in a corner while in a room. Mouse and controller
    /// both drive it through the EventSystem: a panel selects its first button when it opens, Escape or the gamepad's B
    /// goes back, and in a room Escape or the gamepad's Select opens and closes the pause menu (Start is the match's
    /// start button). While the menu has the screen the spectator director runs the camera.
    /// Built by Block Sauce › Build Main Menu (MainMenuBuilder); the references below are set there.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _joinPanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _statusPanel;
        [SerializeField] private GameObject _roomBadge;

        [Header("Main")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TMP_Text _errorText;

        [Header("Join")]
        [SerializeField] private TMP_InputField _codeInput;
        [SerializeField] private Button _joinCodeButton;
        [SerializeField] private Button _joinAnyButton;
        [SerializeField] private Button _joinBackButton;

        [Header("Settings")]
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private Toggle _invertYToggle;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Button _settingsBackButton;

        [Header("Pause")]
        [SerializeField] private TMP_Text _pauseCodeText;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _pauseSettingsButton;
        [SerializeField] private Button _leaveButton;

        [Header("Status")]
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _roomBadgeText;

        private enum Screen
        {
            Main,
            Join,
            Settings,
        }

        private readonly List<Resolution> _resolutions = new List<Resolution>();
        private Screen _screen;
        private bool _settingsFromPause;
        private GameFlow _flow;

        private void Awake()
        {
            _startButton.onClick.AddListener(() => _ = _flow.HostRoom());
            _joinButton.onClick.AddListener(() => Show(Screen.Join));
            _settingsButton.onClick.AddListener(() => OpenSettings(fromPause: false));
            _quitButton.onClick.AddListener(Quit);

            _joinCodeButton.onClick.AddListener(() => _ = _flow.JoinRoom(_codeInput.text));
            _joinAnyButton.onClick.AddListener(() => _ = _flow.JoinAnyRoom());
            _joinBackButton.onClick.AddListener(() => Show(Screen.Main));
            _codeInput.onValueChanged.AddListener(text =>
            {
                string upper = text.ToUpperInvariant();
                if (upper != text)
                {
                    _codeInput.SetTextWithoutNotify(upper);
                }
            });
            _codeInput.onSubmit.AddListener(code => _ = _flow.JoinRoom(code));

            _volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            _volumeSlider.onValueChanged.AddListener(value => GameSettings.Volume = value);
            _sensitivitySlider.minValue = GameSettings.MinSensitivity;
            _sensitivitySlider.maxValue = GameSettings.MaxSensitivity;
            _sensitivitySlider.SetValueWithoutNotify(GameSettings.LookSensitivity);
            _sensitivitySlider.onValueChanged.AddListener(value => GameSettings.LookSensitivity = value);
            _invertYToggle.SetIsOnWithoutNotify(GameSettings.InvertY);
            _invertYToggle.onValueChanged.AddListener(value => GameSettings.InvertY = value);
            _fullscreenToggle.onValueChanged.AddListener(value => GameSettings.Fullscreen = value);
            _resolutionDropdown.onValueChanged.AddListener(index =>
            {
                if (index >= 0 && index < _resolutions.Count)
                {
                    GameSettings.SetResolution(_resolutions[index]);
                }
            });
            _settingsBackButton.onClick.AddListener(CloseSettings);

            _resumeButton.onClick.AddListener(() => SetPaused(false));
            _pauseSettingsButton.onClick.AddListener(() => OpenSettings(fromPause: true));
            _leaveButton.onClick.AddListener(() => _ = _flow.LeaveRoom());
        }

        private void OnEnable()
        {
            _flow = GameFlow.Instance != null ? GameFlow.Instance : FindAnyObjectByType<GameFlow>();
            _flow.Changed += Refresh;
            Show(Screen.Main);
        }

        private void OnDisable()
        {
            if (_flow != null)
            {
                _flow.Changed -= Refresh;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool back = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
            bool pauseButton = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (gamepad != null && gamepad.selectButton.wasPressedThisFrame);

            if (_flow.State == GameFlow.FlowState.InRoom)
            {
                if (_flow.IsPaused == false)
                {
                    if (pauseButton)
                    {
                        SetPaused(true);
                    }
                }
                else if (_settingsPanel.activeSelf)
                {
                    if (back)
                    {
                        CloseSettings();
                    }
                }
                else if (back || (gamepad != null && gamepad.selectButton.wasPressedThisFrame))
                {
                    SetPaused(false);
                }
            }
            else if (back && _screen != Screen.Main && _codeInput.isFocused == false)
            {
                if (_screen == Screen.Settings)
                {
                    CloseSettings();
                }
                else
                {
                    Show(Screen.Main);
                }
            }

            KeepSelection();
        }

        private void SetPaused(bool paused)
        {
            _flow.IsPaused = paused;
            _settingsFromPause = false;
            _settingsPanel.SetActive(false);
            Refresh();
            if (paused)
            {
                Select(_resumeButton);
            }
        }

        private void OpenSettings(bool fromPause)
        {
            _settingsFromPause = fromPause;
            _fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            FillResolutions();
            if (fromPause)
            {
                _pausePanel.SetActive(false);
                _settingsPanel.SetActive(true);
                Select(_volumeSlider);
            }
            else
            {
                Show(Screen.Settings);
            }
        }

        private void CloseSettings()
        {
            GameSettings.Save();
            if (_settingsFromPause)
            {
                _settingsFromPause = false;
                _settingsPanel.SetActive(false);
                _pausePanel.SetActive(true);
                Select(_pauseSettingsButton);
            }
            else
            {
                Show(Screen.Main);
                Select(_settingsButton);
            }
        }

        private void Show(Screen screen)
        {
            _screen = screen;
            _flow?.ClearError();

            Refresh();
            switch (screen)
            {
                case Screen.Main:
                    Select(_startButton);
                    break;
                case Screen.Join:
                    _codeInput.text = string.Empty;
                    Select(_codeInput);
                    break;
                case Screen.Settings:
                    Select(_volumeSlider);
                    break;
            }
        }

        /// <summary>
        /// Shows what the flow's state calls for: the menu screens over the demo, nothing but the room code in a room,
        /// or the pause menu; disables the buttons while a connection is under way; points the camera.
        /// </summary>
        private void Refresh()
        {
            if (_flow == null)
                return;

            bool inRoom = _flow.State == GameFlow.FlowState.InRoom;
            bool connecting = _flow.State == GameFlow.FlowState.Connecting;
            bool paused = inRoom && _flow.IsPaused;

            _root.SetActive(inRoom == false || paused);
            _mainPanel.SetActive(inRoom == false && _screen == Screen.Main);
            _joinPanel.SetActive(inRoom == false && _screen == Screen.Join);
            if (inRoom == false || paused == false || _settingsFromPause == false)
            {
                _settingsPanel.SetActive(inRoom == false && _screen == Screen.Settings);
            }

            _pausePanel.SetActive(paused && _settingsPanel.activeSelf == false);
            _statusPanel.SetActive(connecting);
            _statusText.text = "Connecting…";

            bool idle = _flow.State == GameFlow.FlowState.Menu || _flow.State == GameFlow.FlowState.Starting;
            foreach (Button button in new[] { _startButton, _joinButton, _joinCodeButton, _joinAnyButton })
            {
                button.interactable = idle;
            }

            _errorText.text = _flow.Error ?? string.Empty;
            _errorText.gameObject.SetActive(inRoom == false && string.IsNullOrEmpty(_flow.Error) == false);

            _roomBadge.SetActive(inRoom);
            _roomBadgeText.text = inRoom ? $"ROOM  {_flow.RoomCode}" : string.Empty;
            _pauseCodeText.text = inRoom ? $"Room code: {_flow.RoomCode}" : string.Empty;

            if (inRoom == false || paused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            PointCamera(inRoom == false);
        }

        /// <summary>
        /// The spectator director has the camera while the menu shows; in a room the orbit camera follows the player.
        /// </summary>
        private static void PointCamera(bool directing)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            SpectatorDirector director = camera.GetComponent<SpectatorDirector>();
            if (director == null)
            {
                if (directing == false)
                    return;

                director = camera.gameObject.AddComponent<SpectatorDirector>();
            }

            director.IsDirecting = directing;
            if (camera.TryGetComponent(out SpectatorControls controls))
            {
                controls.enabled = false;
            }
        }

        private void FillResolutions()
        {
            _resolutions.Clear();
            _resolutions.AddRange(GameSettings.GetResolutions());
            var options = new List<string>();
            int current = 0;
            for (int i = 0; i < _resolutions.Count; i++)
            {
                options.Add($"{_resolutions[i].width} × {_resolutions[i].height}");
                if (_resolutions[i].width == UnityEngine.Screen.width && _resolutions[i].height == UnityEngine.Screen.height)
                {
                    current = i;
                }
            }

            _resolutionDropdown.ClearOptions();
            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.SetValueWithoutNotify(current);
        }

        /// <summary>
        /// Keeps a control selected while a panel shows, so a controller always has something to move from; a mouse
        /// click on empty space would otherwise leave nothing selected.
        /// </summary>
        private void KeepSelection()
        {
            EventSystem events = EventSystem.current;
            if (events == null || _root.activeInHierarchy == false)
                return;

            GameObject selected = events.currentSelectedGameObject;
            if (selected != null && selected.activeInHierarchy)
                return;

            if (_pausePanel.activeInHierarchy)
            {
                Select(_resumeButton);
            }
            else if (_settingsPanel.activeInHierarchy)
            {
                Select(_volumeSlider);
            }
            else if (_joinPanel.activeInHierarchy)
            {
                Select(_codeInput);
            }
            else if (_mainPanel.activeInHierarchy)
            {
                Select(_startButton);
            }
        }

        private static void Select(Selectable selectable)
        {
            if (EventSystem.current != null && selectable != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
