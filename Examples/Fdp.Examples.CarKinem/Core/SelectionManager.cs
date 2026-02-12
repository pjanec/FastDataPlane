namespace Fdp.Examples.CarKinem.Core
{
    public class SelectionManager
    {
        private readonly System.Collections.Generic.HashSet<int> _selectedIds = new();
        public event System.Action? SelectionChanged;

        public System.Collections.Generic.IReadOnlyCollection<int> SelectedIds => _selectedIds;
        public int? SelectedEntityId => _selectedIds.Count == 1 ? System.Linq.Enumerable.First(_selectedIds) : null;
        public int? HoveredEntityId { get; set; }

        public void Clear()
        {
            if (_selectedIds.Count > 0)
            {
                _selectedIds.Clear();
                SelectionChanged?.Invoke();
            }
        }

        public void Select(int id, bool additive = false)
        {
            bool changed = false;
            if (!additive)
            {
                if (_selectedIds.Count != 1 || !_selectedIds.Contains(id))
                {
                    _selectedIds.Clear();
                    _selectedIds.Add(id);
                    changed = true;
                }
            }
            else
            {
                if (_selectedIds.Add(id)) changed = true;
            }
            if (changed) SelectionChanged?.Invoke();
        }

        public void SetSelection(System.Collections.Generic.IEnumerable<int> ids)
        {
            _selectedIds.Clear();
            foreach (var id in ids) _selectedIds.Add(id);
            SelectionChanged?.Invoke();
        }

        public bool IsSelected(int id) => _selectedIds.Contains(id);
    }
}
