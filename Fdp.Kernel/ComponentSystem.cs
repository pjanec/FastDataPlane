using System;

namespace Fdp.Kernel
{
    /// <summary>
    /// Base class for all systems in the FDP engine.
    /// Systems contain game logic and are executed by SystemGroups.
    /// </summary>
    public abstract class ComponentSystem : IDisposable
    {
        /// <summary>
        /// Reference to the EntityRepository (World).
        /// Set automatically by the system group when the system is added.
        /// </summary>
        public EntityRepository World { get; internal set; }
        
        /// <summary>
        /// Whether this system is enabled and should execute during Update.
        /// Disabled systems are skipped.
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        private bool _created = false;
        private bool _disposed = false;
        
        /// <summary>
        /// Internal initialization called by SystemGroup.
        /// DO NOT call this directly.
        /// </summary>
        internal void InternalCreate(EntityRepository world)
        {
            if (_created)
            {
                throw new InvalidOperationException($"System {GetType().Name} already created");
            }
            
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }
            
            World = world;
            _created = true;
            OnCreate();
        }
        
        /// <summary>
        /// Internal update called by SystemGroup.
        /// DO NOT call this directly.
        /// </summary>
        internal void InternalUpdate()
        {
            if (!_created)
            {
                throw new InvalidOperationException($"System {GetType().Name} not created. Call InternalCreate first.");
            }
            
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            
            if (Enabled)
            {
                OnUpdate();
            }
        }
        
        /// <summary>
        /// Internal cleanup called by SystemGroup.
        /// DO NOT call this directly.
        /// </summary>
        internal void InternalDestroy()
        {
            if (_disposed)
            {
                return;
            }
            
            _disposed = true;
            OnDestroy();
        }
        
        /// <summary>
        /// Called once when the system is created.
        /// Use this to initialize queries, allocate resources, etc.
        /// </summary>
        protected virtual void OnCreate() { }
        
        /// <summary>
        /// Called every frame if the system is enabled.
        /// This is where your game logic goes.
        /// </summary>
        protected abstract void OnUpdate();
        
        /// <summary>
        /// Called when the system is destroyed.
        /// Use this to cleanup resources.
        /// </summary>
        protected virtual void OnDestroy() { }
        
        /// <summary>
        /// Disposes this system.
        /// </summary>
        public void Dispose()
        {
            InternalDestroy();
            GC.SuppressFinalize(this);
        }
    }
}
