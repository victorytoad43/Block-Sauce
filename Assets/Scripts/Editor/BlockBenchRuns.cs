using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using UnityEditor;
using UnityEngine;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Block Sauce › Block Bench › Run …: the bench's standard panels (Blocks phase). Play the BlockBench scene
    /// (<c>Assets/Scenes/Dev/BlockBench.unity</c>, built by Block Sauce › Build Block Bench), then pick a panel; each
    /// trial writes a <c>[BlockBench]</c> line to the console, and the panel runs sped up (time scale 8). Slides are in
    /// centimeters (the most any block moved sideways), tilt in degrees; "fallen" counts tower blocks off their level,
    /// before and after a hit.
    /// </summary>
    public static class BlockBenchRuns
    {
        private const float TimeScale = 8f;
        private static GameObject _listener;

        [MenuItem("Block Sauce/Block Bench/Run Placing Panel")]
        public static void RunPlacing() => Run("placing", PlacingPanel());

        [MenuItem("Block Sauce/Block Bench/Run Carry Panel")]
        public static void RunCarry() => Run("carry", CarryPanel());

        [MenuItem("Block Sauce/Block Bench/Run Hit Panel")]
        public static void RunHits() => Run("hit", HitPanel());

        [MenuItem("Block Sauce/Block Bench/Run Sauce Panel")]
        public static void RunSauce() => Run("sauce", SaucePanel());

        /// <summary>
        /// A third (or fourth) block let go at offsets from 0 to 30 cm off center, recorded for 15 s.
        /// </summary>
        public static List<(string, BlockBenchTrial)> PlacingPanel()
        {
            var trials = new List<(string, BlockBenchTrial)>();
            foreach (int cm in new[] { 0, 2, 5, 10, 15, 20, 30 })
            {
                trials.Add(($"third, {cm} cm off", With(t => { t.Offset = Off(cm); t.Record = 15; return t; })));
            }

            trials.Add(("third, 10 cm off diagonally", With(t => { t.Offset = Off(7, 7); t.Record = 15; return t; })));
            trials.Add(("third, 10 cm off, dropped 10 cm", With(t => { t.Offset = Off(10); t.Gap = (FP)1 / 10; t.Record = 15; return t; })));
            trials.Add(("third, 10 cm off, base off 5 cm", With(t => { t.Offset = Off(10); t.BaseOffset = Off(5); t.Record = 15; return t; })));
            trials.Add(("third, tower awake", With(t => { t.Offset = Off(5); t.BaseWait = FP._0_50; t.Record = 15; return t; })));
            trials.Add(("fourth, 5 cm off", With(t => { t.Base = 3; t.Offset = Off(5); t.Record = 15; return t; })));
            trials.Add(("second, 15 cm off", With(t => { t.Base = 1; t.Offset = Off(15); t.Record = 15; return t; })));
            return trials;
        }

        /// <summary>
        /// The third block carried in from the side the way a hold carries it, pressed 0 to 50 cm into the tower.
        /// </summary>
        public static List<(string, BlockBenchTrial)> CarryPanel()
        {
            var trials = new List<(string, BlockBenchTrial)>();
            foreach (int speed in new[] { 15, 30 })
            {
                foreach (int cm in new[] { 0, 5, 15, 30, 50 })
                {
                    trials.Add(($"carried at {speed / 10f} m/s, pressed {cm} cm", With(t =>
                    {
                        t.CarrySpeed = (FP)speed / 10;
                        t.Press = (FP)cm / 100;
                        t.Record = 10;
                        return t;
                    })));
                }
            }

            return trials;
        }

        /// <summary>
        /// A block flying into a three-block tower at 4 to 12 m/s, at each level and five points along its face.
        /// </summary>
        public static List<(string, BlockBenchTrial)> HitPanel()
        {
            var trials = new List<(string, BlockBenchTrial)>();
            foreach (int speed in new[] { 4, 6, 8, 12 })
            {
                for (int level = 0; level < 3; level++)
                {
                    foreach (int cm in new[] { -30, -15, 0, 15, 30 })
                    {
                        trials.Add(($"hit {speed} m/s, level {level}, {cm} cm aside", With(t =>
                        {
                            t.HitSpeed = speed;
                            t.HitLevel = level;
                            t.HitSide = (FP)cm / 100;
                            t.Record = 2;
                            return t;
                        })));
                    }
                }
            }

            return trials;
        }

        /// <summary>
        /// Light hits on the top of plain, Slip and Stack towers: Slip ones should fall to hits the others shrug off.
        /// </summary>
        public static List<(string, BlockBenchTrial)> SaucePanel()
        {
            var trials = new List<(string, BlockBenchTrial)>();
            foreach (SauceType sauce in new[] { SauceType.None, SauceType.Slip, SauceType.Stack })
            {
                foreach (int speed in new[] { 2, 3, 4 })
                {
                    foreach (int cm in new[] { -20, 0, 20 })
                    {
                        trials.Add(($"{sauce}, hit {speed} m/s on top, {cm} cm aside", With(t =>
                        {
                            t.Sauce = sauce;
                            t.SauceFraction = FP._1;
                            t.HitSpeed = speed;
                            t.HitLevel = 2;
                            t.HitSide = (FP)cm / 100;
                            t.Record = 2;
                            return t;
                        })));
                    }
                }
            }

            return trials;
        }

        /// <summary>
        /// Hands the trials to the bench and logs a line per finished trial; the bench scene must be playing.
        /// </summary>
        public static void Run(string panel, List<(string, BlockBenchTrial)> trials)
        {
            if (EditorApplication.isPlaying == false || QuantumRunner.Default?.Game == null)
            {
                Debug.LogWarning("[BlockBench] Play the BlockBench scene first (Assets/Scenes/Dev/BlockBench.unity).");
                return;
            }

            if (_listener != null)
            {
                Object.Destroy(_listener);
            }

            _listener = new GameObject("BlockBenchRuns");
            var names = new List<string>();
            var list = new List<BlockBenchTrial>();
            foreach ((string name, BlockBenchTrial trial) in trials)
            {
                names.Add(name);
                list.Add(trial);
            }

            int done = 0;
            QuantumEvent.Subscribe<EventBlockBenchTrialEnded>(_listener, e =>
            {
                FP slide = FPMath.Max(e.BaseSlide, e.TopSlide);
                string fallen = e.Fallen != e.FallenBeforeHit ? $"{e.FallenBeforeHit} -> {e.Fallen} after the hit" : e.Fallen.ToString();
                Debug.Log($"[BlockBench] {panel}: {names[e.Trial]}: slide {slide.AsFloat * 100f:F0} cm, tilt {e.MaxTilt.AsFloat:F1}°, " +
                    $"pushed before let-go {e.Pushed.AsFloat * 100f:F0} cm, fallen {fallen}, counted {e.Counted}");
                if (++done >= names.Count)
                {
                    Time.timeScale = 1f;
                    Debug.Log($"[BlockBench] {panel} panel done ({names.Count} trials)");
                }
            });

            BlockBenchSystem.Trials = list;
            BlockBenchSystem.Restart = true;
            Time.timeScale = TimeScale;
        }

        private static BlockBenchTrial With(System.Func<BlockBenchTrial, BlockBenchTrial> change) => change(BlockBenchTrial.Default);

        private static FPVector2 Off(int xCm, int zCm = 0) => new FPVector2((FP)xCm / 100, (FP)zCm / 100);
    }
}
