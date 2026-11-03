using UnityEditor;
using UnityEngine;

namespace Quantum.Editor
{
    [CustomPropertyDrawer(typeof(AIConfig.KeyValuePair))]
    public class AIConfigKeyValuePairDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            string key = property.FindPropertyRelative("Key").stringValue;

            var valueName = new GUIContent(ObjectNames.NicifyVariableName(key));
            var valueType = (AIConfig.EValueType)property.FindPropertyRelative("Type").enumValueIndex;
            var valueProperty = property.FindPropertyRelative("Value");

            SerializedProperty visibleValueProperty = null;

            switch (valueType)
            {
                case AIConfig.EValueType.Int:
                    visibleValueProperty = valueProperty.FindPropertyRelative("Integer");
                    break;
                case AIConfig.EValueType.Bool:
                    visibleValueProperty = valueProperty.FindPropertyRelative("Boolean");
                    break;
                case AIConfig.EValueType.Byte:
                    visibleValueProperty = valueProperty.FindPropertyRelative("Byte");
                    break;
                case AIConfig.EValueType.FP:
                    visibleValueProperty = valueProperty.FindPropertyRelative("FP");
                    break;
                case AIConfig.EValueType.FPVector2:
                    visibleValueProperty = valueProperty.FindPropertyRelative("FPVector2");
                    break;
                case AIConfig.EValueType.FPVector3:
                    visibleValueProperty = valueProperty.FindPropertyRelative("FPVector3");
                    break;
                case AIConfig.EValueType.String:
                    visibleValueProperty = valueProperty.FindPropertyRelative("String");
                    break;
                case AIConfig.EValueType.EntityRef:
                    visibleValueProperty = valueProperty.FindPropertyRelative("EntityRef");
                    break;
                case AIConfig.EValueType.AssetRef:
                    visibleValueProperty = valueProperty.FindPropertyRelative("AssetRef");
                    break;
            }

            if (visibleValueProperty != null)
            {
                EditorGUI.PropertyField(rect, visibleValueProperty, valueName);
            }
            else
            {
                EditorGUI.LabelField(rect, "Missing value for " + valueName);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueType = (AIConfig.EValueType)property.FindPropertyRelative("Type").enumValueIndex;

            switch (valueType)
            {
                case AIConfig.EValueType.FPVector2:
                    return 60;
                case AIConfig.EValueType.FPVector3:
                    return 85;
                default:
                    return 20;
            }
        }
    }
}