using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Rendering;
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
        private static PlaybackSystem _playback = null!;

        // Systems
        private static MovementSystem _movement = null!;
        private static SpatialSystem _spatial = null!;
        private static PatrolSystem _combat = null!;
        private static CollisionSystem _collision = null!;
        private static CombatSystem _combatSystem = null!;
        private static ProjectileSystem _projectileSystem = null!;
        private static HitFlashSystem _hitFlashSystem = null!;
        private static ParticleSystem _particleSystem = null!;
        private static LifecycleSystem _lifecycle = null!;
        
        // Event Bus
        private static FdpEventBus _eventBus = null!;
        
        // Flight Recorder - AsyncRecorder writes to disk in background thread
        private static AsyncRecorder? _diskRecorder = null;
        private static string _recordingFilePath = "showcase_recording.fdp";
        
        // Playback Controller - handles file replay with seeking
        private static PlaybackController? _playbackController = null;

        // State
        private static bool _isRunning = true;
        private static bool _isRecording = true;
        private static bool _isReplaying = false;
        private static bool _isPaused = false;
        private static bool _showInspector = false;
        private static bool _singleStep = false; // Execute one frame when paused
        
        // Entity Inspector
        private static EntityInspector _inspector = null!;
        
        // Frame tracking
        private static int _totalRecordedFrames = 0;
        private static uint _previousTick = 0;
        
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
            
            // Create the Lifecycle System (Barrier) FIRST
            // Other systems will reference it to queue structural changes
            _lifecycle = new LifecycleSystem(_repo);
            
            // Create Systems - pass lifecycle to systems that need to destroy entities
            _movement = new MovementSystem(_repo);
            _combat = new PatrolSystem(_repo);
            _spatial = new SpatialSystem(_repo);
            _collision = new CollisionSystem(_repo, _eventBus, _spatial);
            _combatSystem = new CombatSystem(_repo, _eventBus, _spatial);
            _projectileSystem = new ProjectileSystem(_repo, _eventBus, _lifecycle, _spatial);
            _hitFlashSystem = new HitFlashSystem(_repo);
            _particleSystem = new ParticleSystem(_repo, _eventBus, _lifecycle);
            
            // Flight Recorder - Async disk recorder with double buffering
            _diskRecorder = new AsyncRecorder(_recordingFilePath);
            _playback = new PlaybackSystem();
            
            // Entity Inspector for debugging
            _inspector = new EntityInspector(_repo);

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
            // Execute if: (not replaying AND not paused) OR single-step requested
            bool shouldExecuteLogic = !_isReplaying && (!_isPaused || _singleStep);
            
            if (shouldExecuteLogic)
            {
                // Clear single step flag (one-shot execution)
                if (_singleStep)
                    _singleStep = false;
                
                // Increment the Global Version so changes this frame get a new timestamp
                // This is CRITICAL for delta frame recording - without it, the dirty scan
                // algorithm won't detect changes and will record empty deltas
                _repo.Tick();
                
                // Live Mode - Run all logic systems
                _timeSystem.Update(); // Updates GlobalTime singleton (TimeSystem has specific API)
                
                // All systems see the SAME world state during this phase
                _combat.Run();          // Patrol/boundaries
                _movement.Run();        // Move entities
                _spatial.Run();         // Rebuild spatial map
                _collision.Run();       // Detect collisions
                _combatSystem.Run();    // Process combat
                _projectileSystem.Run(); // Update projectiles (queues destruction to lifecycle ECB)
                _hitFlashSystem.Run();  // Visual effects
                _particleSystem.Run();  // Particle effects (queues destruction to lifecycle ECB)
                
                // Run the Barrier LAST - Apply all structural changes (destroy, create, etc.)
                // This ensures all systems saw the same entities, and the recorder sees the final state
                _lifecycle.Run();
                
                // Swap event bus buffers (events published this frame become consumable next frame)
                _eventBus.SwapBuffers();
                
                // RECORDING - Async disk recording to prevent memory growth
                if (_isRecording && !_isPaused && _diskRecorder != null)
                {
                    // Record keyframe every 60 frames, delta frames otherwise
                    bool isKeyframe = (_totalRecordedFrames % 60 == 0);
                    
                    if (isKeyframe)
                    {
                        _diskRecorder.CaptureKeyframe(_repo, blocking: false);
                    }
                    else
                    {
                        _diskRecorder.CaptureFrame(_repo, _previousTick, blocking: false);
                    }
                    
                    _totalRecordedFrames++;
                    _previousTick = _repo.GlobalVersion;
                }
            }
            else if (_isReplaying && _playbackController != null)
            {
                // Replay Mode - Automatic playback
                if (!_isPaused)
                {
                    // Advance to next frame
                    if (!_playbackController.StepForward(_repo))
                    {
                        // Reached end of recording - pause automatically
                        _isPaused = true;
                    }
                }
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
                
                // Inspector mode - handle inspector-specific keys, then fall through to common shortcuts
                if (_showInspector)
                {
                    // Inspector handles Tab (focus cycling) and Up/Down (navigation within lists)
                    bool inspectorHandled = _inspector.HandleInput(keyInfo);
                    
                    // If inspector consumed the input, don't process common shortcuts
                    if (inspectorHandled)
                        return;
                    
                    // Otherwise, fall through to allow common shortcuts (ESC, R, P, I, etc.)
                }
                
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
                    
                    case ConsoleKey.I:
                        _showInspector = !_showInspector;
                        break;
                        
                    case ConsoleKey.Spacebar:
                        _isPaused = !_isPaused;
                        break;
                        
                    case ConsoleKey.P: 
                        if (!_isReplaying)
                        {
                            // Enter replay mode: Stop recording, flush file, and open with PlaybackController
                            _isRecording = false;
                            _diskRecorder?.Dispose(); // Flush and close the file
                            _diskRecorder = null;
                            
                            // Initialize PlaybackController - builds frame index automatically
                            try
                            {
                                _playbackController = new PlaybackController(_recordingFilePath);
                                _isReplaying = true;
                                
                                // Seek to first frame
                                if (_playbackController.TotalFrames > 0)
                                {
                                    _playbackController.Rewind(_repo);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load recording: {ex.Message}");
                            }
                        }
                        else
                        {
                            // Exit replay mode
                            _isReplaying = false;
                            _playbackController?.Dispose();
                            _playbackController = null;
                        }
                        break;
                        
                    // Seeking controls
                    case ConsoleKey.LeftArrow:
                        if (_isReplaying && _playbackController != null)
                        {
                            int step = ctrl ? 100 : (shift ? 10 : 1);
                            int targetFrame = Math.Max(0, _playbackController.CurrentFrame - step);
                            _playbackController.SeekToFrame(_repo, targetFrame);
                        }
                        break;
                        
                    case ConsoleKey.RightArrow:
                        if (_isReplaying && _playbackController != null)
                        {
                            // Replay mode - seek forward
                            int step = ctrl ? 100 : (shift ? 10 : 1);
                            int targetFrame = Math.Min(_playbackController.TotalFrames - 1, _playbackController.CurrentFrame + step);
                            _playbackController.SeekToFrame(_repo, targetFrame);
                        }
                        else if (_isPaused && !_isReplaying)
                        {
                            // Live mode + paused - execute one frame
                            _singleStep = true;
                        }
                        break;
                        
                    case ConsoleKey.Home:
                        if (_isReplaying && _playbackController != null)
                        {
                            _playbackController.Rewind(_repo);
                        }
                        break;
                        
                    case ConsoleKey.End:
                        if (_isReplaying && _playbackController != null)
                        {
                            _playbackController.SeekToFrame(_repo, _playbackController.TotalFrames - 1);
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
              
              if (_isReplaying && _playbackController != null)
              {
                  table.AddRow("Replay Frame", $"{_playbackController.CurrentFrame + 1}/{_playbackController.TotalFrames}");
                  table.AddRow("Rec Tick", $"{_playbackController.GetFrameMetadata(_playbackController.CurrentFrame).Tick}");
              }
              else if (_diskRecorder != null)
              {
                  table.AddRow("Rec Frames", $"{_diskRecorder.RecordedFrames}");
                  table.AddRow("Dropped", $"{_diskRecorder.DroppedFrames}");
              }

              // Add controls hint
              string controlsText = _showInspector 
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
              
              // Build right side content
              IRenderable rightContent;
              if (_showInspector)
              {
                  // Update and render inspector
                  _inspector.Update();
                  rightContent = _inspector.Render(30); // Approx width
              }
              else
              {
                  // Normal stats view
                  var grid = new Grid();
                  grid.AddColumn();
                  grid.AddRow(table);
                  grid.AddRow(controls);
                  rightContent = grid;
              }
              
              var map = new System.Text.StringBuilder();
              map.AppendLine($"[bold white on blue] FDP BATTLEFIELD [/]");
              
              // Simple ASCII buffer render
              char[,] buffer = new char[20, 60];
             for(int y=0; y<20;y++) for(int x=0; x<60; x++) buffer[y,x] = ' ';
             
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
                    new Layout("Left").Size(62).Update(new Panel(map.ToString())),
                    new Layout("Right").Update(rightContent)
                );
                
             ctx.UpdateTarget(layout);
        }

        static void Cleanup()
        {
            // Dispose the async recorder - waits for background writes to complete
            _diskRecorder?.Dispose();
            
            // Print recording stats
            if (_diskRecorder != null)
            {
                Console.WriteLine($"\nRecording Statistics:");
                Console.WriteLine($"  Total Frames Recorded: {_diskRecorder.RecordedFrames}");
                Console.WriteLine($"  Dropped Frames: {_diskRecorder.DroppedFrames}");
                Console.WriteLine($"  Recording saved to: {_recordingFilePath}");
            }
            
            _repo.Dispose();
        }
    }
}
