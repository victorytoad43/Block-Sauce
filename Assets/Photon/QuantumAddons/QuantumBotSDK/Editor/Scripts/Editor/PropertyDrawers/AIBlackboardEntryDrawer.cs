using UnityEditor;
using UnityEngine;

namespace Quantum.Editor
{
    [CustomPropertyDrawer(typeof(AIBlackboardEntry))]
    public class AIBlackboardEntryDrawer : PropertyDrawer
    {

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            return 20;
        }

        public override void OnGUI(Rect p, SerializedProperty prop, GUIContent label)
        {
            string index = null;
            if (label.text.StartsWith("Element")) index = $"{label.text.Substring(7)}) Key ";
            EditorGUI.indentLevel--;
            var labelWidth = EditorGUIUtility.labelWidth;
            var halfWidth = p.width / 2;
            EditorGUIUtility.labelWidth = p.width / 4;
            EditorGUI.PropertyField(p.SetWidth(halfWidth), prop.FindPropertyRelative("Key").FindPropertyRelative("Key"), string.IsNullOrWhiteSpace(index) ? label : new GUIContent(index));
            EditorGUI.PropertyField(p.SetWidth(halfWidth - 50).AddX(halfWidth + 50), prop.FindPropertyRelative("Type"));
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel++;
        }
    }

}