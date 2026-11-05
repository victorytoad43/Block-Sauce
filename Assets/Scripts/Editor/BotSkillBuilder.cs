using Photon.Deterministic;
using Quantum;
using UnityEditor;
using UnityEngine;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Creates the bot difficulty assets (docs/PLAN.md, Bot Enhancement), one <see cref="BotSkill"/> per
    /// <see cref="BotSkillLevel"/> in SkillFolder, and lists them on BotConfig in level order. A level whose asset already
    /// exists is left as it is (tune them in the Inspector); only missing ones are made, with the first-guess numbers
    /// below. Normal is the Phase 6 bot's numbers.
    /// </summary>
    public static class BotSkillBuilder
    {
        private const string SkillFolder = "Assets/QuantumUser/Resources/Configs/Bots";
        private const string BotConfigPath = "Assets/QuantumUser/Resources/Configs/BotConfig.asset";

        [MenuItem("Block Sauce/Build Bot Skills")]
        public static void Build()
        {
            if (AssetDatabase.IsValidFolder(SkillFolder) == false)
            {
                System.IO.Directory.CreateDirectory(SkillFolder);
                AssetDatabase.Refresh();
            }

            string[] names = System.Enum.GetNames(typeof(BotSkillLevel));
            var skills = new AssetRef<BotSkill>[names.Length];
            int created = 0;
            for (int level = 0; level < names.Length; level++)
            {
                string path = $"{SkillFolder}/BotSkill_{names[level]}.asset";
                BotSkill skill = AssetDatabase.LoadAssetAtPath<BotSkill>(path);
                if (skill == null)
                {
                    skill = ScriptableObject.CreateInstance<BotSkill>();
                    SetFirstGuess(skill, (BotSkillLevel)level);
                    AssetDatabase.CreateAsset(skill, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    skill = AssetDatabase.LoadAssetAtPath<BotSkill>(path);
                    created++;
                }

                skills[level] = new AssetRef<BotSkill>(skill.Guid);
            }

            BotConfig config = AssetDatabase.LoadAssetAtPath<BotConfig>(BotConfigPath);
            config.Skills = skills;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Quantum.Editor.QuantumUnityDBUtilities.RefreshGlobalDB(true);
            Debug.Log($"Bot skills: {created} created, {skills.Length} listed on BotConfig.");
        }

        /// <summary>
        /// The first-guess numbers for a level. Easy hesitates, aims and places sloppily, slides early and wide at where
        /// a rival is; Hard and Expert react faster, aim tighter, run with a block and keep trying longer.
        /// </summary>
        private static void SetFirstGuess(BotSkill skill, BotSkillLevel level)
        {
            switch (level)
            {
                case BotSkillLevel.Easy:
                    skill.ThinkSecondsMin = (FP)8 / 10;
                    skill.ThinkSecondsMax = 2;
                    skill.RaidAtCount = 3;
                    skill.RunWhileCarrying = false;
                    skill.GrabAttempts = 4;
                    skill.PlaceAttempts = 2;
                    skill.PlaceError = (FP)25 / 100;
                    skill.AimTolerance = 4;
                    skill.MinAimSeconds = 1;
                    skill.AimError = 6;
                    skill.TackleSlideDistance = 5;
                    skill.TackleAlignDegrees = 35;
                    skill.TackleLead = 0;
                    break;
                case BotSkillLevel.Normal:
                    // The defaults on BotSkill are the Phase 6 bot's.
                    break;
                case BotSkillLevel.Hard:
                    skill.ThinkSecondsMin = (FP)2 / 10;
                    skill.ThinkSecondsMax = (FP)6 / 10;
                    skill.RunWhileCarrying = true;
                    skill.GrabAttempts = 8;
                    skill.PlaceAttempts = 4;
                    skill.AimTolerance = (FP)3 / 2;
                    skill.MinAimSeconds = (FP)4 / 10;
                    skill.TackleAlignDegrees = 15;
                    break;
                case BotSkillLevel.Expert:
                    skill.ThinkSecondsMin = (FP)1 / 10;
                    skill.ThinkSecondsMax = (FP)3 / 10;
                    skill.RunWhileCarrying = true;
                    skill.GrabAttempts = 10;
                    skill.PlaceAttempts = 5;
                    skill.AimTolerance = 1;
                    skill.MinAimSeconds = (FP)3 / 10;
                    skill.TackleSlideDistance = (FP)32 / 10;
                    skill.TackleAlignDegrees = 10;
                    break;
            }
        }
    }
}
