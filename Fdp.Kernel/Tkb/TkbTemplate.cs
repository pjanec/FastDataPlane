using System;
using System.Collections.Generic;

namespace Fdp.Kernel.Tkb
{
    /// <summary>
    /// Represents a blueprint for spawning entities.
    /// Contains a list of components to apply to the new entity.
    /// </summary>
    public class TkbTemplate
    {
        /// <summary>
        /// Unique identifier for this template.
        /// </summary>
        public string Name { get; }

        // We use delegates to abstract the type-specific SetComponent calls.
        private readonly List<Action<EntityRepository, Entity>> _applicators = new();

        public TkbTemplate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            
            Name = name;
        }

        /// <summary>
        /// Adds an unmanaged component to the template.
        /// The value is copied when adding, and copied again when spawning.
        /// </summary>
        public void AddComponent<T>(T component) where T : unmanaged
        {
            _applicators.Add((repo, entity) =>
            {
                repo.SetComponent(entity, component);
            });
        }

        /// <summary>
        /// Adds a managed component using a factory function.
        /// The factory is called each time an entity is spawned, ensuring a fresh instance.
        /// </summary>
        public void AddManagedComponent<T>(Func<T> factory) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _applicators.Add((repo, entity) =>
            {
                var instance = factory();
                repo.SetManagedComponent(entity, instance);
            });
        }

        /// <summary>
        /// Applies all components in this template to the target entity.
        /// </summary>
        public void ApplyTo(EntityRepository repo, Entity entity)
        {
            foreach (var apply in _applicators)
            {
                apply(repo, entity);
            }
        }
    }
}
