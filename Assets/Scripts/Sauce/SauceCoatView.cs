using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// On a block's view: draws its sauce coat. A shell over the block's mesh (PH_SauceCoat) is fed each dip's plane,
    /// converted from the block's simulation space into the mesh's object space, with the sauce's color and the time
    /// since the dip ended, which grows the drips. The entity index seeds the coat's noise, so the coat looks the same
    /// on every client. A coated block also gives off its sauce's particle cue, more of it the stronger the sauce.
    /// View only.
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
        private static readonly int CenterId = Shader.PropertyToID("_CenterOS");

        [Tooltip("The block's mesh renderer; the coat is a shell over the same mesh.")]
        [SerializeField] private MeshRenderer _blockRenderer;
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
            Transform mesh = _blockRenderer.transform;
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

            MeshFilter source = _blockRenderer.GetComponent<MeshFilter>();
            GameObject shell = new GameObject("SauceCoat");
            shell.transform.SetParent(_blockRenderer.transform, false);
            shell.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            _coat = shell.AddComponent<MeshRenderer>();
            _coat.sharedMaterial = _coatMaterial;
            _coat.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _properties = new MaterialPropertyBlock();
            _properties.SetFloat(SeedId, EntityRef.Index % 997);
            _properties.SetVector(CenterId, source.sharedMesh.bounds.center);

            if (_looks != null && _looks.ParticleMaterial != null)
            {
                _bubbles = SauceParticles.CreateBubbles(transform, _looks.ParticleMaterial);
                _glints = SauceParticles.CreateGlints(transform, _looks.ParticleMaterial);
                SetColor(_bubbles, _looks.GetParticleColor(SauceType.Stack));
                SetColor(_glints, _looks.GetParticleColor(SauceType.Slip));
            }
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
