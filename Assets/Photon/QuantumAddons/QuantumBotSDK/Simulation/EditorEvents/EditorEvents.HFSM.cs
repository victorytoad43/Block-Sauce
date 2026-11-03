using System;
using System.Runtime.CompilerServices;

namespace Quantum
{
    public static partial class BotSDKEditorEvents
    {
        public static class HFSM
        {
            // --------------------
            // ON SETUP DEBUGGER
            // --------------------
            private static event Action<EntityRef, string> _onSetupDebugger;
            public static event Action<EntityRef, string> OnSetupDebugger
            {
                add => _onSetupDebugger += value;
                remove => _onSetupDebugger -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnSetupDebugger(EntityRef entityRef, string rootAssetPath)
            {
                try
                {
                    _onSetupDebugger?.Invoke(entityRef, rootAssetPath);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // ON STATE CHANGED
            // --------------------
            private static event Action<EntityRef, string, long, long, string, string> _hfsmStateChanged;
            public static event Action<EntityRef, string, long, long, string, string> HFSMStateChanged
            {
                add => _hfsmStateChanged += value;
                remove => _hfsmStateChanged -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeStateChanged(EntityRef entityRef, string rootName, long rootAssetId, long newStateGuid, string transitionId, string circuitId)
            {
                try
                {
                    _hfsmStateChanged?.Invoke(entityRef, rootName, rootAssetId, newStateGuid, transitionId, circuitId);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }
    }
}