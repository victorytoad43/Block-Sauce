using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// Dev only (the BlockBench scene): starts the scene's map as a local game with nobody in it, the way the menu's
    /// offline demo starts (<see cref="GameConnection"/>), with a start timeout long enough for the editor's first start;
    /// Quantum's own local debug runner gives up after 10 s. Trials come from editor scripts (<see cref="BlockBenchSystem"/>).
    /// </summary>
    public class BlockBenchStarter : MonoBehaviour
    {
        [SerializeField] private RuntimeConfig _runtimeConfig;
        [SerializeField] private QuantumDeterministicSessionConfigAsset _sessionConfig;

        private const float StartTimeoutSeconds = 30f;

        private const int Attempts = 5;

        private async void Start()
        {
            // The editor's first start often times out while play mode is still settling (the menu's demo retries
            // the same way), so it tries again.
            for (int attempt = 1; attempt <= Attempts && this != null && QuantumRunner.Default == null; attempt++)
            {
                try
                {
                    await StartSession();
                }
                catch (System.Exception e) when (attempt < Attempts)
                {
                    Debug.LogWarning($"[BlockBench] start attempt {attempt} failed ({e.Message}); trying again");
                }
            }
        }

        private async System.Threading.Tasks.Task StartSession()
        {
            var arguments = new SessionRunner.Arguments();
            arguments.InitForLocal(JsonUtility.FromJson<RuntimeConfig>(JsonUtility.ToJson(_runtimeConfig)));
            if (_sessionConfig != null)
            {
                arguments.SessionConfig = _sessionConfig.Config;
            }

            // Unity's time scale speeds trials up (editor scripts set it).
            arguments.DeltaTimeType = SimulationUpdateTime.EngineDeltaTime;
            arguments.StartGameTimeoutInSeconds = StartTimeoutSeconds;
            await SessionRunner.StartAsync(arguments);
        }

        /// <summary>
        /// Set by the bench's builder from the scene's debug runner.
        /// </summary>
        public void Configure(RuntimeConfig runtimeConfig, QuantumDeterministicSessionConfigAsset sessionConfig)
        {
            _runtimeConfig = runtimeConfig;
            _sessionConfig = sessionConfig;
        }
    }
}
