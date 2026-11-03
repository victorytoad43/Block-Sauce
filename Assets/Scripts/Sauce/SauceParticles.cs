using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// Builds the placeholder sauce particle systems in code (A-009 placeholders): a coated block's bubbles (Stack) or
    /// glints (Slip) and the splash when a block goes into a vat.
    /// </summary>
    public static class SauceParticles
    {
        /// <summary>
        /// Rising bubbles that swell and pop, emitted over a box: the Stack sauce cue.
        /// </summary>
        public static ParticleSystem CreateBubbles(Transform parent, Material material)
        {
            ParticleSystem system = Create("Bubbles", parent, material);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = Vector3.one * 0.85f;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.3f), new Keyframe(0.85f, 1f), new Keyframe(1f, 1.4f)));
            return system;
        }

        /// <summary>
        /// Brief bright flecks that flash on and off over a box: the Slip sauce cue.
        /// </summary>
        public static ParticleSystem CreateGlints(Transform parent, Material material)
        {
            ParticleSystem system = Create("Glints", parent, material);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = Vector3.one * 0.86f;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
            return system;
        }

        /// <summary>
        /// A burst of droplets thrown up and out, for <see cref="Burst"/> to fire.
        /// </summary>
        public static ParticleSystem CreateSplash(Transform parent, Material material)
        {
            ParticleSystem system = Create("Splash", parent, material);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.gravityModifier = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            return system;
        }

        /// <summary>
        /// Emits <paramref name="count"/> particles at <paramref name="position"/> in <paramref name="color"/>.
        /// </summary>
        public static void Burst(ParticleSystem system, Vector3 position, Color color, int count, float speed = -1f)
        {
            system.transform.position = position;
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;
            if (speed >= 0f)
            {
                main.startSpeed = speed;
            }

            system.Emit(count);
        }

        private static ParticleSystem Create(string name, Transform parent, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.maxParticles = 200;
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return system;
        }
    }
}
