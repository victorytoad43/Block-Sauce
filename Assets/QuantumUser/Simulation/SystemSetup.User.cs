namespace Quantum
{
    using System;
    using System.Collections.Generic;

    public static partial class DeterministicSystemSetup
    {
        /// <summary>
        /// Adds Block Sauce's gameplay systems after the core systems from the SystemsConfig, in update order:
        /// input and camera first, then grounding, then the movement that reads both.
        /// </summary>
        static partial void AddSystemsUser(ICollection<SystemBase> systems, RuntimeConfig gameConfig, SimulationConfig simulationConfig, SystemsConfig systemsConfig)
        {
            systems.Add(new PlayerSystem());
            systems.Add(new PlayerLookSystem());
            systems.Add(new GroundedTrackerSystem());
            systems.Add(new MovementSystem());
        }
    }
}
