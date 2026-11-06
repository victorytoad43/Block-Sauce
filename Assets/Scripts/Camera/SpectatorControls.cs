using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockSauce
{
    /// <summary>
    /// Placeholder spectator controls (IMGUI, until the room screen and HUD art, A-006): a bar along the bottom of the
    /// screen while the <see cref="SpectatorDirector"/> on the same camera is directing, showing what the camera is on,
    /// with buttons to follow the previous or next player, hold the overview, or hand back to the director (Auto). The
    /// arrow keys and the gamepad's d-pad do the same: left and right follow the previous or next player, up the
    /// overview, down Auto. The menu's background match uses the director without these controls.
    /// </summary>
    [RequireComponent(typeof(SpectatorDirector))]
    public class SpectatorControls : MonoBehaviour
    {
        private SpectatorDirector _director;
        private GUIStyle _label;
        private GUIStyle _button;
        private GUIStyle _hint;
        private Texture2D _panel;

        private void Awake()
        {
            _director = GetComponent<SpectatorDirector>();
        }

        private void OnEnable()
        {
            // The buttons need the mouse.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (_director.IsDirecting == false)
                return;

            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            if (WasPressed(keyboard?.leftArrowKey, gamepad?.dpad.left))
            {
                _director.FollowNext(-1);
            }
            else if (WasPressed(keyboard?.rightArrowKey, gamepad?.dpad.right))
            {
                _director.FollowNext(1);
            }
            else if (WasPressed(keyboard?.upArrowKey, gamepad?.dpad.up))
            {
                _director.ShowOverview();
            }
            else if (WasPressed(keyboard?.downArrowKey, gamepad?.dpad.down))
            {
                _director.SetAuto();
            }
        }

        private void OnGUI()
        {
            if (_director.IsDirecting == false)
                return;

            EnsureStyles();

            const float width = 640f;
            const float height = 78f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 16f, width, height);
            GUI.DrawTexture(panel, _panel);

            float x = panel.x + 10f;
            float y = panel.y + 10f;
            if (GUI.Button(new Rect(x, y, 44f, 36f), "◀", _button))
            {
                _director.FollowNext(-1);
            }

            // Dev builds add what the shown bot is (personality, skill); players never see it.
            GUI.Label(new Rect(x + 50f, y, 300f, 36f), _director.Describe() + BotDevInfo.Describe(_director.ShownPlayer),
                _label);

            if (GUI.Button(new Rect(x + 356f, y, 44f, 36f), "▶", _button))
            {
                _director.FollowNext(1);
            }

            DrawModeButton(new Rect(x + 410f, y, 104f, 36f), "Overview", SpectatorDirector.ViewMode.Overview,
                _director.ShowOverview);
            DrawModeButton(new Rect(x + 520f, y, 100f, 36f), "Auto", SpectatorDirector.ViewMode.Auto,
                _director.SetAuto);

            GUI.Label(new Rect(panel.x, y + 40f, width, 20f),
                "← → follow a player   ↑ overview   ↓ auto   (d-pad on a gamepad)", _hint);
        }

        private void DrawModeButton(Rect rect, string text, SpectatorDirector.ViewMode mode, System.Action select)
        {
            bool active = _director.Mode == mode;
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(1f, 0.75f, 0.3f) : previous;
            if (GUI.Button(rect, text, _button))
            {
                select();
            }

            GUI.backgroundColor = previous;
        }

        private static bool WasPressed(UnityEngine.InputSystem.Controls.KeyControl key,
            UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            return (key != null && key.wasPressedThisFrame) || (button != null && button.wasPressedThisFrame);
        }

        private void EnsureStyles()
        {
            if (_label != null)
                return;

            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            _hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.7f) },
            };
            _panel = new Texture2D(1, 1);
            _panel.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            _panel.Apply();
        }
    }
}
