using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Block Sauce › Build Sound Bank: copies the placeholder sounds picked from Jackson's CC0 pack
    /// (<c>Assets/Art/Audio/_Pack_FreeSFX</c>, not in git) to <c>Assets/Art/Audio/SFX_*.wav</c> and <c>MUS_*.wav</c>,
    /// fills <c>PH_SoundBank</c> with them, and puts a <see cref="GameAudio"/> in the Arena scene. The picks are by file
    /// name only; change a line here (or the bank asset) to swap one.
    /// </summary>
    public static class SoundBankBuilder
    {
        private const string PackFolder = "Assets/Art/Audio/_Pack_FreeSFX/GameSFX";
        private const string AudioFolder = "Assets/Art/Audio";
        private const string BankPath = "Assets/Placeholders/PH_SoundBank.asset";
        private const string ArenaPath = "Assets/Scenes/Arena.unity";

        private static readonly (string field, string source, string name)[] Picks =
        {
            ("Jump", "Bounce Jump/Retro Jump Simple A 01.wav", "SFX_Jump"),
            ("Knockdown", "Impact/Retro Impact Punch Hurt 01.wav", "SFX_Knockdown"),
            ("FallRespawn", "Magic/Retro Magic 06.wav", "SFX_FallRespawn"),
            ("Grab", "PickUp/Retro PickUp 10.wav", "SFX_Grab"),
            ("Place", "Blops/Retro Blop 07.wav", "SFX_Place"),
            ("Drop", "Blops/Retro Blop 18.wav", "SFX_Drop"),
            ("Throw", "Swoosh/Retro Swooosh 07.wav", "SFX_Throw"),
            ("Impact", "Impact/Retro Impact Punch 07.wav", "SFX_Impact"),
            ("SettleStarted", "Ascending/Retro Ascending Short 20.wav", "SFX_SettleStarted"),
            ("Counted", "PickUp/Retro PickUp Coin 04.wav", "SFX_Counted"),
            ("Uncounted", "Events/Negative/Retro Negative Short 07.wav", "SFX_Uncounted"),
            ("Countdown", "Alarms Blip Beeps/Retro Beeep 06.wav", "SFX_Countdown"),
            ("MatchStart", "Events/Retro Event StereoUP 02.wav", "SFX_MatchStart"),
            ("RoundEnd", "Music/Success/Retro Success Melody 04 - electric piano 2.wav", "SFX_RoundEnd"),
            ("Results", "Events/Retro Event UI 15.wav", "SFX_Results"),
            ("Click", "Events/Retro Event UI 13.wav", "SFX_Click"),
            ("MenuMusic", "Music/Nes Style/Retro Music Loop - PV8 - NES Style 01.wav", "MUS_Menu"),
            ("MatchMusic", "Music/ChipWave/Retro Music - ABMU - ChipWave 01.wav", "MUS_Match"),
        };

        [MenuItem("Block Sauce/Build Sound Bank")]
        public static void Build()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }

            var missing = new List<string>();
            var serialized = new SerializedObject(bank);
            foreach ((string field, string source, string name) in Picks)
            {
                string from = $"{PackFolder}/{source}";
                string to = $"{AudioFolder}/{name}.wav";
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(to) == null)
                {
                    if (File.Exists(from) == false)
                    {
                        missing.Add(source);
                        continue;
                    }

                    AssetDatabase.CopyAsset(from, to);
                }

                serialized.FindProperty(field).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(to);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene(ArenaPath);
            // Not ??: Unity's missing objects aren't C# null in the editor.
            GameObject host = GameObject.Find("GameAudio");
            if (host == null)
            {
                host = new GameObject("GameAudio");
            }

            GameAudio audio = host.GetComponent<GameAudio>();
            if (audio == null)
            {
                audio = host.AddComponent<GameAudio>();
            }

            var audioSerialized = new SerializedObject(audio);
            audioSerialized.FindProperty("_bank").objectReferenceValue = bank;
            audioSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(missing.Count == 0 ? "Built the sound bank." : "Sound bank: missing " + string.Join(", ", missing));
        }
    }
}
