namespace Quantum.BotSDK.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(AIConfig), true)]
    public class AIConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var defaultConfig = serializedObject.FindProperty("DefaultConfig");
            var referencedGuid = defaultConfig.FindPropertyRelative("Id").FindPropertyRelative("Value").longValue;
            var myGuid = serializedObject.FindProperty("Identifier").FindPropertyRelative("Guid").FindPropertyRelative("Value").longValue;

            if (IsDefaultConfig() == true)
            {
                DrawPropertiesExcluding(serializedObject, "DefaultConfig");
            }
            else
            {
                DrawDefaultInspector();
            }

            if (defaultConfig != null && IsDefaultConfig() == false)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Update Config") == true)
                {
                    Undo.RecordObject(serializedObject.targetObject, "Updated Config Values");
                    UpdateConfig();
                }

                if (GUILayout.Button("Reset to Default") == true)
                {
                    Undo.RecordObject(serializedObject.targetObject, "Reset Config to Default");
                    ResetConfig();
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool IsDefaultConfig()
        {
            var defaultConfig = serializedObject.FindProperty("DefaultConfig");
            var referencedGuid = defaultConfig.FindPropertyRelative("Id").FindPropertyRelative("Value").longValue;
            var myGuid = serializedObject.FindProperty("Identifier").FindPropertyRelative("Guid").FindPropertyRelative("Value").longValue;

            return referencedGuid == myGuid;
        }

        private void UpdateConfig()
        {
            var configAsset = serializedObject.targetObject as AIConfig;
            var configValues = configAsset.KeyValuePairs;

            var defaultConfigGUID = serializedObject.FindProperty("DefaultConfig").FindPropertyRelative("Id").FindPropertyRelative("Value").longValue;
            var defaultConfigAssetObject = QuantumUnityDB.GetGlobalAsset(new AssetRef<AIConfig>(new AssetGuid(defaultConfigGUID)));
            var defaultConfigValues = defaultConfigAssetObject.KeyValuePairs;

            // Remove old values
            for (int i = configValues.Count - 1; i >= 0; i--)
            {
                var value = configValues[i];
                var defaultValue = defaultConfigValues.Find(t => t.Key == value.Key);

                if (defaultValue == null || defaultValue.Type != value.Type)
                {
                    configValues.RemoveAt(i);
                }
            }

            // Add missing values
            for (int i = 0; i < defaultConfigValues.Count; i++)
            {
                var defaultValue = defaultConfigValues[i];

                if (configValues.Find(t => t.Key == defaultValue.Key) == null)
                {
                    configValues.Add(new AIConfig.KeyValuePair
                    {
                        Key = defaultValue.Key,
                        Type = defaultValue.Type,
                        Value = defaultValue.Value,
                    });
                }
            }

            configValues.Sort((a, b) => a.Key.CompareTo(b.Key));

            serializedObject.Update();
        }

        private void ResetConfig()
        {
            var configAsset = serializedObject.targetObject as AIConfig;
            var configValues = configAsset.KeyValuePairs;

            var defaultConfigGUID = serializedObject.FindProperty("DefaultConfig").FindPropertyRelative("Id").FindPropertyRelative("Value").longValue;
            var defaultConfigAssetObject = QuantumUnityDB.GetGlobalAsset(new AssetRef<AIConfig>(new AssetGuid(defaultConfigGUID)));
            var defaultConfigValues = defaultConfigAssetObject.KeyValuePairs;

            configValues.Clear();

            for (int i = 0; i < defaultConfigValues.Count; i++)
            {
                var defaultValue = defaultConfigValues[i];

                configValues.Add(new AIConfig.KeyValuePair
                {
                    Key = defaultValue.Key,
                    Type = defaultValue.Type,
                    Value = defaultValue.Value,
                });
            }

            serializedObject.Update();
        }
    }
}
