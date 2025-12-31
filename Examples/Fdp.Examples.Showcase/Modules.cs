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
