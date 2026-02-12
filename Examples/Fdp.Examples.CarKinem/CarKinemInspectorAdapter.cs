using Fdp.Kernel;
using FDP.Toolkit.ImGui.Abstractions;
using FDP.Toolkit.Vis2D.Abstractions;
using Fdp.Examples.CarKinem.Core;
using System.Collections.Generic;

namespace Fdp.Examples.CarKinem
{
    public class CarKinemInspectorAdapter : IInspectorContext, ISelectionState
    {
        private readonly SelectionManager _selectionManager;
        private readonly EntityRepository _repository; 
        
        public CarKinemInspectorAdapter(SelectionManager selectionManager, EntityRepository repository)
        {
            _selectionManager = selectionManager;
            _repository = repository;
        }

        // ISelectionState Implementation
        public bool IsSelected(Entity entity) 
        {
             return _selectionManager.IsSelected(entity);
        }

        public IReadOnlyCollection<Entity> SelectedEntities => _selectionManager.SelectedEntities;

        public Entity? PrimarySelected 
        {
            get => SelectedEntity;
            set => SelectedEntity = value;
        }

        public Entity? SelectedEntity 
        { 
            get => _selectionManager.SelectedEntity;
            set 
            {
                if (value.HasValue)
                    _selectionManager.Select(value.Value, false);
                else
                    _selectionManager.Clear();
            }
        }
        
        public Entity? HoveredEntity 
        { 
            get => _selectionManager.HoveredEntity;
            set => _selectionManager.HoveredEntity = value;
        }
    }
}
