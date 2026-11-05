using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Block Sauce › Build Arena: turns Jackson's graybox (<c>EN_Arena.fbx</c>, with <c>COL_stairs</c> from
    /// <c>EN_ArenaPieces.fbx</c>) into the <c>Arena</c> scene, where matches are played. The scene starts as a copy of
    /// the Lobby, so the menu, HUD, bots, camera and dev keys come along; this replaces the Lobby's terrain, vats,
    /// spawn points and blocks with the arena's:
    /// <list type="bullet">
    /// <item>Quantum static mesh colliders on the ground, wall, inner ring and pillars; ramps (<c>COL_stairs</c>) on
    /// the stairs, fitted to each stair's position.</item>
    /// <item>A player spawn on each of the six pillars, facing the center, numbered round the ring.</item>
    /// <item>Block markers (<see cref="BlockSpawnPoint"/>): each player's home block on their pillar in front of them,
    /// one on the inner ring across the center from them, and one in a jumbled pile of six in the center.</item>
    /// <item>Six sandbox blocks in the center pile for the room's free play, and the arena's own map and game mode.</item>
    /// <item>Unity mesh colliders on the CameraBlock layer, which only the orbit camera sweeps against.</item>
    /// </list>
    /// Rerun it after a re-export of the graybox; it rebuilds everything it owns.
    /// </summary>
    public static class ArenaBuilder
    {
        private const string ArenaPath = "Assets/Scenes/Arena.unity";
        private const string LobbyPath = "Assets/Scenes/Lobby.unity";
        private const string MapPath = "Assets/Scenes/Arena/ArenaMap.asset";
        private const string ModePath = "Assets/QuantumUser/Resources/Configs/StackAndLockMode_Arena.asset";
        private const string ArenaModel = "Assets/Art/Environment/EN_Arena.fbx";
        private const string PiecesModel = "Assets/Art/Environment/EN_ArenaPieces.fbx";
        private const int Slots = 6;
        private const float BlockHalf = 0.4f;

        [MenuItem("Block Sauce/Build Arena")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath) == null)
            {
                AssetDatabase.CopyAsset(LobbyPath, ArenaPath);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(ArenaPath);
            GameObject spawnTemplate = Object.Instantiate(GameObject.Find("SpawnPoints").transform.GetChild(0).gameObject);
            spawnTemplate.name = "Template";

            foreach (string name in new[] { "PH_Terrain", "PH_SauceVats", "Arena", "SpawnPoints", "BlockSpawnPoints" })
            {
                GameObject existing = GameObject.Find(name);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
            }

            // Sandbox blocks: six stay (moved to the center pile below), the rest go.
            Transform sandbox = GameObject.Find("PH_Blocks").transform;
            for (int i = sandbox.childCount - 1; i >= Slots; i--)
            {
                Object.DestroyImmediate(sandbox.GetChild(i).gameObject);
            }

            GameObject arena = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ArenaModel), scene);
            arena.name = "Arena";
            Transform ground = arena.transform.Find("displace_ground");
            Transform wall = arena.transform.Find("exterior_wall");
            Transform ring = arena.transform.Find("inner_circle");
            Transform pillars = arena.transform.Find("spawn_pillars");
            Transform stairs = pillars.Find("spawn_pillar_stairs");

            AddMeshCollider(ground, true);
            AddMeshCollider(wall, false);
            AddMeshCollider(ring, false);
            AddMeshCollider(pillars, false);
            AddStairColliders(arena.transform, stairs);
            foreach (Transform part in new[] { ground, wall, ring, pillars, stairs })
            {
                AddCameraCollider(part);
            }

            // Temporary Unity colliders to measure the surfaces.
            var probes = new List<MeshCollider>();
            foreach (Transform part in new[] { ground, ring, pillars })
            {
                MeshCollider probe = part.gameObject.AddComponent<MeshCollider>();
                probe.sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
                probes.Add(probe);
            }

            Physics.SyncTransforms();
            List<Vector3> pillarCenters = FindPillarCenters(pillars);
            float pillarRadius = MeasurePillarRadius(pillars, pillarCenters);
            (float ringInner, float ringOuter) = MeasureRing(ring);

            var spawnRoot = new GameObject("SpawnPoints").transform;
            var markerRoot = new GameObject("BlockSpawnPoints").transform;
            List<Vector3> pile = PilePositions(probes);
            for (int slot = 0; slot < Slots; slot++)
            {
                Vector3 center = pillarCenters[slot];
                Vector3 inward = -new Vector3(center.x, 0f, center.z).normalized;
                Quaternion facing = Quaternion.LookRotation(inward, Vector3.up);
                float top = SurfaceY(center, probes);

                GameObject spawn = Object.Instantiate(spawnTemplate, spawnRoot);
                spawn.name = $"PlayerSpawnPoint_{slot}";
                spawn.transform.SetPositionAndRotation(new Vector3(center.x, top, center.z) - inward * (pillarRadius * 0.3f), facing);

                Vector3 home = center + inward * (pillarRadius * 0.45f);
                AddMarker(spawnTemplate, markerRoot, $"Home_{slot}", slot, true, Above(home, probes), facing);

                // Across the center, on the middle of the inner ring.
                Vector3 across = inward * ((ringInner + ringOuter) * 0.5f);
                AddMarker(spawnTemplate, markerRoot, $"Ring_{slot}", slot, false, Above(across, probes),
                    Quaternion.LookRotation(-inward, Vector3.up));

                AddMarker(spawnTemplate, markerRoot, $"Pile_{slot}", slot, false, pile[slot], PileRotation(slot));

                Transform block = sandbox.GetChild(slot);
                block.name = $"PH_Block_{slot}";
                block.SetPositionAndRotation(pile[slot], PileRotation(slot));
            }

            foreach (MeshCollider probe in probes)
            {
                Object.DestroyImmediate(probe);
            }

            Object.DestroyImmediate(spawnTemplate);
            SetUpMapAndMode(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Quantum.Editor.QuantumEditorAutoBaker.BakeCurrentScene_MapData();
            EditorSceneManager.SaveScene(scene);
            AddToBuild();
            Debug.Log($"Built {ArenaPath}: pillar radius {pillarRadius:F2} m, inner ring {ringInner:F2}–{ringOuter:F2} m.");
        }

        /// <summary>
        /// The layer the orbit camera can't see through (<see cref="OrbitCamera"/>).
        /// </summary>
        private const int CameraBlockLayer = 10;

        /// <summary>
        /// A Unity collider the orbit camera sweeps against, on its own child and layer so the Quantum collider on the
        /// part keeps its layer.
        /// </summary>
        private static void AddCameraCollider(Transform part)
        {
            NameLayer(CameraBlockLayer, "CameraBlock");
            var child = new GameObject("CameraCollider") { layer = CameraBlockLayer };
            child.transform.SetParent(part, false);
            child.AddComponent<MeshCollider>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
        }

        private static void NameLayer(int layer, string name)
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty slot = tags.FindProperty("layers").GetArrayElementAtIndex(layer);
            if (slot.stringValue == name)
                return;

            slot.stringValue = name;
            tags.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddMeshCollider(Transform part, bool smooth)
        {
            var collider = part.gameObject.AddComponent<QuantumStaticMeshCollider3D>();
            collider.Mesh = part.GetComponent<MeshFilter>().sharedMesh;
            collider.SmoothSphereMeshCollisions = smooth;
        }

        /// <summary>
        /// Places a <c>COL_stairs</c> ramp on each stair: the stairs in the arena are one merged mesh, so each stair's
        /// vertices are found by direction from the center and <c>MESH_stairs</c> (the same stair, alone at the origin)
        /// is fitted to them by turning it about the vertical and moving it; the ramp gets the same move.
        /// </summary>
        private static void AddStairColliders(Transform arena, Transform stairs)
        {
            GameObject pieces = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PiecesModel));
            try
            {
                Transform meshStair = pieces.transform.Find("MESH_stairs");
                Transform colStair = pieces.transform.Find("COL_stairs");
                Vector3[] model = WorldVertices(meshStair);
                Vector3 modelCenter = Centroid(model);

                Vector3[] all = WorldVertices(stairs);
                var root = new GameObject("StairColliders").transform;
                root.SetParent(arena, false);
                foreach (Vector3[] cluster in ClusterByAngle(all, Slots))
                {
                    Vector3 target = Centroid(cluster);
                    float bestYaw = 0f, bestError = float.MaxValue;
                    for (float yaw = 0f; yaw < 360f; yaw += 0.5f)
                    {
                        float error = FitError(model, modelCenter, target, yaw, cluster);
                        if (error < bestError)
                        {
                            bestError = error;
                            bestYaw = yaw;
                        }
                    }

                    if (bestError > 0.05f)
                    {
                        Debug.LogWarning($"Arena: a stair fits MESH_stairs only to {bestError:F3} m; check COL_stairs on it.");
                    }

                    Quaternion turn = Quaternion.Euler(0f, bestYaw, 0f);
                    var holder = new GameObject($"COL_stairs_{root.childCount}").transform;
                    holder.SetParent(root, false);
                    holder.SetPositionAndRotation(target - turn * modelCenter, turn);
                    var ramp = new GameObject("Ramp").transform;
                    ramp.SetParent(holder, false);
                    ramp.localPosition = colStair.position;
                    ramp.localRotation = colStair.rotation;
                    ramp.localScale = colStair.lossyScale;
                    var collider = ramp.gameObject.AddComponent<QuantumStaticMeshCollider3D>();
                    collider.Mesh = colStair.GetComponent<MeshFilter>().sharedMesh;
                }
            }
            finally
            {
                Object.DestroyImmediate(pieces);
            }
        }

        private static float FitError(Vector3[] model, Vector3 modelCenter, Vector3 target, float yaw, Vector3[] cluster)
        {
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            float total = 0f;
            foreach (Vector3 vertex in model)
            {
                Vector3 moved = target + turn * (vertex - modelCenter);
                float nearest = float.MaxValue;
                foreach (Vector3 other in cluster)
                {
                    nearest = Mathf.Min(nearest, (other - moved).sqrMagnitude);
                }

                total += Mathf.Sqrt(nearest);
            }

            return total / model.Length;
        }

        /// <summary>
        /// The centers of the six pillar tops, ordered round the ring (counter-clockwise seen from above, from +Z).
        /// </summary>
        private static List<Vector3> FindPillarCenters(Transform pillars)
        {
            return ClusterByAngle(WorldVertices(pillars), Slots)
                .Select(cluster =>
                {
                    float top = cluster.Max(v => v.y);
                    Vector3[] rim = cluster.Where(v => v.y > top - 0.05f).ToArray();
                    Vector3 center = Centroid(rim);
                    return new Vector3(center.x, top, center.z);
                })
                .ToList();
        }

        private static float MeasurePillarRadius(Transform pillars, List<Vector3> centers)
        {
            Vector3[] vertices = WorldVertices(pillars);
            Vector3 center = centers[0];
            return vertices.Where(v => Flat(v - center).magnitude < 4f).Max(v => Flat(v - center).magnitude);
        }

        private static (float inner, float outer) MeasureRing(Transform ring)
        {
            float[] radii = WorldVertices(ring).Select(v => Flat(v).magnitude).ToArray();
            return (radii.Min(), radii.Max());
        }

        /// <summary>
        /// Splits vertices into <paramref name="count"/> groups by their direction from the arena's center, ordered by
        /// angle; the groups are found from the widest gaps between vertex angles.
        /// </summary>
        private static List<Vector3[]> ClusterByAngle(Vector3[] vertices, int count)
        {
            float Angle(Vector3 v) => (Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg + 360f) % 360f;
            Vector3[] sorted = vertices.OrderBy(Angle).ToArray();
            var gaps = new List<(float gap, int index)>();
            for (int i = 0; i < sorted.Length; i++)
            {
                float next = i + 1 < sorted.Length ? Angle(sorted[i + 1]) : Angle(sorted[0]) + 360f;
                gaps.Add((next - Angle(sorted[i]), i));
            }

            int[] cuts = gaps.OrderByDescending(g => g.gap).Take(count).Select(g => g.index).OrderBy(i => i).ToArray();
            var clusters = new List<Vector3[]>();
            for (int c = 0; c < count; c++)
            {
                int start = cuts[c] + 1;
                int end = c + 1 < count ? cuts[c + 1] : cuts[0] + sorted.Length;
                var cluster = new List<Vector3>();
                for (int i = start; i <= end; i++)
                {
                    cluster.Add(sorted[i % sorted.Length]);
                }

                clusters.Add(cluster.ToArray());
            }

            return clusters.OrderBy(c => Angle(Centroid(c))).ToList();
        }

        /// <summary>
        /// Six spots in the center for a jumbled pile: four on the ground, one on them, one on top.
        /// </summary>
        private static List<Vector3> PilePositions(List<MeshCollider> probes)
        {
            var result = new List<Vector3>();
            foreach (Vector2 spot in new[] { new Vector2(-0.62f, -0.6f), new Vector2(0.6f, -0.64f), new Vector2(-0.58f, 0.62f), new Vector2(0.64f, 0.58f) })
            {
                result.Add(Above(new Vector3(spot.x, 0f, spot.y), probes));
            }

            float layer = result.Max(p => p.y);
            result.Add(new Vector3(-0.1f, layer + BlockHalf * 2f + 0.15f, 0.05f));
            result.Add(new Vector3(0.25f, layer + BlockHalf * 4f + 0.35f, -0.15f));
            return result;
        }

        private static Quaternion PileRotation(int slot)
        {
            float[] yaws = { 12f, 47f, 71f, 28f, 63f, 5f };
            float[] tilts = { 0f, 0f, 0f, 0f, 22f, 35f };
            return Quaternion.Euler(tilts[slot], yaws[slot], tilts[slot] * 0.5f);
        }

        /// <summary>
        /// A block's center resting on the surface under <paramref name="point"/>, with a little drop.
        /// </summary>
        private static Vector3 Above(Vector3 point, List<MeshCollider> probes)
        {
            return new Vector3(point.x, SurfaceY(point, probes) + BlockHalf + 0.05f, point.z);
        }

        private static float SurfaceY(Vector3 point, List<MeshCollider> probes)
        {
            float best = float.MinValue;
            var ray = new Ray(new Vector3(point.x, 50f, point.z), Vector3.down);
            foreach (MeshCollider probe in probes)
            {
                if (probe.Raycast(ray, out RaycastHit hit, 100f))
                {
                    best = Mathf.Max(best, hit.point.y);
                }
            }

            return best;
        }

        private static void AddMarker(GameObject template, Transform parent, string name, int slot, bool home, Vector3 position, Quaternion rotation)
        {
            GameObject marker = Object.Instantiate(template, parent);
            marker.name = name;
            Object.DestroyImmediate(marker.GetComponent<QPrototypePlayerSpawnPoint>());
            var prototype = marker.AddComponent<QPrototypeBlockSpawnPoint>();
            prototype.Prototype = new Quantum.Prototypes.BlockSpawnPointPrototype { Slot = slot, Home = home };
            marker.transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// Gives the scene its own map asset (so the Lobby's stays as it is) and the arena's game mode (spawns as placed,
        /// not pushed out), and points the connection and debug runner at the map.
        /// </summary>
        private static void SetUpMapAndMode(UnityEngine.SceneManagement.Scene scene)
        {
            var map = AssetDatabase.LoadAssetAtPath<Map>(MapPath);
            if (map == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MapPath));
                map = ScriptableObject.CreateInstance<Map>();
                AssetDatabase.CreateAsset(map, MapPath);
            }

            var mode = AssetDatabase.LoadAssetAtPath<GameModeConfig>(ModePath);
            if (mode == null)
            {
                mode = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameModeConfig>(
                    "Assets/QuantumUser/Resources/Configs/StackAndLockMode.asset"));
                AssetDatabase.CreateAsset(mode, ModePath);
            }

            mode.SpawnRadiusScale = FP._1;
            EditorUtility.SetDirty(mode);
            AssetDatabase.SaveAssets();
            Quantum.Editor.QuantumUnityDBUtilities.RefreshGlobalDB();
            AssetRef mapRef = new AssetRef(Quantum.Editor.QuantumUnityDBUtilities.GetExpectedAssetGuid(new LazyLoadReference<Object>(map), out _));
            AssetRef modeRef = new AssetRef(Quantum.Editor.QuantumUnityDBUtilities.GetExpectedAssetGuid(new LazyLoadReference<Object>(mode), out _));

            var data = Object.FindAnyObjectByType<QuantumMapData>();
            data.AssetRef = new AssetRef<Map>(mapRef.Id);
            EditorUtility.SetDirty(data);

            var match = GameObject.Find("Match").GetComponent<QPrototypeMatch>();
            match.Prototype.Mode = new AssetRef<GameModeConfig>(modeRef.Id);
            EditorUtility.SetDirty(match);

            foreach (var connection in Object.FindObjectsByType<QuantumStartUIConnection>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                connection.RuntimeConfig.Map = data.AssetRef;
                connection.AppVersionMachineIdPostfix = "-Arena";
                EditorUtility.SetDirty(connection);
            }

            foreach (var runner in Object.FindObjectsByType<QuantumRunnerLocalDebug>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                runner.RuntimeConfig.Map = data.AssetRef;
                EditorUtility.SetDirty(runner);
            }
        }

        /// <summary>
        /// The arena is the game's first scene (the menu and matches); the Lobby stays in the build as a test scene.
        /// </summary>
        private static void AddToBuild()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ArenaPath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ArenaPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Vector3[] WorldVertices(Transform part)
        {
            Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
            Matrix4x4 matrix = part.localToWorldMatrix;
            return mesh.vertices.Select(v => matrix.MultiplyPoint3x4(v)).ToArray();
        }

        private static Vector3 Centroid(IReadOnlyCollection<Vector3> points)
        {
            Vector3 sum = Vector3.zero;
            foreach (Vector3 point in points)
            {
                sum += point;
            }

            return sum / points.Count;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
