using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Spectre.Console;
using Fdp.Kernel;
using Fdp.Kernel.FlightRecorder;
using Fdp.Examples.Showcase.Components;
using Fdp.Examples.Showcase.Modules;
using Fdp.Examples.Showcase.Systems;

namespace Fdp.Examples.Showcase
{
    class Program
    {
        private static EntityRepository _repo = null!;
        private static TimeSystem _timeSystem = null!;
        private static RecorderSystem _recorder = null!;
        private static PlaybackSystem _playback = null!;

        // Systems
        private static MovementSystem _movement = null!;
        private static PatrolSystem _combat = null!;
        private static CollisionSystem _collision = null!;
        private static CombatSystem _combatSystem = null!;
        private static ProjectileSystem _projectileSystem = null!;
        private static HitFlashSystem _hitFlashSystem = null!;
        private static ParticleSystem _particleSystem = null!;
        
        // Event Bus
        private static FdpEventBus _eventBus = null!;

        // State
        private static bool _isRunning = true;
        private static bool _isRecording = true;
        private static bool _isReplaying = false;
        private static bool _isPaused = false;
        private static MemoryStream _flightRecorderStream = null!; // In-memory flight recorder
        private static BinaryWriter _recorderWriter = null!;
        
        // Frame indexing for seeking
        private static List<long> _framePositions = new List<long>(); // Stream position of each frame
        private static int _currentFrameIndex = 0;
        private static int _totalRecordedFrames = 0;
        
        static void Main(string[] args)
        {
            // Setup Console
            try 
            { 
                if (!Console.IsOutputRedirected) Console.Clear(); 
            } catch (IOException) { /* Ignore if no console */ }
            
            AnsiConsole.Write(new FigletText("FDP Military").Color(Color.Green));

            try
            {
                // Initialize FDP
                Initialize();

                // Run Loop
                RunLoop();
                
                // Cleanup
                Cleanup();
            }
            catch (Exception ex)
            {
                Console.WriteLine("CRITICAL ERROR: " + ex.ToString());
            }
        }

        static void Initialize()
        {
            _repo = new EntityRepository();
            _eventBus = new FdpEventBus();

            // Load Modules
            new PhysicsModule().Load(_repo);
            new CombatModule().Load(_repo);
            new RenderModule().Load(_repo);

            // Register global services
            _timeSystem = new TimeSystem(_repo);
            
            // Create Systems
            _movement = new MovementSystem(_repo);
            _combat = new PatrolSystem(_repo);
            _collision = new CollisionSystem(_repo, _eventBus);
            _combatSystem = new CombatSystem(_repo, _eventBus);
            _projectileSystem = new ProjectileSystem(_repo, _eventBus);
            _hitFlashSystem = new HitFlashSystem(_repo);
            _particleSystem = new ParticleSystem(_repo, _eventBus);
            
            // Flight Recorder
            _recorder = new RecorderSystem();
            _playback = new PlaybackSystem();
            _flightRecorderStream = new MemoryStream();
            _recorderWriter = new BinaryWriter(_flightRecorderStream);

            // Spawn Initial Entities
            SpawnUnit(UnitType.Tank, 10, 10, 5, 0);
            SpawnUnit(UnitType.Aircraft, 5, 5, 10, 2);
            SpawnUnit(UnitType.Infantry, 20, 12, 1, 1);
        }

        static void SpawnUnit(UnitType type, float x, float y, float vx, float vy)
        {
            var e = _repo.CreateEntity();
            _repo.AddComponent(e, new Position { X = x, Y = y });
            _repo.AddComponent(e, new Velocity { X = vx, Y = vy });
            
            char sym = type switch {
                UnitType.Tank => 'T',
                UnitType.Aircraft => '^',
                _ => 'i'
            };
            ConsoleColor col = type switch {
                UnitType.Tank => ConsoleColor.Yellow,
                UnitType.Aircraft => ConsoleColor.Cyan,
                 _ => ConsoleColor.White
            };
            
            _repo.AddComponent(e, new RenderSymbol { Symbol = sym, Color = col });
            _repo.AddComponent(e, new UnitStats { Type = type, Health = 100, MaxHealth = 100 });
        }

        static void RunLoop()
        {
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine("Headless mode detected. Running simulation...");
                while (_isRunning)
                {
                    UpdateLoop(null);
                    Thread.Sleep(33);
                }
            }
            else
            {
                try
                {
                    // Use live display for stats
                    AnsiConsole.Live(new Panel("Initializing..."))
                        .Start(ctx => 
                        {
                            while (_isRunning)
                            {
                                UpdateLoop(ctx);
                                Thread.Sleep(33);
                            }
                        });
                }
                catch (IOException)
                {
                    // Fallback if Live display fails (e.g. resized too small or weird terminal)
                    while (_isRunning)
                    {
                         UpdateLoop(null);
                         Thread.Sleep(33);
                    }
                }
            }
        }

        static void UpdateLoop(LiveDisplayContext? ctx)
        {
            // 1. Input
            HandleInput();
            
            // 2. Logic Step
            if (!_isReplaying && !_isPaused)
            {
                // Live Mode
                _timeSystem.Update(); // Updates GlobalTime singleton (TimeSystem has specific API)
                
                _combat.Run();          // Patrol/boundaries
                _movement.Run();        // Move entities
                _collision.Run();       // Detect collisions
                _combatSystem.Run();    // Process combat
                _projectileSystem.Run(); // Update projectiles
                _hitFlashSystem.Run();  // Visual effects
                _particleSystem.Run();  // Particle effects
                
                // Swap event bus buffers (events published this frame become consumable next frame)
                _eventBus.SwapBuffers();
                
                // RECORDING
                if (_isRecording && !_isPaused)
                {
                    long pos = _flightRecorderStream.Position;
                    _framePositions.Add(pos); // Index this frame for seeking
                    _recorder.RecordKeyframe(_repo, _recorderWriter, _eventBus);
                    _totalRecordedFrames++;
                    _currentFrameIndex = _totalRecordedFrames - 1; // We're at the latest frame
                }
            }
            else
            {
                // Replay Mode logic would go here (seeking stream)
            }

            // 3. Render
            if (ctx != null)
            {
                Render(ctx);
            }
        }
        
        static void HandleInput()
        {
            if (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;
                var shift = (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0;
                var ctrl = (keyInfo.Modifiers & ConsoleModifiers.Control) != 0;
                
                switch(key)
                {
                    case ConsoleKey.Escape: 
                        _isRunning = false; 
                        break;
                        
                    case ConsoleKey.R: 
                        _isRecording = !_isRecording; 
                        break;
                        
                    case ConsoleKey.Spacebar:
                        _isPaused = !_isPaused;
                        break;
                        
                    case ConsoleKey.P: 
                        _isReplaying = !_isReplaying;
                        if (_isReplaying && _totalRecordedFrames > 0)
                        {
                            _currentFrameIndex = 0; // Start from beginning
                            SeekToFrame(_currentFrameIndex);
                        }
                        break;
                        
                    // Seeking controls
                    case ConsoleKey.LeftArrow:
                        if (_isReplaying)
                        {
                            int step = ctrl ? 100 : (shift ? 10 : 1);
                            _currentFrameIndex = Math.Max(0, _currentFrameIndex - step);
                            SeekToFrame(_currentFrameIndex);
                        }
                        break;
                        
                    case ConsoleKey.RightArrow:
                        if (_isReplaying)
                        {
                            int step = ctrl ? 100 : (shift ? 10 : 1);
                            _currentFrameIndex = Math.Min(_totalRecordedFrames - 1, _currentFrameIndex + step);
                            SeekToFrame(_currentFrameIndex);
                        }
                        break;
                        
                    case ConsoleKey.Home:
                        if (_isReplaying && _totalRecordedFrames > 0)
                        {
                            _currentFrameIndex = 0;
                            SeekToFrame(_currentFrameIndex);
                        }
                        break;
                        
                    case ConsoleKey.End:
                        if (_isReplaying && _totalRecordedFrames > 0)
                        {
                            _currentFrameIndex = _totalRecordedFrames - 1;
                            SeekToFrame(_currentFrameIndex);
                        }
                        break;
                        
                    // Entity spawning
                    case ConsoleKey.D1: SpawnRandomUnit(UnitType.Infantry); break;
                    case ConsoleKey.D2: SpawnRandomUnit(UnitType.Tank); break;
                    case ConsoleKey.D3: SpawnRandomUnit(UnitType.Aircraft); break;
                    
                    // Remove random entity
                    case ConsoleKey.Delete:
                    case ConsoleKey.Backspace:
                        RemoveRandomUnit();
                        break;
                }
            }
        }
        
        static void SeekToFrame(int frameIndex)
        {
            if (frameIndex < 0 || frameIndex >= _totalRecordedFrames) return;
            
            long position = _framePositions[frameIndex];
            _flightRecorderStream.Position = position;
            
            using var reader = new BinaryReader(_flightRecorderStream, System.Text.Encoding.UTF8, true);
            _playback.ApplyFrame(_repo, reader, processEvents: false);
        }
        
        static void SpawnRandomUnit(UnitType type)
        {
            if (_isReplaying) return; // Can't spawn during replay
            
            var rand = new Random();
            float x = rand.Next(5, 55);
            float y = rand.Next(2, 18);
            float vx = rand.Next(-5, 6);
            float vy = rand.Next(-5, 6);
            
            SpawnUnit(type, x, y, vx, vy);
        }
        
        static void RemoveRandomUnit()
        {
            if (_isReplaying) return; // Can't remove during replay
            
            var query = _repo.Query().Build();
            var entities = new List<Entity>();
            query.ForEach(e => entities.Add(e));
            
            if (entities.Count > 0)
            {
                var rand = new Random();
                var toRemove = entities[rand.Next(entities.Count)];
                _repo.DestroyEntity(toRemove);
            }
        }
        


        static void Render(LiveDisplayContext ctx)
        {
             // Collect entities to render
             var canvas = new Canvas(80, 24); 
             
            var renderQuery = _repo.Query()
                .With<Position>()
                .With<RenderSymbol>()
                .Build();

             renderQuery.ForEach(entity =>
             {
                 ref readonly var pos = ref _repo.GetComponentRO<Position>(entity);
                 ref readonly var sym = ref _repo.GetComponentRO<RenderSymbol>(entity);
                 
                 int x = (int)Math.Clamp(pos.X, 0, 79);
                 int y = (int)Math.Clamp(pos.Y, 0, 23);
                 
                 canvas.SetPixel(x, y, new Color((byte)sym.Color, (byte)sym.Color, (byte)sym.Color)); // Simplified color mapping
             });

             // Global Stats
             ref var time = ref _repo.GetSingletonUnmanaged<GlobalTime>();
             
             var table = new Table().Border(TableBorder.Rounded);
             table.AddColumn("Stat");
             table.AddColumn("Value");
              // Count entities
              int entityCount = 0;
              renderQuery.ForEach(_ => entityCount++);
              
              table.AddRow("Time", $"{time.TotalTime:F2}s");
              table.AddRow("Frame", $"{time.FrameCount}");
              table.AddRow("Mode", _isReplaying ? "[yellow]REPLAY[/]" : "[green]LIVE[/]");
              table.AddRow("Recording", _isRecording ? "[green]ON[/]" : "[red]OFF[/]");
              table.AddRow("Paused", _isPaused ? "[red]YES[/]" : "[green]NO[/]");
              table.AddRow("Entities", $"{entityCount}");
              table.AddRow("Rec Frames", $"{_totalRecordedFrames}");
              
              if (_isReplaying && _totalRecordedFrames > 0)
              {
                  table.AddRow("Replay Frame", $"{_currentFrameIndex + 1}/{_totalRecordedFrames}");
              }
              
              table.AddRow("Buffer Size", $"{_flightRecorderStream.Length / 1024} KB");

              // Add controls hint
              var controls = new Panel(
                  "[yellow]ESC[/]=Quit [yellow]SPACE[/]=Pause [yellow]R[/]=Record [yellow]P[/]=Replay\n" +
                  "[yellow]←→[/]=Seek +[yellow]SHIFT[/]=10x +[yellow]CTRL[/]=100x\n" +
                  "[yellow]HOME/END[/]=First/Last [yellow]1/2/3[/]=Spawn [yellow]DEL[/]=Remove"
              )
              {
                  Header = new PanelHeader("Controls"),
                  Border = BoxBorder.Rounded
              };
              
              var grid = new Grid();
             grid.AddColumn();
             grid.AddRow(table);
             grid.AddRow(controls);
             // Note: Spectre.Console Canvas isn't fully robust for 60fps blit in a loop efficiently without specialized handling,
             // so we might just use Table or text dump for "Radar".
             // Actually, let's just output text map string.
             
             var map = new System.Text.StringBuilder();
             map.AppendLine($"[bold white on blue] FDP BATTLEFIELD [/]");
             
             // Simple ASCII buffer render
             char[,] buffer = new char[20, 60];
             for(int y=0; y<20;y++) for(int x=0; x<60; x++) buffer[y,x] = '.';
             
             renderQuery.ForEach(entity =>
             {
                 ref readonly var pos = ref _repo.GetComponentRO<Position>(entity);
                 ref readonly var sym = ref _repo.GetComponentRO<RenderSymbol>(entity);
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
                    new Layout("Left").Ratio(2).Update(new Panel(map.ToString())),
                    new Layout("Right").Ratio(1).Update(grid)
                );
                
             ctx.UpdateTarget(layout);
        }

        static void Cleanup()
        {
            _repo.Dispose();
            _recorderWriter.Dispose();
            _flightRecorderStream.Dispose();
        }
    }
}
