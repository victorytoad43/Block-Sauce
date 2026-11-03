using Photon.Deterministic;
using Quantum.Core;
using System;
using System.Runtime.CompilerServices;

namespace Quantum
{
    public static partial class BotSDKEditorEvents
    {
        public static class UT
        {
            // --------------------
            // SETUP DEBUGGER
            // --------------------
            private static event Action<FrameBase, EntityRef, string, long> _onSetupDebugger;
            public static event Action<FrameBase, EntityRef, string, long> OnSetupDebugger
            {
                add => _onSetupDebugger += value;
                remove => _onSetupDebugger -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeSetupDebugger(FrameBase frame, EntityRef entityRef, string utPath, long rootGuid)
            {
                try
                {
                    _onSetupDebugger?.Invoke(frame, entityRef, utPath, rootGuid);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // CLEAR CHOSEN CONSIDERATIONS
            // --------------------
            private static event Action<EntityRef, long> _clearChosenConsiderations;
            public static event Action<EntityRef, long> ClearChosenConsiderations
            {
                add => _clearChosenConsiderations += value;
                remove => _clearChosenConsiderations -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeClearChosenConsiderations(EntityRef entityRef, long rootAssetId)
            {
                try
                {
                    _clearChosenConsiderations?.Invoke(entityRef, rootAssetId);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // CONSIDERATION CHOSEN
            // --------------------
            private static event Action<EntityRef, long, string> _considerationChosen;
            public static event Action<EntityRef, long, string> ConsiderationChosen
            {
                add => _considerationChosen += value;
                remove => _considerationChosen -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeConsiderationChosen(EntityRef entityRef, long rootAssetId, string circuitId)
            {
                try
                {
                    _considerationChosen?.Invoke(entityRef, rootAssetId, circuitId);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // PROCESSED RANK
            // --------------------
            private static event Action<EntityRef, long, string, int, bool> _processedRank;
            public static event Action<EntityRef, long, string, int, bool> ProcessedRank
            {
                add => _processedRank += value;
                remove => _processedRank -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeProcessedRank(EntityRef entityRef, long rootAssetId, string circuitId, int rank, bool isInMomentum)
            {
                try
                {
                    _processedRank?.Invoke(entityRef, rootAssetId, circuitId, rank, isInMomentum);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // PROCESSED SCORE
            // --------------------
            private static event Action<EntityRef, long, string, FP> _processedScore;
            public static event Action<EntityRef, long, string, FP> ProcessedScore
            {
                add => _processedScore += value;
                remove => _processedScore -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeProcessedScore(EntityRef entityRef, long rootAssetId, string circuitId, FP score)
            {
                try
                {
                    _processedScore?.Invoke(entityRef, rootAssetId, circuitId, score);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // PROCESSED RESPONSE CURVE
            // --------------------
            private static event Action<EntityRef, long, string, int, FP, FP> _processedResponseCurve;
            public static event Action<EntityRef, long, string, int, FP, FP> ProcessedResponseCurve
            {
                add => _processedResponseCurve += value;
                remove => _processedResponseCurve -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeProcessedResponseCurve(EntityRef entityRef, long rootAssetId, string circuitId, int curveId, FP input, FP score)
            {
                try
                {
                    _processedResponseCurve?.Invoke(entityRef, rootAssetId, circuitId, curveId, input, score);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // --------------------
            // COOLDOWN STATE CHANGE
            // --------------------
            private static event Action<EntityRef, long, string, bool> _cooldownStateChanged;
            public static event Action<EntityRef, long, string, bool> CooldownStateChanged
            {
                add => _cooldownStateChanged += value;
                remove => _cooldownStateChanged -= value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void InvokeCooldownStateChanged(EntityRef entityRef, long rootAssetId, string circuitId, bool state)
            {
                try
                {
                    _cooldownStateChanged?.Invoke(entityRef, rootAssetId, circuitId, state);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }
    }
}