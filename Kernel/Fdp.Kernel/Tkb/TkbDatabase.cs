using System;
using System.Collections.Generic;

namespace Fdp.Kernel.Tkb
{
    /// <summary>
    /// Manages entity templates (Transient Knowledge Base).
    /// Used to spawn pre-configured entities.
    /// </summary>
    public class TkbDatabase
    {
        private readonly Dictionary<string, TkbTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a new template.
        /// </summary>
        public void AddTemplate(TkbTemplate template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            if (_templates.ContainsKey(template.Name))
            {
                throw new InvalidOperationException($"Template '{template.Name}' already exists.");
            }

            _templates[template.Name] = template;
        }

        /// <summary>
        /// Gets an existing template by name.
        /// </summary>
        public TkbTemplate GetTemplate(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Template name cannot be null or empty", nameof(name));

            if (!_templates.TryGetValue(name, out var template))
            {
                throw new KeyNotFoundException($"Template '{name}' not found in TKB Database.");
            }

            return template;
        }

        /// <summary>
        /// Spawns a new entity from the specified template.
        /// </summary>
        public Entity Spawn(string templateName, EntityRepository repo)
        {
            if (repo == null)
                throw new ArgumentNullException(nameof(repo));

            var template = GetTemplate(templateName);
            
            // Create the entity
            var entity = repo.CreateEntity();
            
            try
            {
                // Apply template components
                template.ApplyTo(repo, entity);
            }
            catch
            {
                // If applying fails (e.g. factory error), we should probably cleanup the partial entity
                // but for now we let the exception bubble up.
                // repo.DestroyEntity(entity); // Optional safety
                throw;
            }

            return entity;
        }
        
        /// <summary>
        /// Clears all templates.
        /// </summary>
        public void Clear()
        {
            _templates.Clear();
        }
    }
}
