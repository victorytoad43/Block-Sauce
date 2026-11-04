using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockSauce
{
    /// <summary>
    /// Dev shortcuts (editor and development builds only; CLAUDE.md › Testing): F5 spectator director, F6 all-bot match,
    /// F7 / Shift+F7 add or remove a bot in the lobby, F8 / Shift+F8 sauce, F9 stack onto your tower, F10 knock yourself
    /// down. Ignored while a menu has the screen. They send commands, the only way the view talks to the simulation
    /// besides input.
    /// </summary>
    public class DevKeys : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            QuantumGame game = QuantumRunner.Default?.Game;
            Frame frame = game?.Frames.Predicted;
            Keyboard keyboard = Keyboard.current;
            if (frame == null || keyboard == null || frame.TryGetSingleton(out Match match) == false
                || GameFlow.AllowsGameInput == false)
                return;

            // Drop your next block onto your tower.
            if (match.State == MatchState.Playing && keyboard.f9Key.wasPressedThisFrame)
            {
                game.AddCommand(new CommandDevStackTower());
            }

            // Coat your held block, or all your blocks, with Stack sauce (F8) or Slip sauce (Shift+F8).
            if (keyboard.f8Key.wasPressedThisFrame)
            {
                game.AddCommand(new CommandDevSauce { Type = keyboard.shiftKey.isPressed ? SauceType.Slip : SauceType.Stack });
            }

            // Add a bot in the lobby (F7), or remove one (Shift+F7); the room screen has buttons for both.
            if (match.State == MatchState.Lobby && keyboard.f7Key.wasPressedThisFrame)
            {
                game.AddCommand(keyboard.shiftKey.isPressed ? new CommandRemoveBot() : new CommandAddBot());
            }

            // An all-bot match. In the lobby, F6 hands your character to a bot and starts the match (bots fill the
            // other slots); you watch your bot. F6 again, at any time, takes your character back.
            if (keyboard.f6Key.wasPressedThisFrame)
            {
                bool startMatch = match.State == MatchState.Lobby && IsLocalCharacterBot(frame, game) == false;
                game.AddCommand(new CommandDevBotSelf());
                if (startMatch)
                {
                    game.AddCommand(new CommandStartMatch());
                }
            }

            // F5 switches the main camera to the spectator director, with its controls, and back.
            if (keyboard.f5Key.wasPressedThisFrame && Camera.main != null)
            {
                SpectatorDirector director = Camera.main.GetComponent<SpectatorDirector>();
                if (director == null)
                {
                    Camera.main.gameObject.AddComponent<SpectatorDirector>();
                    Camera.main.gameObject.AddComponent<SpectatorControls>();
                }
                else
                {
                    director.IsDirecting = director.IsDirecting == false;
                    SpectatorControls controls = Camera.main.GetComponent<SpectatorControls>();
                    if (controls == null)
                    {
                        controls = Camera.main.gameObject.AddComponent<SpectatorControls>();
                    }

                    controls.enabled = director.IsDirecting;
                }
            }

            // Knock yourself down.
            if (keyboard.f10Key.wasPressedThisFrame)
            {
                game.AddCommand(new CommandDevKnockdown());
            }
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
#endif
    }
}
