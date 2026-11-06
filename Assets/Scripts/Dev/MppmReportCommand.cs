#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Quantum;
using TMPro;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// Editor test tool for Multiplayer Play Mode: the main editor sends it to its clones, and each clone writes what its
    /// screen shows (flow state, room screen rows, host controls, HUD) to its own Editor.log, and can save a screenshot.
    /// Send it from an eval with <c>QuantumMppm.MainEditor.Send(new MppmReportCommand { ... })</c>.
    /// </summary>
    [Serializable]
    public class MppmReportCommand : QuantumMppmCommand
    {
        public string Label;
        public string ScreenshotPath;

        public override Task ExecuteAsync()
        {
            var text = new StringBuilder($"[MPPM report {Label}] ");
            GameFlow flow = GameFlow.Instance;
            text.Append(flow != null ? $"flow {flow.State} room {flow.RoomCode} paused {flow.IsPaused} error '{flow.Error}'" : "no flow");

            QuantumGame game = QuantumRunner.Default?.Game;
            Frame frame = game?.Frames.Predicted;
            if (frame != null && frame.TryGetSingleton(out Match match))
            {
                text.Append($" | match {match.State}");
                foreach (PlayerRef local in game.GetLocalPlayers())
                {
                    text.Append($" local slot {(int)local}");
                }
            }

            RoomScreen room = UnityEngine.Object.FindAnyObjectByType<RoomScreen>();
            if (room != null)
            {
                text.Append(" | room screen:");
                foreach (RoomScreen.Row row in (RoomScreen.Row[])Field(room, "_rows"))
                {
                    if (row.Root.activeSelf)
                    {
                        string skill = row.Skill != null && row.Skill.activeSelf
                            ? $" {row.SkillText.text}{(row.SkillDown.gameObject.activeSelf ? " with arrows" : string.Empty)}"
                            : string.Empty;
                        text.Append($" [{row.Name.text}{(row.Tag.text.Length > 0 ? " " + row.Tag.text : string.Empty)}{skill}]");
                    }
                }

                text.Append($" start shown {((Component)Field(room, "_startButton")).gameObject.activeInHierarchy}");
                text.Append($" waiting shown {((Component)Field(room, "_waitingText")).gameObject.activeInHierarchy}");
            }
            else
            {
                text.Append(" | room screen hidden");
            }

            MatchHud hud = UnityEngine.Object.FindAnyObjectByType<MatchHud>();
            if (hud != null && ((GameObject)Field(hud, "_root")).activeSelf)
            {
                var prompt = (TMP_Text)Field(hud, "_promptText");
                var big = (TMP_Text)Field(hud, "_bigText");
                text.Append($" | hud prompt '{(prompt.isActiveAndEnabled ? prompt.text : string.Empty)}' big '{(big.isActiveAndEnabled ? big.text : string.Empty)}'");
            }

            Debug.Log(text.ToString());
            if (string.IsNullOrEmpty(ScreenshotPath) == false)
            {
                ScreenCapture.CaptureScreenshot(ScreenshotPath);
            }

            return Task.CompletedTask;
        }

        private static object Field(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        }
    }
}
#endif
