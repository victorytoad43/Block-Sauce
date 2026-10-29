using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockSauce
{
    /// <summary>
    /// Quantum's start menu with one change: while a game runs, only Escape toggles the menu. The SDK version also
    /// toggles it on the gamepad's Start button, which Block Sauce uses to start a match (see MatchHud). Kept as a
    /// subclass rather than an SDK edit, so SDK updates don't undo it.
    /// </summary>
    public class BlockSauceStartUI : QuantumStartUI
    {
        protected override void Update()
        {
            switch (CurrentState)
            {
                case State.Running:
                    if (ToggleUnityCursor)
                    {
                        bool menuOpen = UI.PanelGroup.gameObject.activeSelf;
                        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
                        Cursor.visible = menuOpen;
                    }

                    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    {
                        OnTogglePressed();
                    }

                    if (UI.SignalGroup != null && UI.SignalGroup.activeSelf)
                    {
                        int activeIndex = Connection.Ping > 0
                            ? Mathf.Min((Connection.Ping / PingDelta) + 1, UI.SignalIcons.Count - 1)
                            : 0;
                        for (int i = 0; i < UI.SignalIcons.Count; i++)
                        {
                            UI.SignalIcons[i].SetActive(i == activeIndex);
                        }
                    }

                    if (UI.FpsGroup != null && UI.FpsGroup.activeSelf)
                    {
                        UI.SignalText.text = Connection.Ping.ToString();
                    }
                    break;

                case State.Idle:
                    if (UI.RoomPrivateToggleGroup != null && UI.PrivateRoomInput.interactable)
                    {
                        if (UI.RoomNameInput.text.Length > 0 && UI.RoomPrivateToggleGroup.activeSelf == false)
                        {
                            UI.PrivateRoomInput.isOn = true;
                        }

                        UI.RoomPrivateToggleGroup.SetActive(UI.RoomNameInput.text.Length > 0);
                    }
                    break;
            }
        }
    }
}
