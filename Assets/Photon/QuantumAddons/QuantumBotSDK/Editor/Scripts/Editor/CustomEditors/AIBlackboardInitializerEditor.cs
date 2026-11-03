//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;

//namespace Quantum.Editor
//{
//  [CustomEditor(typeof(AIBlackboardInitializerAsset), true)]
//  public class AIBlackboardInitializerEditor : UnityEditor.Editor
//  {

//    private SerializedProperty propInitialValues;
//    private SerializedProperty propBlackboardGuid;
//    private AIBlackboardAsset asset;

//    public void OnEnable()
//    {
//      propInitialValues = serializedObject.FindProperty("Settings").FindPropertyRelative("InitialValues");
//      propBlackboardGuid = serializedObject.FindProperty("Settings").FindPropertyRelative("AIBlackboard").
//        FindPropertyRelative("Id").FindPropertyRelative("Value");
//    }

//    public override void OnInspectorGUI()
//    {
//#pragma warning disable
//      //base.OnInspectorGUI();
//      //return;

//      serializedObject.Update();

//      // This draws all fields except the "Settings" property.
//      CustomEditorsHelper.DrawDefaultInspector(serializedObject, new string[] { "Settings" });

//      // As we show only the content of "Settings" add a headline here to make it clear that this is not the default asset layout.
//      CustomEditorsHelper.DrawHeadline(target != null ? target.GetType().Name : "Quantum Asset Inspector");

//      // We know the data is called "Settings" so we can go ahead and only render the content of "Settings".
//      CustomEditorsHelper.BeginBox();
//      CustomEditorsHelper.DrawDefaultInspector(serializedObject, "Settings", new string[] { "Settings.InitialValues" }, false);

//      List<string> existingEntries = new List<string>();

//      long blackboardGUID = propBlackboardGuid.longValue;
//      if (blackboardGUID != 0)
//      {
//        if (asset == null)
//        {
//          asset = UnityDB.FindAsset<AIBlackboardAsset>(blackboardGUID);
//        }

//        if (asset == null)
//        {
//          GUILayout.Label("Please assign a Blackboard first.");
//        }
//        else
//        {
//          EditorGUILayout.LabelField("Initial Values:");
//          for (int i = 0; i < asset.Settings.Entries.Length; ++i)
//          {
//            string key = asset.Settings.Entries[i].Key.Key;

//            bool found = false;

//            for (int j = 0; j < propInitialValues.arraySize; ++j)
//            {
//              var ivEntryProp = propInitialValues.GetArrayElementAtIndex(j);
//              if (ivEntryProp == null) continue;

//              var entryKey = ivEntryProp.FindPropertyRelative("Key");
//              if (key == entryKey.stringValue)
//              {
//                GUILayout.BeginHorizontal();
//                EditorGUI.indentLevel++;
//                EditorGUILayout.PropertyField(ivEntryProp);
//                EditorGUI.indentLevel--;
//                GUILayout.Space(2f);
//                if (GUILayout.Button("\u2717", GUILayout.MaxWidth(20)))
//                {
//                  propInitialValues.DeleteArrayElementAtIndex(j);
//                }
//                GUILayout.EndHorizontal();

//                found = true;
//                break;
//              }
//            }
//            if (found == false)
//            {
//              EditorGUI.indentLevel++;

//              GUILayout.BeginHorizontal();
//              EditorGUILayout.LabelField(key);
//              if (GUILayout.Button("Add Field"))
//              {
//                int newIndex = Mathf.Max(0, Mathf.Min(i, propInitialValues.arraySize - 1));
//                propInitialValues.InsertArrayElementAtIndex(newIndex);
//                var newProp = propInitialValues.GetArrayElementAtIndex(newIndex);
//                newProp.FindPropertyRelative("Key").stringValue = key;
//              }
//              GUILayout.EndHorizontal();
//              EditorGUI.indentLevel--;
//            }
//          }
//        }
//      }
//      if (serializedObject.hasModifiedProperties)
//      {
//        serializedObject.ApplyModifiedProperties();
//      }

//      CustomEditorsHelper.EndBox();

//#pragma warning restore
//    }
//  }
//}