using System;
using Fdp.Kernel;
using Fdp.Examples.Showcase.Components;

namespace Fdp.Examples.Showcase.Modules
{
    public interface IModule
    {
        void Load(EntityRepository repo);
    }

    public class PhysicsModule : IModule
    {
        public void Load(EntityRepository repo)
        {
            repo.RegisterComponent<Position>();
            repo.RegisterComponent<Velocity>();
            repo.RegisterComponent<Projectile>();
            repo.RegisterComponent<Particle>();
        }
    }

    public class CombatModule : IModule
    {
        public void Load(EntityRepository repo)
        {
            repo.RegisterComponent<UnitStats>();

			// Explicitly force snapshotting for this mutable class
			// WARNING: Only safe if you don't use background modules reading this component
			//          In this demo we are not creating any other snapshot but those for the FlightRecorder
			//          so using snapshotable=true for mutable class is safe.
			repo.RegisterComponent<CombatHistory>(snapshotable: true);

            repo.RegisterComponent<CombatState>();
            repo.RegisterComponent<Corpse>();
        }
    }

    public class RenderModule : IModule
    {
        public void Load(EntityRepository repo)
        {
            repo.RegisterComponent<RenderSymbol>();
            repo.RegisterComponent<HitFlash>();
        }
    }
}
