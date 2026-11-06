using Quantum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Block Sauce › Build Block Bench: makes the BlockBench dev scene (Blocks phase) from the ragdoll stress test's
    /// plain floor, without its start UI (the local debug runner starts it): its own map, a match on the bench's mode (a tower too tall to win, so trials never end the match)
    /// and the BlockBench entity that runs trials (<see cref="BlockBenchSystem"/>). Never in a build.
    /// </summary>
    public static class BlockBenchBuilder
    {
        private const string SourcePath = "Assets/Scenes/Tests/Test_RagdollStress.unity";
        private const string ScenePath = "Assets/Scenes/Dev/BlockBench.unity";
        private const string MapPath = "Assets/Scenes/Dev/BlockBench/BlockBenchMap.asset";
        private const string BaseModePath = "Assets/QuantumUser/Resources/Configs/StackAndLockMode.asset";
        private const string ModePath = "Assets/QuantumUser/Resources/Configs/BlockBenchMode.asset";
        private const string GrabPath = "Assets/QuantumUser/Resources/Configs/PlayerGrabConfig.asset";
        private static readonly Vector3 BenchPosition = new Vector3(0f, 0f, -6f);

        [MenuItem("Block Sauce/Build Block Bench")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MapPath));
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                AssetDatabase.CopyAsset(SourcePath, ScenePath);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (string name in new[] { "RagdollStressSpawners", "PH_Blocks", "Match", "BlockBench", "Canvas" })
            {
                GameObject existing = GameObject.Find(name);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
            }

            var map = AssetDatabase.LoadAssetAtPath<Map>(MapPath);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<Map>();
                AssetDatabase.CreateAsset(map, MapPath);
            }

            var baseMode = AssetDatabase.LoadAssetAtPath<GameModeConfig>(BaseModePath);
            var mode = AssetDatabase.LoadAssetAtPath<GameModeConfig>(ModePath);
            if (mode == null)
            {
                mode = Object.Instantiate(baseMode);
                AssetDatabase.CreateAsset(mode, ModePath);
            }

            // Everything as in a match, except a tower of up to five blocks counts and none wins.
            mode.TowerHeight = 6;
            EditorUtility.SetDirty(mode);
            AssetDatabase.SaveAssets();
            Quantum.Editor.QuantumUnityDBUtilities.RefreshGlobalDB();

            var data = Object.FindAnyObjectByType<QuantumMapData>();
            data.AssetRef = new AssetRef<Map>(RefOf(map).Id);
            EditorUtility.SetDirty(data);
            foreach (var runner in Object.FindObjectsByType<QuantumRunnerLocalDebug>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                runner.RuntimeConfig.Map = data.AssetRef;
                runner.enabled = false;
                EditorUtility.SetDirty(runner);

                // Quantum's debug runner gives up on the editor's slow first start; the starter waits longer.
                BlockSauce.BlockBenchStarter starter = runner.GetComponent<BlockSauce.BlockBenchStarter>();
                if (starter == null)
                {
                    starter = runner.gameObject.AddComponent<BlockSauce.BlockBenchStarter>();
                }

                starter.Configure(runner.RuntimeConfig, runner.SessionConfig);
                EditorUtility.SetDirty(starter);
            }

            var match = new GameObject("Match");
            match.AddComponent<QuantumEntityPrototype>().TransformMode = QuantumEntityPrototypeTransformMode.None;
            match.AddComponent<QPrototypeMatch>().Prototype = new Quantum.Prototypes.MatchPrototype { Mode = new AssetRef<GameModeConfig>(RefOf(mode).Id) };

            var bench = new GameObject("BlockBench");
            bench.transform.position = BenchPosition;
            bench.AddComponent<QuantumEntityPrototype>().TransformMode = QuantumEntityPrototypeTransformMode.Transform3D;
            bench.AddComponent<QPrototypeBlockBench>().Prototype = new Quantum.Prototypes.Unity.BlockBenchPrototype
            {
                Block = baseMode.BlockPrototype,
                Grab = new AssetRef<GrabConfig>(RefOf(AssetDatabase.LoadAssetAtPath<GrabConfig>(GrabPath)).Id),
            };

            // The camera looks at the bench from the side the carried blocks come from.
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = BenchPosition + new Vector3(-3.5f, 2.2f, -3.5f);
                camera.transform.LookAt(BenchPosition + Vector3.up * 1.2f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Quantum.Editor.QuantumEditorAutoBaker.BakeCurrentScene_MapData();
            EditorSceneManager.SaveScene(scene);
        }

        private static AssetRef RefOf(Object asset)
        {
            return new AssetRef(Quantum.Editor.QuantumUnityDBUtilities.GetExpectedAssetGuid(new LazyLoadReference<Object>(asset), out _));
        }
    }
}
