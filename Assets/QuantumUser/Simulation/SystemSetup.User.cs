namespace Quantum
{
    using System;
    using System.Collections.Generic;

    public static partial class DeterministicSystemSetup
    {
        /// <summary>
        /// Adds Block Sauce's gameplay systems after the core systems from the SystemsConfig, in update order:
        /// input and camera first, then grounding, then the movement that reads both, then grabbing (which can turn
        /// the body while swinging). Signal-only systems react to physics callbacks and other systems' signals.
        /// </summary>
        static partial void AddSystemsUser(ICollection<SystemBase> systems, RuntimeConfig gameConfig, SimulationConfig simulationConfig, SystemsConfig systemsConfig)
        {
            systems.Add(new MatchSystem());
            systems.Add(new PlayerSystem());
            systems.Add(new PlayerLookSystem());
            systems.Add(new GroundedTrackerSystem());
            systems.Add(new MovementSystem());
            systems.Add(new GrabSystem());
            systems.Add(new GrabJointCleanupSystem());
            systems.Add(new GrabbableCollisionSystem());
            systems.Add(new KnockbackSystem());
            systems.Add(new ObjectImpactSystem());
            systems.Add(new BlockLockSystem());

            // Test-only: does nothing unless a scene contains a RagdollStressTest entity.
            systems.Add(new RagdollStressTestSystem());
            // Test-only: acts on dev commands in debug builds only.
            systems.Add(new DevCommandSystem());
        }
    }
}
