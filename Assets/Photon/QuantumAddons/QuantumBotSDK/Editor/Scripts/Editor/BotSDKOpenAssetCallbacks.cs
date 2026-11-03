using UnityEditor;
using UnityEditor.Callbacks;

namespace Quantum.BotSDK.Editor
{
  /// <summary>
  /// Opens Bot SDK documents on the Circuit window when they are double clicked on the Project window.
  /// This is done on the Unity side, rather than on the precompiled Visual Editor assembly, because the signature required
  /// by [OnOpenAsset] depends on the Unity version: int only before Unity 6000.3, int or EntityId from 6000.3 to 6000.5,
  /// and EntityId only from Unity 6000.6 on.
  /// </summary>
  public static class BotSDKOpenAssetCallbacks
  {
#if UNITY_6000_3_OR_NEWER
    [OnOpenAsset(1)]
    public static bool OpenCircuitAsset(UnityEngine.EntityId entityId, int line)
    {
      return Circuit.CircuitEditorCallbacks.OpenCircuitAsset(EditorUtility.EntityIdToObject(entityId));
    }
#else
    [OnOpenAsset(1)]
    public static bool OpenCircuitAsset(int instanceID, int line)
    {
      return Circuit.CircuitEditorCallbacks.OpenCircuitAsset(EditorUtility.InstanceIDToObject(instanceID));
    }
#endif
  }
}
