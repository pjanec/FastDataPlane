using System;
using System.Linq;
using Spectre.Console;
using Spectre.Console.Rendering;
using Fdp.Kernel;
using Fdp.Examples.Showcase.Components;

namespace Fdp.Examples.Showcase.Core
{
    public class ShowcaseRenderer
    {
        private readonly ShowcaseGame _game;

        public ShowcaseRenderer(ShowcaseGame game)
        {
            _game = game;
        }

        public void Render(LiveDisplayContext ctx)
        {
             var canvas = new Canvas(80, 24); 
             
             var renderQuery = _game.Repo.Query()
                .With<Position>()
                .With<RenderSymbol>()
                .Build();

             renderQuery.ForEach(entity =>
             {
                 ref readonly var pos = ref _game.Repo.GetComponentRO<Position>(entity);
                 ref readonly var sym = ref _game.Repo.GetComponentRO<RenderSymbol>(entity);
                 
                 int x = (int)Math.Clamp(pos.X, 0, 79);
                 int y = (int)Math.Clamp(pos.Y, 0, 23);
                 
                 canvas.SetPixel(x, y, new Color((byte)sym.Color, (byte)sym.Color, (byte)sym.Color));
             });

             ref var time = ref _game.Repo.GetSingletonUnmanaged<GlobalTime>();
             
             var table = new Table().Border(TableBorder.Rounded);
             table.AddColumn("Stat");
             table.AddColumn("Value");
             
              int entityCount = 0;
              renderQuery.ForEach(_ => entityCount++);
              
              table.AddRow("Time", $"{time.TotalTime:F2}s");
              table.AddRow("Frame", $"{time.FrameCount}");
              table.AddRow("Mode", _game.IsReplaying ? "[yellow]REPLAY[/]" : "[green]LIVE[/]");
              table.AddRow("Recording", _game.IsRecording ? "[green]ON[/]" : "[red]OFF[/]");
              table.AddRow("Paused", _game.IsPaused ? "[red]YES[/]" : "[green]NO[/]");
              table.AddRow("Entities", $"{entityCount}");
              
              if (_game.IsReplaying && _game.PlaybackController != null)
              {
                  table.AddRow("Replay Frame", $"{_game.PlaybackController.CurrentFrame + 1}/{_game.PlaybackController.TotalFrames}");
                  table.AddRow("Rec Tick", $"{_game.PlaybackController.GetFrameMetadata(_game.PlaybackController.CurrentFrame).Tick}");
              }
              else if (_game.DiskRecorder != null)
              {
                  table.AddRow("Rec Frames", $"{_game.DiskRecorder.RecordedFrames}");
                  table.AddRow("Dropped", $"{_game.DiskRecorder.DroppedFrames}");
              }

              string controlsText = _game.ShowInspector 
                  ? "[yellow]I[/]=Inspector [yellow]TAB[/]=Next [yellow]SHIFT+TAB[/]=Back [yellow]↑↓[/]=Navigate\n" +
                    "[yellow]ESC/SPACE/R/P[/]=Common shortcuts still work"
                  : "[yellow]ESC[/]=Quit [yellow]SPACE[/]=Pause [yellow]R[/]=Record [yellow]P[/]=Replay [yellow]I[/]=Inspector\n" +
                    "[yellow]arrows[/]=Seek +[yellow]SHIFT[/]=10x +[yellow]CTRL[/]=100x\n" +
                    "[yellow]HOME/END[/]=First/Last [yellow]1/2/3[/]=Spawn [yellow]DEL[/]=Remove";
              
              var controls = new Panel(controlsText)
              {
                  Header = new PanelHeader("Controls"),
                  Border = BoxBorder.Rounded
              };
              
              IRenderable rightContent;
              if (_game.ShowInspector)
              {
                  _game.Inspector.Update();
                  rightContent = _game.Inspector.Render(30); 
              }
              else
              {
                  var perfTable = CreatePerformanceTable();
                  
                  var grid = new Grid();
                  grid.AddColumn();
                  grid.AddRow(table);
                  grid.AddRow(perfTable);
                  grid.AddRow(controls);
                  rightContent = grid;
              }
              
              var map = new System.Text.StringBuilder();
              map.AppendLine($"[bold white on blue] FDP BATTLEFIELD [/]");
              
              char[,] buffer = new char[20, 60];
              for(int y=0; y<20;y++) for(int x=0; x<60; x++) buffer[y,x] = ' ';
             
             renderQuery.ForEach(entity =>
             {
                 ref readonly var pos = ref _game.Repo.GetComponentRO<Position>(entity);
                 ref readonly var sym = ref _game.Repo.GetComponentRO<RenderSymbol>(entity);
                 int x = (int)pos.X; int y = (int)pos.Y;
                 if(x>=0 && x<60 && y>=0 && y<20) buffer[y,x] = sym.Symbol;
             });
             
             for(int y=0; y<20;y++) 
             {
                 for(int x=0; x<60; x++) map.Append(buffer[y,x]);
                 map.AppendLine();
             }

             var layout = new Layout("Root")
                .SplitColumns(
                    new Layout("Left").Size(62).Update(new Panel(map.ToString())),
                    new Layout("Right").Update(rightContent)
                );
                
             ctx.UpdateTarget(layout);
        }
        
        private Table CreatePerformanceTable()
        {
            var perfTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey);
            
            perfTable.AddColumn(new TableColumn("[bold]Phase[/]").Width(18));
            perfTable.AddColumn(new TableColumn("[bold]Time (ms)[/]").RightAligned().Width(12));
            perfTable.AddColumn(new TableColumn("[bold]%[/]").RightAligned().Width(7));
            
            // Sort by original order (numbered phases)
            var orderedPhases = _game.PhaseTimings
                .OrderBy(kvp => kvp.Key)
                .ToList();
            
            double totalLogicTime = orderedPhases
                .Where(kvp => !kvp.Key.Contains("Render") && !kvp.Key.Contains("Input"))
                .Sum(kvp => kvp.Value);
            
            foreach (var phase in orderedPhases)
            {
                string phaseName = phase.Key;
                double timeMs = phase.Value;
                
                // Calculate percentage of total frame time
                double percentage = _game.TotalFrameTime > 0 
                    ? (timeMs / _game.TotalFrameTime) * 100.0 
                    : 0;
                
                // Color code based on time
                string timeColor = timeMs switch
                {
                    > 5.0 => "red",
                    > 2.0 => "yellow",
                    > 1.0 => "orange1",
                    > 0.5 => "white",
                    _ => "grey"
                };
                
                string percentColor = percentage switch
                {
                    > 30.0 => "red",
                    > 15.0 => "yellow",
                    > 5.0 => "white",
                    _ => "grey"
                };
                
                // Clean up phase name for display
                string displayName = phaseName.Length > 3 && char.IsDigit(phaseName[0]) 
                    ? phaseName.Substring(phaseName.IndexOf('.') + 2)  // Remove "1. "
                    : phaseName;
                
                perfTable.AddRow(
                    displayName,
                    $"[{timeColor}]{timeMs:F3}[/]",
                    $"[{percentColor}]{percentage:F1}[/]"
                );
            }
            
            // Add separator
            perfTable.AddEmptyRow();
            
            // Add totals
            perfTable.AddRow(
                "[bold cyan]Logic Total[/]",
                $"[bold cyan]{totalLogicTime:F3}[/]",
                $"[bold cyan]{(totalLogicTime / _game.TotalFrameTime * 100):F1}[/]"
            );
            
            perfTable.AddRow(
                "[bold yellow]Frame Total[/]",
                $"[bold yellow]{_game.TotalFrameTime:F3}[/]",
                "[bold yellow]100.0[/]"
            );
            
            double fps = _game.TotalFrameTime > 0 ? 1000.0 / _game.TotalFrameTime : 0;
            string fpsColor = fps switch
            {
                >= 60 => "green",
                >= 30 => "yellow",
                >= 20 => "orange1",
                _ => "red"
            };
            
            perfTable.AddRow(
                "[bold]Target FPS[/]",
                $"[bold {fpsColor}]{fps:F1}[/]",
                ""
            );

            return perfTable;
        }
    }
}
