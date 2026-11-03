using System;
using System.Collections.Generic;
using Quantum;
using UnityEditor;
using UnityEngine;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Builds the bots' brain (docs/PLAN.md, Phase 6) as Bot SDK state machine assets in BrainFolder and points
    /// BotConfig at it. The graph is kept here, in code, because the SDK's visual editor has no way to create its
    /// documents from a script; the actions and decisions it uses (BotBrain.cs) show up in the visual editor too, so the
    /// graph can be redrawn there. Rerunning it rebuilds the assets in place, keeping their GUIDs.
    ///
    /// Waiting (outside a match) → Fetching → Using (place, throw, dunk or set down) → Fetching ...; a failed task
    /// waits a second in Pausing; any state goes back to Waiting when the match stops.
    /// </summary>
    public static class BotBrainBuilder
    {
        private const string BrainFolder = "Assets/QuantumUser/Resources/Bots/Brain";
        private const string BotConfigPath = "Assets/QuantumUser/Resources/Configs/BotConfig.asset";

        private enum Holding
        {
            Either = 0,
            Yes = 1,
            No = 2,
        }

        [MenuItem("Block Sauce/Build Bot Brain")]
        public static void Build()
        {
            if (AssetDatabase.IsValidFolder(BrainFolder) == false)
            {
                System.IO.Directory.CreateDirectory(BrainFolder);
                AssetDatabase.Refresh();
            }

            BotStopAction stop = Make<BotStopAction>("Action_Stop", a => a.Label = "Stop");
            BotFetchAction fetch = Make<BotFetchAction>("Action_Fetch", a => a.Label = "Fetch");
            BotUseBlockAction useBlock = Make<BotUseBlockAction>("Action_UseBlock", a => a.Label = "Use block");

            BotMatchPlayingDecision playing = Make<BotMatchPlayingDecision>("Decision_Playing", d => { d.Label = "Playing"; d.Invert = false; });
            BotMatchPlayingDecision notPlaying = Make<BotMatchPlayingDecision>("Decision_NotPlaying", d => { d.Label = "Not playing"; d.Invert = true; });
            BotTaskEndedDecision doneHolding = Ended("Decision_DoneHolding", BotTaskStatus.Done, Holding.Yes);
            BotTaskEndedDecision doneEmpty = Ended("Decision_DoneEmpty", BotTaskStatus.Done, Holding.No);
            BotTaskEndedDecision failed = Ended("Decision_Failed", BotTaskStatus.Failed, Holding.Either);
            BotWaitedDecision waitedHolding = Waited("Decision_WaitedHolding", Holding.Yes);
            BotWaitedDecision waitedEmpty = Waited("Decision_WaitedEmpty", Holding.No);

            HFSMState waiting = Make<HFSMState>("State_Waiting", s => { });
            HFSMState fetching = Make<HFSMState>("State_Fetching", s => { });
            HFSMState usingBlock = Make<HFSMState>("State_Using", s => { });
            HFSMState pausing = Make<HFSMState>("State_Pausing", s => { });
            HFSMRoot root = Make<HFSMRoot>("BotBrain", r => { });

            // Quantum gives each asset its GUID on import; link them only once every one has one.
            AssetDatabase.SaveAssets();

            Fill(waiting, "Waiting", 0, stop,
                (playing, fetching));
            Fill(fetching, "Fetching", 1, fetch,
                (notPlaying, waiting), (doneHolding, usingBlock), (failed, pausing), (doneEmpty, pausing));
            Fill(usingBlock, "Using", 2, useBlock,
                (notPlaying, waiting), (failed, pausing), (doneHolding, usingBlock), (doneEmpty, fetching));
            Fill(pausing, "Pausing", 3, stop,
                (notPlaying, waiting), (waitedHolding, usingBlock), (waitedEmpty, fetching));

            root.Label = "Bot brain";
            root.StatesLinks = new[] { Ref(waiting), Ref(fetching), Ref(usingBlock), Ref(pausing) };
            root.EventsNames = Array.Empty<string>();
            EditorUtility.SetDirty(root);

            BotConfig config = AssetDatabase.LoadAssetAtPath<BotConfig>(BotConfigPath);
            config.Brain = new AssetRef<HFSMRoot>(root.Guid);
            EditorUtility.SetDirty(config);

            AssetDatabase.SaveAssets();
            Quantum.Editor.QuantumUnityDBUtilities.RefreshGlobalDB(true);
            Debug.Log($"Built the bot brain in {BrainFolder} ({root.StatesLinks.Length} states) and set it on BotConfig.");
        }

        private static BotTaskEndedDecision Ended(string name, BotTaskStatus status, Holding holding)
        {
            return Make<BotTaskEndedDecision>(name, d =>
            {
                d.Label = $"Task {status}, holding {holding}";
                d.Status = status;
                d.RequireHolding = (int)holding;
            });
        }

        private static BotWaitedDecision Waited(string name, Holding holding)
        {
            return Make<BotWaitedDecision>(name, d =>
            {
                d.Label = $"Waited, holding {holding}";
                d.Seconds = 1;
                d.RequireHolding = (int)holding;
            });
        }

        /// <summary>
        /// Sets up a top-level state: its index, its one on-enter action, and its transitions, checked in order.
        /// </summary>
        private static void Fill(HFSMState state, string label, byte index, AIAction onEnter,
            params (HFSMDecision decision, HFSMState target)[] transitions)
        {
            state.Label = label;
            state.Index = index;
            state.Level = 0;
            state.OnEnterLinks = new[] { new AssetRef<AIAction>(onEnter.Guid) };
            state.OnUpdateLinks = Array.Empty<AssetRef<AIAction>>();
            state.OnExitLinks = Array.Empty<AssetRef<AIAction>>();
            state.ChildrenLinks = Array.Empty<AssetRef<HFSMState>>();
            state.ParentLink = default;

            var list = new List<HFSMTransition>();
            foreach ((HFSMDecision decision, HFSMState target) in transitions)
            {
                list.Add(new HFSMTransition
                {
                    Id = $"{label} → {target.name.Replace("State_", "")} ({decision.Label})",
                    DecisionLink = new AssetRef<HFSMDecision>(decision.Guid),
                    StateLink = Ref(target),
                });
            }

            state.Transitions = list.ToArray();
            EditorUtility.SetDirty(state);
        }

        private static AssetRef<HFSMState> Ref(HFSMState state)
        {
            return new AssetRef<HFSMState>(state.Guid);
        }

        /// <summary>
        /// Loads the asset of type T at BrainFolder/name, or creates it, applies <paramref name="setup"/> and imports it
        /// so it has its Quantum GUID. An asset there that isn't a T, or has lost its script (Unity only finds a class's
        /// script in a file of the class's own name), is replaced.
        /// </summary>
        private static T Make<T>(string name, Action<T> setup) where T : AssetObject
        {
            string path = $"{BrainFolder}/{name}.asset";
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (System.IO.File.Exists(path) && (asset == null || MonoScript.FromScriptableObject(asset) == null))
            {
                AssetDatabase.DeleteAsset(path);
                asset = null;
            }

            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<T>();
            }

            setup(asset);
            if (created)
            {
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
