using Fdp.Kernel;
using System.Collections.Generic;
using System.Linq;

namespace Fdp.Examples.CarKinem.Core
{
    public class SelectionManager
    {
        private readonly HashSet<Entity> _selectedEntities = new();
        public event System.Action? SelectionChanged;

        public IReadOnlyCollection<Entity> SelectedEntities => _selectedEntities;
        
        public Entity? SelectedEntity => _selectedEntities.Count == 1 ? _selectedEntities.First() : null;
        public Entity? HoveredEntity { get; set; }

        public void Clear()
        {
            if (_selectedEntities.Count > 0)
            {
                _selectedEntities.Clear();
                SelectionChanged?.Invoke();
            }
        }

        public void Select(Entity entity, bool additive = false)
        {
            bool changed = false;
            if (!additive)
            {
                if (_selectedEntities.Count != 1 || !_selectedEntities.Contains(entity))
                {
                    _selectedEntities.Clear();
                    _selectedEntities.Add(entity);
                    changed = true;
                }
            }
            else
            {
                if (_selectedEntities.Add(entity)) changed = true;
            }
            if (changed) SelectionChanged?.Invoke();
        }

        public void Deselect(Entity entity)
        {
             if (_selectedEntities.Remove(entity)) SelectionChanged?.Invoke();
        }

        public void SetSelection(IEnumerable<Entity> entities)
        {
            _selectedEntities.Clear();
            foreach (var e in entities) _selectedEntities.Add(e);
            SelectionChanged?.Invoke();
        }

        public bool IsSelected(Entity entity) => _selectedEntities.Contains(entity);
    }
}
