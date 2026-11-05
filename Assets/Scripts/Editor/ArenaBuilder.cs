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
    /// <item>Unity mesh colliders on the CameraBlock layer, which only the orbit camera sweeps against, and from which
    /// the navmesh is baked (Unity's bake, imported into the Quantum map for the bots' pathfinding).</item>
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
            List<Vector3> stairCenters = AddStairColliders(arena.transform, stairs);
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
            MeshCollider pillarProbe = probes[2];
            List<Vector3> pillarCenters = FindPillarCenters(pillars)
                .Select(rough => MeasurePillarTop(pillarProbe, rough, out _)).ToList();
            MeasurePillarTop(pillarProbe, pillarCenters[0], out float pillarRadius);
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

            AddNavLinks(arena.transform, probes, pillarCenters, pillarRadius, ringInner, ringOuter, stairCenters);
            foreach (MeshCollider probe in probes)
            {
                Object.DestroyImmediate(probe);
            }

            Object.DestroyImmediate(spawnTemplate);
            SetUpMapAndMode(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            SetUpNavMesh();
            BakeNavMesh();
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

        /// <summary>
        /// The navmesh's walker: Guy's capsule (0.4 m radius), standing height, the slope he walks up, and a climb just
        /// under his 0.5 m step, so the pillar tops and the inner ring are areas of their own, joined to the ground only
        /// by the links from <see cref="AddNavLinks"/> (where bots hop up or down).
        /// </summary>
        private const float AgentRadius = 0.4f, AgentHeight = 1.8f, AgentSlope = 35f, AgentClimb = 0.45f;

        /// <summary>
        /// Block Sauce › Bake Arena NavMesh: rebakes the arena's navmesh (Unity's bake of the arena's CameraBlock
        /// colliders, imported by Quantum into the map) and the map, after a hand change to the arena scene.
        /// </summary>
        [MenuItem("Block Sauce/Bake Arena NavMesh")]
        public static void BakeNavMeshMenu()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(ArenaPath);
            SetUpNavMesh();
            BakeNavMesh();
        }

        /// <summary>
        /// Puts the navmesh surface and Quantum's importer under the map data (Quantum only imports navmeshes parented
        /// there) and sets Unity's default agent to Guy's size.
        /// </summary>
        private static void SetUpNavMesh()
        {
            var agents = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0]);
            SerializedProperty agent = agents.FindProperty("m_Settings").GetArrayElementAtIndex(0);
            agent.FindPropertyRelative("agentRadius").floatValue = AgentRadius;
            agent.FindPropertyRelative("agentHeight").floatValue = AgentHeight;
            agent.FindPropertyRelative("agentSlope").floatValue = AgentSlope;
            agent.FindPropertyRelative("agentClimb").floatValue = AgentClimb;
            agents.ApplyModifiedPropertiesWithoutUndo();

            var mapData = Object.FindAnyObjectByType<QuantumMapData>();
            Transform existing = mapData.transform.Find(NavMeshName);
            if (existing != null)
            {
                // Unity's bake data from last time, saved beside the scene; the next bake saves a fresh one.
                var oldSurface = existing.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
                string oldData = oldSurface != null && oldSurface.navMeshData != null
                    ? AssetDatabase.GetAssetPath(oldSurface.navMeshData) : null;
                Object.DestroyImmediate(existing.gameObject);
                if (string.IsNullOrEmpty(oldData) == false)
                {
                    AssetDatabase.DeleteAsset(oldData);
                }
            }

            var host = new GameObject(NavMeshName);
            host.transform.SetParent(mapData.transform, false);
            var surface = host.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << CameraBlockLayer;
            var importer = host.AddComponent<QuantumMapNavMeshUnity>();
            importer.NavMeshSurfaces = new[] { host };
            importer.Settings = new QuantumNavMesh.ImportSettings { LinkErrorCorrection = 0.6f };

            // The top of the wall isn't anywhere to go.
            Transform wall = GameObject.Find("Arena").transform.Find("exterior_wall/CameraCollider");
            var skip = wall.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
            if (skip == null)
            {
                skip = wall.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            }

            skip.ignoreFromBuild = true;
            EditorSceneManager.MarkSceneDirty(host.scene);
        }

        /// <summary>
        /// Bakes Unity's navmesh, then the Quantum map with the navmesh imported into it, and saves the scene.
        /// </summary>
        private static void BakeNavMesh()
        {
            var surface = Object.FindAnyObjectByType<Unity.AI.Navigation.NavMeshSurface>();
            surface.BuildNavMesh();
            UnityEngine.SceneManagement.Scene scene = surface.gameObject.scene;
            EditorSceneManager.SaveScene(scene);
            Quantum.Editor.QuantumEditorAutoBaker.BakeCurrentScene_ImportNavMesh();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// The navmesh's name in the map; the simulation looks it up by this name (<c>BotNavigation</c>).
        /// </summary>
        public const string NavMeshName = "ArenaNavMesh";

        /// <summary>
        /// Jump links, both ways, between the ground and the ledges a bot has to hop: round the inner ring's outer and
        /// inner edges every 20°, round each pillar's rim every 45° (skipping the stair side and the side facing the middle, where the
        /// home block and tower stand; the top ends sit just inside the rim, clear of the tower), and from the top of each
        /// stair onto its pillar (the top step is just over Guy's step). Named <c>Jump</c>; the simulation jumps on any
        /// link.
        /// </summary>
        private static void AddNavLinks(Transform arena, List<MeshCollider> probes, List<Vector3> pillars, float pillarRadius,
            float ringInner, float ringOuter, List<Vector3> stairs)
        {
            var root = new GameObject("NavLinks").transform;
            root.SetParent(arena, false);
            void Link(Vector3 from, Vector3 to)
            {
                var link = new GameObject("Jump").AddComponent<Unity.AI.Navigation.NavMeshLink>();
                link.transform.SetParent(root, false);
                link.agentTypeID = 0;
                link.bidirectional = true;
                link.width = 0f;
                link.startPoint = new Vector3(from.x, SurfaceY(from, probes), from.z);
                link.endPoint = new Vector3(to.x, SurfaceY(to, probes), to.z);
            }

            const float reach = 0.9f;
            for (float angle = 0f; angle < 360f; angle += 20f)
            {
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Link(direction * (ringOuter + reach), direction * (ringOuter - reach));
                Link(direction * (ringInner - reach), direction * (ringInner + reach));
            }

            for (int i = 0; i < pillars.Count; i++)
            {
                Vector3 center = Flat(pillars[i]);
                Vector3 toStair = Flat(stairs.OrderBy(s => (Flat(s) - center).sqrMagnitude).First()) - center;
                for (float angle = 0f; angle < 360f; angle += 45f)
                {
                    Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    // Not over the stair, and not on the side facing the arena's middle, where the home block (and
                    // so the player's tower) stands.
                    if (Vector3.Angle(direction, toStair) < 35f || Vector3.Angle(direction, -center) < 35f)
                        continue;

                    Link(center + direction * (pillarRadius + reach), center + direction * (pillarRadius - 0.5f));
                }

                // The stair's top end: from the stair's middle onto the pillar.
                Vector3 stairDirection = toStair.normalized;
                Link(center + toStair, center + stairDirection * (pillarRadius - 0.5f));
            }
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
        private static List<Vector3> AddStairColliders(Transform arena, Transform stairs)
        {
            GameObject pieces = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PiecesModel));
            try
            {
                Transform meshStair = pieces.transform.Find("MESH_stairs");
                Transform colStair = pieces.transform.Find("COL_stairs");
                Vector3[] model = WorldVertices(meshStair);
                Vector3 modelCenter = Centroid(model);

                Vector3[] all = WorldVertices(stairs);
                var centers = new List<Vector3>();
                var root = new GameObject("StairColliders").transform;
                root.SetParent(arena, false);
                foreach (Vector3[] cluster in ClusterByAngle(all, Slots))
                {
                    Vector3 target = Centroid(cluster);
                    centers.Add(target);
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

                return centers;
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
        /// The centers of the six pillar tops, ordered round the ring (clockwise seen from above, from +Z).
        /// </summary>
        private static List<Vector3> FindPillarCenters(Transform pillars)
        {
            return ClusterByAngle(WorldVertices(pillars), Slots)
                .Select(cluster =>
                {
                    // The middle of the pillar's bounds: an average of its vertices leans toward where they bunch
                    // up (round the stair's cutout in the rim).
                    float top = cluster.Max(v => v.y);
                    float x = (cluster.Min(v => v.x) + cluster.Max(v => v.x)) * 0.5f;
                    float z = (cluster.Min(v => v.z) + cluster.Max(v => v.z)) * 0.5f;
                    return new Vector3(x, top, z);
                })
                .ToList();
        }

        /// <summary>
        /// Finds a pillar's standing surface by looking down on a grid round <paramref name="rough"/> (the pillar mesh
        /// reaches out past its top, so its vertices only give a rough place): the middle of the surface, at its height,
        /// and its radius (from its area, so inside the rim).
        /// </summary>
        private static Vector3 MeasurePillarTop(MeshCollider pillar, Vector3 rough, out float radius)
        {
            const float step = 0.1f, span = 4f;
            float top = float.MinValue;
            var hits = new List<Vector3>();
            for (float x = -span; x <= span; x += step)
            {
                for (float z = -span; z <= span; z += step)
                {
                    var ray = new Ray(new Vector3(rough.x + x, 50f, rough.z + z), Vector3.down);
                    if (pillar.Raycast(ray, out RaycastHit hit, 100f))
                    {
                        hits.Add(hit.point);
                        top = Mathf.Max(top, hit.point.y);
                    }
                }
            }

            // The standing surface: everything within 10 cm of the rim's top.
            Vector3[] surface = hits.Where(h => h.y > top - 0.1f).ToArray();
            Vector3 center = Centroid(surface);
            radius = Mathf.Sqrt(surface.Length * step * step / Mathf.PI);
            float floor = surface.Min(h => h.y);
            return new Vector3(center.x, floor, center.z);
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
