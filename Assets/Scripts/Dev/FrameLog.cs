using System.Collections.Generic;
using System.Globalization;
using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// Development builds only: with "-logframes" on the command line, writes a line to the player log every 5 seconds
    /// with the frame rate, median, 99th percentile and worst frame time, the main thread, render thread and GPU times,
    /// the simulation's update time and predicted frames, and how many characters are down. Measures performance
    /// outside the editor. "-fps N" caps the frame rate (for profiler captures that hold a long stretch of frames).
    /// </summary>
    public class FrameLog : MonoBehaviour
    {
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        private const float Interval = 5f;

        private readonly List<float> _frames = new List<float>();
        private readonly FrameTiming[] _timings = new FrameTiming[1];
        private double _mainSum, _renderSum, _gpuSum, _simSum;
        private int _timingCount, _simCount, _maxPredicted;
        private float _nextLog;
        private int _lastGcCount;
        private readonly List<string> _hitches = new List<string>();
        private Unity.Profiling.ProfilerRecorder _presentWait;
        private Unity.Profiling.ProfilerRecorder _playerLoop;

        private void OnEnable()
        {
            _presentWait = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,
                "TimeUpdate.WaitForLastPresentationAndUpdateTime");
            _playerLoop = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "PlayerLoop");
        }

        private void OnDisable()
        {
            _presentWait.Dispose();
            _playerLoop.Dispose();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int fps = System.Array.IndexOf(args, "-fps");
            if (fps >= 0 && fps + 1 < args.Length && int.TryParse(args[fps + 1], out int target))
            {
                Application.targetFrameRate = target;
            }

            if (System.Array.IndexOf(args, "-logframes") < 0)
                return;

            var host = new GameObject("FrameLog");
            DontDestroyOnLoad(host);
            host.AddComponent<FrameLog>();
        }

        private void Update()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            _frames.Add(ms);
            int gcCount = System.GC.CollectionCount(0);
            if (ms > 50f && _hitches.Count < 8)
            {
                // The recorders hold the previous frame's samples: this frame's present wait shows up in the next frame's loop.
                _hitches.Add($"{ms:F0} ms{(gcCount != _lastGcCount ? " (GC)" : "")}"
                    + $" [loop {_playerLoop.LastValue / 1e6:F0}, present wait {_presentWait.LastValue / 1e6:F0}]");
            }

            _lastGcCount = gcCount;

            FrameTimingManager.CaptureFrameTimings();
            // The GPU time is sometimes garbage for a frame; skip those samples.
            if (FrameTimingManager.GetLatestTimings(1, _timings) > 0 && _timings[0].gpuFrameTime < 1000.0)
            {
                _mainSum += _timings[0].cpuMainThreadFrameTime;
                _renderSum += _timings[0].cpuRenderThreadFrameTime;
                _gpuSum += _timings[0].gpuFrameTime;
                _timingCount++;
            }

            QuantumGame game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            if (game?.Session != null)
            {
                _simSum += game.Session.Stats.UpdateTime * 1000.0;
                _simCount++;
                _maxPredicted = Mathf.Max(_maxPredicted, game.Session.PredictedFrames);
            }

            if (Time.unscaledTime < _nextLog)
                return;

            _nextLog = Time.unscaledTime + Interval;
            if (_frames.Count > 1)
            {
                Write(game);
            }

            _frames.Clear();
            _hitches.Clear();
            _mainSum = _renderSum = _gpuSum = _simSum = 0;
            _timingCount = _simCount = _maxPredicted = 0;
        }

        private void Write(QuantumGame game)
        {
            _frames.Sort();
            float total = 0f;
            foreach (float frame in _frames)
            {
                total += frame;
            }

            Frame verified = game?.Frames.Verified;
            int down = verified != null ? verified.ComponentCount<RagdollOwner>() : 0;
            string state = verified != null && verified.TryGetSingleton(out Match match) ? match.State.ToString() : "no match";
            string F(double value) => value.ToString("F1", CultureInfo.InvariantCulture);
            Debug.Log($"[FrameLog] fps {F(_frames.Count * 1000f / total)} median {F(_frames[_frames.Count / 2])} ms"
                + $" p99 {F(_frames[(int)(_frames.Count * 0.99f)])} worst {F(_frames[_frames.Count - 1])}"
                + $" | main {F(_timingCount > 0 ? _mainSum / _timingCount : 0)} render {F(_timingCount > 0 ? _renderSum / _timingCount : 0)}"
                + $" gpu {F(_timingCount > 0 ? _gpuSum / _timingCount : 0)} | sim {F(_simCount > 0 ? _simSum / _simCount : 0)} ms"
                + $" predicted max {_maxPredicted} | down {down} | {state} | GCs {System.GC.CollectionCount(0)}, heap {System.GC.GetTotalMemory(false) / 1048576} MB"
                + (_hitches.Count > 0 ? " | hitches " + string.Join(", ", _hitches) : ""));
        }
#endif
    }
}
