//using System;
//using UnityEditor;
//using UnityEngine;
//using Photon.Deterministic;
//using Quantum;

//namespace Quantum.Editor
//{
//  [CustomPropertyDrawer(typeof(AIBlackboardInitializer.AIBlackboardInitialValueEntry))]
//  public class AIBlackboardInitialValueEntryDrawer : PropertyDrawer
//  {

//    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
//    {
//      // Find the blackboard first
//      long blackboardGUID = prop.serializedObject.FindProperty("Settings").FindPropertyRelative("AIBlackboard").
//                            FindPropertyRelative("Id").FindPropertyRelative("Value").longValue;

//      string blackboardKey = prop.FindPropertyRelative("Key").stringValue;
//      if (blackboardGUID != 0 && blackboardKey != null)
//      {
//        var asset = QuantumUnityDB.GetGlobalAsset<AIBlackboard>(blackboardGUID);
//        if (asset.HasEntry(blackboardKey) == false)
//        {
//          return 20;
//        }

//        var entry = asset.GetEntry(blackboardKey);
//        switch (entry.Type)
//        {
//          case AIBlackboardValueType.Vector2:
//            return 60;
//          case AIBlackboardValueType.Vector3:
//            return 80;
//        }
//      }

//      return 20;
//    }

//    public override void OnGUI(Rect p, SerializedProperty prop, GUIContent label)
//    {
//      // Find the blackboard first
//      long blackboardGUID = prop.serializedObject.FindProperty("Settings").FindPropertyRelative("AIBlackboard").
//                            FindPropertyRelative("Id").FindPropertyRelative("Value").longValue;

//      string blackboardKey = prop.FindPropertyRelative("Key").stringValue;
//      if (blackboardGUID != 0 && blackboardKey != null)
//      {
//        var asset = UnityDB.FindAsset<AIBlackboard>(blackboardGUID);

//        if (asset.Settings.HasEntry(blackboardKey) == false)
//        {
//          EditorGUI.LabelField(p, "Previously used value. Toggle the inspector to debug mode to remove.");
//          return;
//        }

//        var niceName = new GUIContent(ObjectNames.NicifyVariableName(blackboardKey));
//        var entry = asset.Settings.GetEntry(blackboardKey);
//        SerializedProperty propToDraw = prop.FindPropertyRelative("Key");
//        switch (entry.Type)
//        {
//          case AIBlackboardValueType.Boolean:
//            propToDraw = prop.FindPropertyRelative("Value").FindPropertyRelative("AsBoolean");
//            break;
//          case AIBlackboardValueType.Byte:
//            propToDraw = prop.FindPropertyRelative("Value").FindPropertyRelative("AsByte");
//            break;
//          case AIBlackboardValueType.Integer:
//            propToDraw = prop.FindPropertyRelative("Value").FindPropertyRelative("AsInteger");
//            break;
//          case AIBlackboardValueType.FP:
//            propToDraw = prop.FindPropertyRelative("Value").FindPropertyRelative("AsFP");
//            break;
//          case AIBlackboardValueType.Vector2:
//            propToDraw = prop.FindPropertyRelative("Value").FindPropertyRelative("AsFPVector2");
//            break;
//          case AIBlackboardValueType.Vector3:
//            propToDraw = prop.FindPropertyRelative("Value").FindPropertyRelative("AsFPVector3");
//            break;
//					case AIBlackboardValueType.AssetRef:
//						propToDraw = prop.FindPropertyRelative("Value").FindPropertyRelative("AsAssetRef");
//						break;
//					case AIBlackboardValueType.EntityRef:
//          default:
//            //propToDraw = prop.FindPropertyRelative( "Value" ).FindPropertyRelative( "asEntityRef" );
//            //break;
//            EditorGUI.LabelField(p, $"{blackboardKey}: This field type is not supported.");
//            return;
//        }

//        EditorGUI.PropertyField(p, propToDraw, niceName);
//      }
//      else
//      {
//        EditorGUI.LabelField(p, $"{blackboardKey}: Unused value. Toggle the inspector to debug mode to remove.");
//      }
//    }
//  }

//}