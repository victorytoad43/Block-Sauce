using Photon.Deterministic;
using System;
using System.Runtime.CompilerServices;

namespace Quantum
{
    public static partial class BotSDKEditorEvents
    {
        public static class BT
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void InvokeAction(Action<EntityRef, long, long, string> action, EntityRef entityRefParam, long longParam, long longParam2, string stringParam)
            {
                try
                {
                    action?.Invoke(entityRefParam, longParam, longParam2, stringParam);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void InvokeAction(Action<EntityRef, long, string, FP> action, EntityRef entityRefParam, long longParam, string stringParam, FP fpParam)
            {
                try
                {
                    action?.Invoke(entityRefParam, longParam, stringParam, fpParam);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

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
            public static void InvokeOnSetupDebugger(EntityRef entityRef, string treePath)
            {
                try
                {
                    _onSetupDebugger?.Invoke(entityRef, treePath);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // ON TREE COMPLETED
            // --------------------
            private static event Action<EntityRef, long> _onTreeStart;
            public static event Action<EntityRef, long> OnTreeStart
            {
                add => _onTreeStart += value;
                remove => _onTreeStart -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnTreeStart(EntityRef entityRef, long treeGuid)
            {
                try
                {
                    _onTreeStart?.Invoke(entityRef, treeGuid);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // ON NODE ENTER
            // --------------------
            private static event Action<EntityRef, long, long, string> _onNodeEnter;
            public static event Action<EntityRef, long, long, string> OnNodeEnter
            {
                add => _onNodeEnter += value;
                remove => _onNodeEnter -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnNodeEnter(EntityRef entityRef, long rootAssetId, long nodeId, string circuitId)
            {
                InvokeAction(_onNodeEnter, entityRef, rootAssetId, nodeId, circuitId);
            }

            // --------------------
            // ON NODE EXIT
            // --------------------
            private static event Action<EntityRef, long, long, string> _onNodeExit;
            public static event Action<EntityRef, long, long, string> OnNodeExit
            {
                add => _onNodeExit += value;
                remove => _onNodeExit -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnNodeExit(EntityRef entityRef, long rootAssetId, long nodeId, string circuitId)
            {
                InvokeAction(_onNodeExit, entityRef, rootAssetId, nodeId, circuitId);
            }

            // --------------------
            // ON DYNAMIC FAILURE
            // --------------------
            private static event Action<EntityRef, long, long, string> _onDynamicFailure;
            public static event Action<EntityRef, long, long, string> OnDynamicFailure
            {
                add => _onDynamicFailure += value;
                remove => _onDynamicFailure -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnDynamicFailure(EntityRef entityRef, long rootAssetId, long nodeId, string circuitId)
            {
                InvokeAction(_onDynamicFailure, entityRef, rootAssetId, nodeId, circuitId);
            }

            // --------------------
            // ON NODE SUCCESS
            // --------------------
            private static event Action<EntityRef, long, long, string> _onNodeSuccess;
            public static event Action<EntityRef, long, long, string> OnNodeSuccess
            {
                add => _onNodeSuccess += value;
                remove => _onNodeSuccess -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnNodeSuccess(EntityRef entityRef, long rootAssetId, long nodeId, string circuitId)
            {
                InvokeAction(_onNodeSuccess, entityRef, rootAssetId, nodeId, circuitId);
            }

            // --------------------
            // ON NODE FAILURE
            // --------------------
            private static event Action<EntityRef, long, long, string> _onNodeFailure;
            public static event Action<EntityRef, long, long, string> OnNodeFailure
            {
                add => _onNodeFailure += value;
                remove => _onNodeFailure -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnNodeFailure(EntityRef entityRef, long rootAssetId, long nodeId, string circuitId)
            {
                InvokeAction(_onNodeFailure, entityRef, rootAssetId, nodeId, circuitId);
            }

            // --------------------
            // ON NODE INACTIVE
            // --------------------
            private static event Action<EntityRef, long, long, string> _onNodeInactive;
            public static event Action<EntityRef, long, long, string> OnNodeInactive
            {
                add => _onNodeInactive += value;
                remove => _onNodeInactive -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnNodeInactive(EntityRef entityRef, long rootAssetId, long nodeId, string circuitId)
            {
                InvokeAction(_onNodeInactive, entityRef, rootAssetId, nodeId, circuitId);
            }

            // --------------------
            // ON DECORATOR CHECKED
            // --------------------
            private static event Action<EntityRef, long, long, string, bool> _onDecoratorChecked;
            public static event Action<EntityRef, long, long, string, bool> OnDecoratorChecked
            {
                add => _onDecoratorChecked += value;
                remove => _onDecoratorChecked -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnDecoratorChecked(EntityRef entityRef, long rootAssetId, long nodeId, string circuitId, bool success)
            {
                try
                {
                    _onDecoratorChecked?.Invoke(entityRef, rootAssetId, nodeId, circuitId, success);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // ON DECORATOR RESET
            // --------------------
            private static event Action<EntityRef, long, long, string> _onDecoratorReset;
            public static event Action<EntityRef, long, long, string> OnDecoratorReset
            {
                add => _onDecoratorReset += value;
                remove => _onDecoratorReset -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnDecoratorReset(EntityRef entityRef, long rootAssetId, long nodeId, string circuitId)
            {
                InvokeAction(_onDecoratorReset, entityRef, rootAssetId, nodeId, circuitId);
            }

            // --------------------
            // ON DECORATOR RESET
            // --------------------
            private static event Action<EntityRef, long, string, FP> _onServiceProgress;
            public static event Action<EntityRef, long, string, FP> OnServiceProgress
            {
                add => _onServiceProgress += value;
                remove => _onServiceProgress -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeOnServiceProgress(EntityRef entityRef, long rootAssetId, string circuitId, FP progress)
            {
                InvokeAction(_onServiceProgress, entityRef, rootAssetId, circuitId, progress);
            }
        }
    }
}
