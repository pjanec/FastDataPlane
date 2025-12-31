using System;
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
                  var grid = new Grid();
                  grid.AddColumn();
                  grid.AddRow(table);
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
    }
}
