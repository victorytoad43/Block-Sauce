using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a block's view: draws its sauce coat. The coat is a shell (PH_SauceCoat) over an evenly divided rounded box the
    /// size of the block's mesh, shared by every block, so the shader has the vertices to give the sauce lumpy volume
    /// however few the block's own mesh has. It is fed each dip's plane, converted from the block's simulation space into
    /// the mesh's object space, with the sauce's color and the time since the dip ended. The entity index seeds the
    /// coat's noise, so the coat looks the same on every client. A coated block also gives off its sauce's particle cue,
    /// more of it the stronger the sauce. View only.
    /// </summary>
    public class SauceCoatView : QuantumEntityViewComponent
    {
        private const int MaxDips = 3;
        private static readonly int[] DipIds =
        {
            Shader.PropertyToID("_Dip0"), Shader.PropertyToID("_Dip1"), Shader.PropertyToID("_Dip2"),
        };
        private static readonly int[] DipColorIds =
        {
            Shader.PropertyToID("_DipColor0"), Shader.PropertyToID("_DipColor1"), Shader.PropertyToID("_DipColor2"),
        };
        private static readonly int DipAgeId = Shader.PropertyToID("_DipAge");
        private static readonly int DipCountId = Shader.PropertyToID("_DipCount");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly System.Collections.Generic.Dictionary<(Vector3, int), Mesh> ShellMeshes = new();

        [Tooltip("The block's mesh renderer; the coat is a shell the size of its mesh.")]
        [SerializeField] private MeshRenderer _blockRenderer;
        [Tooltip("Rounding of the coat's edges, in meters: rounder than the block's own, so sauce softens its edges.")]
        [SerializeField] private float _shellRounding = 0.04f;
        [Tooltip("Quads along each edge of each face of the coat's shell.")]
        [SerializeField] private int _shellDivisions = 28;
        [Tooltip("The coat material (PH_SauceCoat).")]
        [SerializeField] private Material _coatMaterial;
        [Tooltip("The sauces' colors and particle material.")]
        [SerializeField] private SauceLooks _looks;
        [Tooltip("Bubbles (Stack) and glints (Slip) per second at full strength.")]
        [SerializeField] private float _bubblesPerSecond = 14f;
        [SerializeField] private float _glintsPerSecond = 10f;

        private MeshRenderer _coat;
        private MaterialPropertyBlock _properties;
        private ParticleSystem _bubbles;
        private ParticleSystem _glints;
        private readonly float[] _dipEndTimes = new float[MaxDips];

        public override void OnActivate(Frame frame)
        {
            for (int i = 0; i < MaxDips; i++)
            {
                // A coat already there when this view appears (joining late) is fully grown.
                _dipEndTimes[i] = -100f;
            }

            QuantumEvent.Subscribe(this, (EventBlockDipEnded e) =>
            {
                if (e.Block == EntityRef && e.Dip < MaxDips)
                {
                    _dipEndTimes[e.Dip] = Time.time;
                }
            });
        }

        public override void OnDeactivate()
        {
            QuantumEvent.UnsubscribeListener(this);
            if (_coat != null)
            {
                _coat.enabled = false;
            }

            SetRate(_bubbles, 0f);
            SetRate(_glints, 0f);
        }

        public override void OnUpdateView()
        {
            if (_blockRenderer == null || TryGetPredictedQuantumComponent(out SauceCoat coat) == false || coat.DipCount == 0)
            {
                if (_coat != null)
                {
                    _coat.enabled = false;
                }

                SetRate(_bubbles, 0f);
                SetRate(_glints, 0f);
                return;
            }

            EnsureCoat();
            _coat.enabled = true;
            UpdateCoat(coat);

            SetRate(_bubbles, coat.StackStrength.AsFloat * _bubblesPerSecond);
            SetRate(_glints, coat.SlipStrength.AsFloat * _glintsPerSecond);
        }

        /// <summary>
        /// Feeds the coat shader each dip's plane in the mesh's object space, its color and its age.
        /// </summary>
        private void UpdateCoat(SauceCoat coat)
        {
            Transform mesh = _coat.transform;
            Vector4 ages = Vector4.zero;
            for (int i = 0; i < MaxDips; i++)
            {
                if (i >= coat.DipCount)
                {
                    _properties.SetVector(DipIds[i], new Vector4(0f, 1f, 0f, -100f));
                    continue;
                }

                SauceDip dip = coat.Dips[i];
                // The simulation's plane is about the block's center in its own frame, which the view's root follows.
                Vector3 normal = dip.Normal.ToUnityVector3();
                Vector3 pointWorld = transform.TransformPoint(normal * dip.Offset.AsFloat);
                Vector3 normalObject = mesh.InverseTransformDirection(transform.TransformDirection(normal)).normalized;
                float offsetObject = Vector3.Dot(normalObject, mesh.InverseTransformPoint(pointWorld));
                _properties.SetVector(DipIds[i], new Vector4(normalObject.x, normalObject.y, normalObject.z, offsetObject));
                _properties.SetColor(DipColorIds[i], _looks != null ? _looks.GetColor(dip.Type) : Color.gray);

                bool dipping = coat.InVat != EntityRef.None && coat.ActiveDip == i;
                ages[i] = dipping ? 0f : Time.time - _dipEndTimes[i];
            }

            _properties.SetVector(DipAgeId, ages);
            _properties.SetFloat(DipCountId, coat.DipCount);
            _coat.SetPropertyBlock(_properties);
        }

        private void EnsureCoat()
        {
            if (_coat != null)
                return;

            Bounds bounds = _blockRenderer.GetComponent<MeshFilter>().sharedMesh.bounds;
            GameObject shell = new GameObject("SauceCoat");
            shell.transform.SetParent(_blockRenderer.transform, false);
            shell.transform.localPosition = bounds.center;
            shell.AddComponent<MeshFilter>().sharedMesh = GetShellMesh(bounds.extents);
            _coat = shell.AddComponent<MeshRenderer>();
            _coat.sharedMaterial = _coatMaterial;
            _coat.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _properties = new MaterialPropertyBlock();
            _properties.SetFloat(SeedId, EntityRef.Index % 997);

            if (_looks != null && _looks.ParticleMaterial != null)
            {
                _bubbles = SauceParticles.CreateBubbles(transform, _looks.ParticleMaterial);
                _glints = SauceParticles.CreateGlints(transform, _looks.ParticleMaterial);
                SetColor(_bubbles, _looks.GetParticleColor(SauceType.Stack));
                SetColor(_glints, _looks.GetParticleColor(SauceType.Slip));
            }
        }

        /// <summary>
        /// Returns the coat's shell for a block of these half extents: a rounded box, each face an even grid, with
        /// normals that turn smoothly over the rounded edges so the coat can be pushed out along them without cracks.
        /// </summary>
        private Mesh GetShellMesh(Vector3 extents)
        {
            int divisions = Mathf.Max(2, _shellDivisions);
            if (ShellMeshes.TryGetValue((extents, divisions), out Mesh mesh))
                return mesh;

            float rounding = Mathf.Clamp(_shellRounding, 0.001f, Mathf.Min(extents.x, Mathf.Min(extents.y, extents.z)));
            Vector3 inner = extents - Vector3.one * rounding;
            int side = divisions + 1;
            var vertices = new Vector3[6 * side * side];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[6 * divisions * divisions * 6];
            Vector3[] faceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            int v = 0, t = 0;
            foreach (Vector3 normal in faceNormals)
            {
                Vector3 u = new Vector3(normal.y, normal.z, normal.x);
                Vector3 w = Vector3.Cross(normal, u);
                int first = v;
                for (int i = 0; i < side; i++)
                {
                    for (int j = 0; j < side; j++)
                    {
                        Vector3 onCube = normal + u * (2f * i / divisions - 1f) + w * (2f * j / divisions - 1f);
                        Vector3 point = Vector3.Scale(onCube, extents);
                        Vector3 core = Vector3.Max(-inner, Vector3.Min(inner, point));
                        Vector3 outward = (point - core).normalized;
                        vertices[v] = core + outward * rounding;
                        normals[v] = outward;
                        v++;
                    }
                }

                for (int i = 0; i < divisions; i++)
                {
                    for (int j = 0; j < divisions; j++)
                    {
                        int a = first + i * side + j;
                        triangles[t++] = a;
                        triangles[t++] = a + side;
                        triangles[t++] = a + 1;
                        triangles[t++] = a + 1;
                        triangles[t++] = a + side;
                        triangles[t++] = a + side + 1;
                    }
                }
            }

            mesh = new Mesh { name = "SauceCoatShell", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            // The shader pushes the shell out by up to several centimeters.
            mesh.bounds = new Bounds(Vector3.zero, extents * 2f + Vector3.one * 0.2f);
            ShellMeshes[(extents, divisions)] = mesh;
            return mesh;
        }

        private static void SetRate(ParticleSystem system, float rate)
        {
            if (system == null)
                return;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
            if (rate > 0f && system.isPlaying == false)
            {
                system.Play();
            }
        }

        private static void SetColor(ParticleSystem system, Color color)
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;
        }
    }
}
