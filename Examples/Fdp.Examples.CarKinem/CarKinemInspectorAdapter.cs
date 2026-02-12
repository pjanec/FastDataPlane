using Fdp.Kernel;
using FDP.Toolkit.ImGui.Abstractions;
using Fdp.Examples.CarKinem.Input;

namespace Fdp.Examples.CarKinem
{
    public class CarKinemInspectorAdapter : IInspectorContext
    {
        private readonly SelectionManager _selectionManager;
        private readonly EntityRepository _repository; 
        
        public CarKinemInspectorAdapter(SelectionManager selectionManager, EntityRepository repository)
        {
            _selectionManager = selectionManager;
            _repository = repository;
        }

        public Entity? SelectedEntity 
        { 
            get 
            {
                if (_selectionManager.SelectedEntityId.HasValue)
                {
                    int index = _selectionManager.SelectedEntityId.Value;
                    var idx = _repository.GetEntityIndex();
                    
                    if (index <= idx.MaxIssuedIndex)
                    {
                        ref var header = ref idx.GetHeader(index);
                        if (header.IsActive)
                        {
                            return new Entity(index, header.Generation);
                        }
                    }
                    
                    return new Entity(index, 0); 
                }
                return null;
            }
            set 
            {
                if (value.HasValue)
                    _selectionManager.SelectedEntityId = value.Value.Index;
                else
                    _selectionManager.SelectedEntityId = null;
            }
        }
        
        public Entity? HoveredEntity { get; set; }
    }
}
