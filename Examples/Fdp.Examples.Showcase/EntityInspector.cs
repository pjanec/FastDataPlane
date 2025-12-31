using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdp.Kernel;
using Spectre.Console;
using Spectre.Console.Rendering;
using Fdp.Examples.Showcase.Components;

namespace Fdp.Examples.Showcase
{
    /// <summary>
    /// Interactive entity inspector for debugging.
    /// Shows entities, their components, and detailed component properties.
    /// </summary>
    public class EntityInspector
    {
        private enum FocusMode
        {
            EntityList,
            ComponentList,
            ComponentDetail
        }
        
        private readonly EntityRepository _repo;
        private FocusMode _focusMode = FocusMode.EntityList;
        
        // Selection state
        private List<Entity> _entities = new();
        private int _selectedEntityIndex = 0;
        private List<ComponentInfo> _components = new();
        private int _selectedComponentIndex = 0;
        
        public EntityInspector(EntityRepository repo)
        {
            _repo = repo;
        }
        
        public void Update()
        {
            // Refresh entity list
            _entities.Clear();
            var index = _repo.GetEntityIndex();
            for (int i = 0; i <= index.MaxIssuedIndex; i++)
            {
                ref var header = ref index.GetHeader(i);
                if (header.IsActive)
                {
                    _entities.Add(new Entity(i, header.Generation));
                }
            }
            
            // Clamp selection
            if (_selectedEntityIndex >= _entities.Count)
                _selectedEntityIndex = Math.Max(0, _entities.Count - 1);
            
            // Refresh component list for selected entity
            if (_entities.Count > 0 && _selectedEntityIndex < _entities.Count)
            {
                var selectedEntity = _entities[_selectedEntityIndex];
                _components = GetComponentsForEntity(selectedEntity);
                
                if (_selectedComponentIndex >= _components.Count)
                    _selectedComponentIndex = Math.Max(0, _components.Count - 1);
            }
            else
            {
                _components.Clear();
            }
        }
        
        public bool HandleInput(ConsoleKeyInfo keyInfo)
        {
            bool shift = (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0;
            
            switch (keyInfo.Key)
            {
                case ConsoleKey.Tab:
                    if (shift)
                    {
                        // Shift+Tab - Cycle backwards
                        _focusMode = _focusMode switch
                        {
                            FocusMode.EntityList => FocusMode.ComponentDetail,
                            FocusMode.ComponentList => FocusMode.EntityList,
                            FocusMode.ComponentDetail => FocusMode.ComponentList,
                            _ => FocusMode.EntityList
                        };
                    }
                    else
                    {
                        // Tab - Cycle forwards
                        _focusMode = _focusMode switch
                        {
                            FocusMode.EntityList => FocusMode.ComponentList,
                            FocusMode.ComponentList => FocusMode.ComponentDetail,
                            FocusMode.ComponentDetail => FocusMode.EntityList,
                            _ => FocusMode.EntityList
                        };
                    }
                    return true; // Consumed
                    
                case ConsoleKey.UpArrow:
                    if (_focusMode == FocusMode.EntityList)
                        _selectedEntityIndex = Math.Max(0, _selectedEntityIndex - 1);
                    else if (_focusMode == FocusMode.ComponentList)
                        _selectedComponentIndex = Math.Max(0, _selectedComponentIndex - 1);
                    return true; // Consumed
                    
                case ConsoleKey.DownArrow:
                    if (_focusMode == FocusMode.EntityList)
                        _selectedEntityIndex = Math.Min(_entities.Count - 1, _selectedEntityIndex + 1);
                    else if (_focusMode == FocusMode.ComponentList)
                        _selectedComponentIndex = Math.Min(_components.Count - 1, _selectedComponentIndex + 1);
                    return true; // Consumed
                    
                case ConsoleKey.PageUp:
                    if (_focusMode == FocusMode.EntityList)
                        _selectedEntityIndex = Math.Max(0, _selectedEntityIndex - 10);
                    else if (_focusMode == FocusMode.ComponentList)
                        _selectedComponentIndex = Math.Max(0, _selectedComponentIndex - 5);
                    return true; // Consumed
                    
                case ConsoleKey.PageDown:
                    if (_focusMode == FocusMode.EntityList)
                        _selectedEntityIndex = Math.Min(_entities.Count - 1, _selectedEntityIndex + 10);
                    else if (_focusMode == FocusMode.ComponentList)
                        _selectedComponentIndex = Math.Min(_components.Count - 1, _selectedComponentIndex + 5);
                    return true; // Consumed
                    
                default:
                    return false; // Not consumed - allow common shortcuts to process
            }
        }
        
        public IRenderable Render(int width)
        {
            // Use Grid instead of Layout to stack panels without overlap
            var grid = new Grid();
            grid.AddColumn();
            
            grid.AddRow(RenderEntityList());
            grid.AddRow(RenderComponentList());
            grid.AddRow(RenderComponentDetails());
            
            return grid;
        }
        
        private Panel RenderEntityList()
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(_focusMode == FocusMode.EntityList ? Color.Yellow : Color.Grey);
            
            table.AddColumn("ID");
            table.AddColumn("Gen");
            table.AddColumn("Type");
            table.AddColumn("Health");
            table.AddColumn("Position");
            
            int displayStart = Math.Max(0, _selectedEntityIndex - 4);
            int displayEnd = Math.Min(_entities.Count, displayStart + 8);
            
            for (int i = displayStart; i < displayEnd; i++)
            {
                var entity = _entities[i];
                bool isSelected = (i == _selectedEntityIndex);
                
                string id = entity.Index.ToString();
                string gen = entity.Generation.ToString();
                string type = GetEntityType(entity);
                string health = GetEntityHealth(entity);
                string pos = GetEntityPosition(entity);
                
                if (isSelected && _focusMode == FocusMode.EntityList)
                {
                    table.AddRow(
                        $"[yellow]>{id}[/]",
                        $"[yellow]{gen}[/]",
                        $"[yellow]{type}[/]",
                        $"[yellow]{health}[/]",
                        $"[yellow]{pos}[/]"
                    );
                }
                else if (isSelected)
                {
                    table.AddRow($"[bold]{id}[/]", gen, type, health, pos);
                }
                else
                {
                    table.AddRow(id, gen, type, health, pos);
                }
            }
            
            return new Panel(table)
                .Header($"[bold]Entities ({_entities.Count})[/]")
                .BorderColor(_focusMode == FocusMode.EntityList ? Color.Yellow : Color.Grey);
        }
        
        private Panel RenderComponentList()
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(_focusMode == FocusMode.ComponentList ? Color.Yellow : Color.Grey);
            
            table.AddColumn("Component");
            table.AddColumn("Summary");
            
            if (_entities.Count == 0 || _selectedEntityIndex >= _entities.Count)
            {
                table.AddRow("[dim]No entity selected[/]", "");
            }
            else
            {
                for (int i = 0; i < _components.Count; i++)
                {
                    var comp = _components[i];
                    bool isSelected = (i == _selectedComponentIndex);
                    
                    if (isSelected && _focusMode == FocusMode.ComponentList)
                    {
                        table.AddRow(
                            $"[yellow]>{comp.TypeName}[/]",
                            $"[yellow]{comp.Summary}[/]"
                        );
                    }
                    else if (isSelected)
                    {
                        table.AddRow($"[bold]{comp.TypeName}[/]", comp.Summary);
                    }
                    else
                    {
                        table.AddRow(comp.TypeName, comp.Summary);
                    }
                }
            }
            
            return new Panel(table)
                .Header($"[bold]Components ({_components.Count})[/]")
                .BorderColor(_focusMode == FocusMode.ComponentList ? Color.Yellow : Color.Grey);
        }
        
        private Panel RenderComponentDetails()
        {
            var grid = new Grid();
            grid.AddColumn();
            
            if (_components.Count == 0 || _selectedComponentIndex >= _components.Count)
            {
                grid.AddRow("[dim]No component selected[/]");
            }
            else
            {
                var comp = _components[_selectedComponentIndex];
                
                grid.AddRow($"[bold underline]{comp.TypeName}[/]");
                grid.AddRow("");
                
                foreach (var prop in comp.Properties)
                {
                    grid.AddRow($"[cyan]{prop.Key}:[/] {prop.Value}");
                }
            }
            
            return new Panel(grid)
                .Header("[bold]Component Details[/]")
                .BorderColor(_focusMode == FocusMode.ComponentDetail ? Color.Yellow : Color.Grey);
        }
        
        private string GetEntityType(Entity entity)
        {
            if (_repo.HasComponent<UnitStats>(entity))
            {
                ref readonly var stats = ref _repo.GetComponentRO<UnitStats>(entity);
                return stats.Type.ToString();
            }
            return "Unknown";
        }
        
        private string GetEntityHealth(Entity entity)
        {
            if (_repo.HasComponent<UnitStats>(entity))
            {
                ref readonly var stats = ref _repo.GetComponentRO<UnitStats>(entity);
                return $"{stats.Health:F0}";
            }
            return "-";
        }
        
        private string GetEntityPosition(Entity entity)
        {
            if (_repo.HasComponent<Position>(entity))
            {
                ref readonly var pos = ref _repo.GetComponentRO<Position>(entity);
                return $"({pos.X:F1}, {pos.Y:F1})";
            }
            return "-";
        }
        
        private List<ComponentInfo> GetComponentsForEntity(Entity entity)
        {
            var result = new List<ComponentInfo>();
            
            // Check all known component types
            CheckComponent<Position>(entity, result);
            CheckComponent<Velocity>(entity, result);
            CheckComponent<RenderSymbol>(entity, result);
            CheckComponent<UnitStats>(entity, result);
            CheckComponent<Projectile>(entity, result);
            CheckComponent<Particle>(entity, result);
            CheckComponent<HitFlash>(entity, result);
            CheckComponent<Corpse>(entity, result);
            
            return result;
        }
        
        private void CheckComponent<T>(Entity entity, List<ComponentInfo> list) where T : struct
        {
            if (_repo.HasComponent<T>(entity))
            {
                ref readonly var comp = ref _repo.GetComponentRO<T>(entity);
                var info = new ComponentInfo
                {
                    TypeName = typeof(T).Name,
                    Summary = GetComponentSummary(comp),
                    Properties = GetComponentProperties(comp)
                };
                list.Add(info);
            }
        }
        
        private string GetComponentSummary<T>(in T component)
        {
            return component switch
            {
                Position pos => $"({pos.X:F1}, {pos.Y:F1})",
                Velocity vel => $"({vel.X:F1}, {vel.Y:F1})",
                RenderSymbol sym => $"'{sym.Symbol}' {sym.Color}",
                UnitStats stats => $"{stats.Type} HP:{stats.Health:F0}",
                Projectile proj => $"Life:{proj.Lifetime:F1}s",
                Particle part => $"Life:{part.Lifetime:F1}s",
                HitFlash flash => $"{flash.Duration:F2}s",
                Corpse corpse => $"Ttl:{corpse.TimeRemaining:F1}s",
                _ => ""
            };
        }
        
        private Dictionary<string, string> GetComponentProperties<T>(in T component)
        {
            var props = new Dictionary<string, string>();
            var type = typeof(T);
            
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = field.GetValue(component);
                props[field.Name] = value?.ToString() ?? "null";
            }
            
            return props;
        }
        
        private class ComponentInfo
        {
            public string TypeName { get; set; } = "";
            public string Summary { get; set; } = "";
            public Dictionary<string, string> Properties { get; set; } = new();
        }
    }
}
