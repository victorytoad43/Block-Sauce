using Photon.Deterministic;
using Quantum;
using UnityEditor;
using UnityEngine;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Creates the bot personality assets (docs/PLAN.md, Bot Enhancement), one <see cref="BotPersonality"/> per
    /// <see cref="BotPersonalityType"/> in Folder, and lists them on BotConfig in type order. A type whose asset already
    /// exists is left as it is (tune them in the Inspector); only missing ones are made, with the first-guess numbers
    /// below. Klutz chooses like the bot before personalities (its mistakes come separately).
    /// </summary>
    public static class BotPersonalityBuilder
    {
        private const string Folder = "Assets/QuantumUser/Resources/Configs/Bots";
        private const string BotConfigPath = "Assets/QuantumUser/Resources/Configs/BotConfig.asset";

        [MenuItem("Block Sauce/Build Bot Personalities")]
        public static void Build()
        {
            if (AssetDatabase.IsValidFolder(Folder) == false)
            {
                System.IO.Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
            }

            string[] names = System.Enum.GetNames(typeof(BotPersonalityType));
            var personalities = new AssetRef<BotPersonality>[names.Length];
            int created = 0;
            for (int type = 0; type < names.Length; type++)
            {
                string path = $"{Folder}/BotPersonality_{names[type]}.asset";
                BotPersonality personality = AssetDatabase.LoadAssetAtPath<BotPersonality>(path);
                if (personality == null)
                {
                    personality = ScriptableObject.CreateInstance<BotPersonality>();
                    SetFirstGuess(personality, (BotPersonalityType)type);
                    AssetDatabase.CreateAsset(personality, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    personality = AssetDatabase.LoadAssetAtPath<BotPersonality>(path);
                    created++;
                }

                personalities[type] = new AssetRef<BotPersonality>(personality.Guid);
            }

            BotConfig config = AssetDatabase.LoadAssetAtPath<BotConfig>(BotConfigPath);
            config.Personalities = personalities;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Quantum.Editor.QuantumUnityDBUtilities.RefreshGlobalDB(true);
            Debug.Log($"Bot personalities: {created} created, {personalities.Length} listed on BotConfig.");
        }

        /// <summary>
        /// Wrecker: raids from a rival's first counted block, now and then even with its own blocks loose (mostly once a
        /// rival has two), throws at carriers 30% of the time, tackles anyone carrying a block and rivals at their towers
        /// (60% of the chances), dunks less. Builder: raids only a rival about to finish and only with nothing of its own
        /// to fetch, clears its top first, dunks more, guards its tower (anyone within 5 m, 80% of the chances).
        /// Klutz: chooses like the bot before personalities, and makes mistakes: trips about once in 30 s of running,
        /// slides without looking once in 25 s, drops its block once in 30 s of carrying, lets go well off center on a
        /// quarter of its placings and throws wild on 30% of its throws.
        /// </summary>
        private static void SetFirstGuess(BotPersonality personality, BotPersonalityType type)
        {
            switch (type)
            {
                case BotPersonalityType.Wrecker:
                    personality.BuildWeight = 1;
                    personality.RaidWeight = (FP)7 / 5;
                    personality.ClearTopWeight = FP._0_50;
                    personality.RaidAtCount = 1;
                    personality.ThrowAtCarrierChance = (FP)3 / 10;
                    personality.DunkChanceScale = FP._0_50;
                    personality.TackleCarriers = 2;
                    personality.TackleBuilders = true;
                    personality.TackleChance = (FP)6 / 10;
                    break;
                case BotPersonalityType.Builder:
                    personality.BuildWeight = 1;
                    personality.RaidWeight = FP._0_50;
                    personality.ClearTopWeight = 2;
                    personality.RaidOnlyNearWin = true;
                    personality.DunkChanceScale = 2;
                    personality.GuardRadius = 5;
                    personality.GuardEmptyHanded = true;
                    personality.TackleChance = (FP)8 / 10;
                    break;
                case BotPersonalityType.Klutz:
                    personality.TripChance = (FP)3 / 100;
                    personality.WildSlideChance = (FP)4 / 100;
                    personality.DropChance = (FP)3 / 100;
                    personality.OffCenterChance = FP._0_25;
                    personality.WildThrowChance = (FP)3 / 10;
                    break;
            }
        }
    }
}
