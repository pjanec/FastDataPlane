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

namespace Fdp.Examples.Showcase.Core
{
    public class ShowcaseGame
    {
        public EntityRepository Repo { get; private set; } = null!;
        private TimeSystem _timeSystem = null!;
        private PlaybackSystem _playback = null!;

        // Systems
        private MovementSystem _movement = null!;
        private SpatialSystem _spatial = null!;
        private PatrolSystem _combat = null!;
        private CollisionSystem _collision = null!;
        private CombatSystem _combatSystem = null!;
        private ProjectileSystem _projectileSystem = null!;
        private HitFlashSystem _hitFlashSystem = null!;
        private ParticleSystem _particleSystem = null!;
        private LifecycleSystem _lifecycle = null!;
        
        // Event Bus
        private FdpEventBus _eventBus = null!;
        
        // Flight Recorder
        public AsyncRecorder? DiskRecorder { get; set; } = null;
        private string _recordingFilePath = "showcase_recording.fdp";
        public string RecordingFilePath => _recordingFilePath;
        
        // Playback Controller
        public PlaybackController? PlaybackController { get; set; } = null;

        // State
        public bool IsRunning { get; set; } = true;
        public bool IsRecording { get; set; } = true;
        public bool IsReplaying { get; set; } = false;
        public bool IsPaused { get; set; } = false;
        public bool ShowInspector { get; set; } = false;
        public bool SingleStep { get; set; } = false;
        
        // Entity Inspector
        public EntityInspector Inspector { get; private set; } = null!;
        
        // Frame tracking
        private int _totalRecordedFrames = 0;
        private uint _previousTick = 0;

        private ShowcaseRenderer _renderer = null!;
        private ShowcaseInput _input = null!;

        public ShowcaseGame()
        {
            _renderer = new ShowcaseRenderer(this);
            _input = new ShowcaseInput(this);
        }

        public void Initialize()
        {
            Repo = new EntityRepository();
            _eventBus = new FdpEventBus();

            // Load Modules
            new PhysicsModule().Load(Repo);
            new CombatModule().Load(Repo);
            new RenderModule().Load(Repo);

            // Register global services
            _timeSystem = new TimeSystem(Repo);
            
            // Create the Lifecycle System (Barrier) FIRST
            _lifecycle = new LifecycleSystem(Repo);
            
            // Create Systems
            _movement = new MovementSystem(Repo);
            _combat = new PatrolSystem(Repo);
            _spatial = new SpatialSystem(Repo);
            _collision = new CollisionSystem(Repo, _eventBus, _spatial);
            _combatSystem = new CombatSystem(Repo, _eventBus, _spatial);
            _projectileSystem = new ProjectileSystem(Repo, _eventBus, _lifecycle, _spatial);
            _hitFlashSystem = new HitFlashSystem(Repo);
            _particleSystem = new ParticleSystem(Repo, _eventBus, _lifecycle);
            
            // Flight Recorder
            DiskRecorder = new AsyncRecorder(_recordingFilePath);
            _playback = new PlaybackSystem();
            
            // Entity Inspector
            Inspector = new EntityInspector(Repo);

            // Spawn Initial Entities
            SpawnUnit(UnitType.Tank, 10, 10, 5, 0);
            SpawnUnit(UnitType.Aircraft, 5, 5, 10, 2);
            SpawnUnit(UnitType.Infantry, 20, 12, 1, 1);
        }

        public void RunLoop()
        {
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine("Headless mode detected. Running simulation...");
                while (IsRunning)
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
                            while (IsRunning)
                            {
                                UpdateLoop(ctx);
                                Thread.Sleep(33);
                            }
                        });
                }
                catch (IOException)
                {
                    // Fallback
                    while (IsRunning)
                    {
                         UpdateLoop(null);
                         Thread.Sleep(33);
                    }
                }
            }
        }

        private void UpdateLoop(LiveDisplayContext? ctx)
        {
            // 1. Input
            _input.HandleInput();
            
            // 2. Logic Step
            bool shouldExecuteLogic = !IsReplaying && (!IsPaused || SingleStep);
            
            if (shouldExecuteLogic)
            {
                if (SingleStep)
                    SingleStep = false;
                
                Repo.Tick();
                
                _timeSystem.Update();
                
                _combat.Run();
                _movement.Run();
                _spatial.Run();
                _collision.Run();
                _combatSystem.Run();
                _projectileSystem.Run();
                _hitFlashSystem.Run();
                _particleSystem.Run();
                
                _lifecycle.Run();
                
                _eventBus.SwapBuffers();
                
                if (IsRecording && !IsPaused && DiskRecorder != null)
                {
                    bool isKeyframe = (_totalRecordedFrames % 60 == 0);
                    
                    if (isKeyframe)
                    {
                        DiskRecorder.CaptureKeyframe(Repo, blocking: false);
                    }
                    else
                    {
                        DiskRecorder.CaptureFrame(Repo, _previousTick, blocking: false);
                    }
                    
                    _totalRecordedFrames++;
                    _previousTick = Repo.GlobalVersion;
                }
            }
            else if (IsReplaying && PlaybackController != null)
            {
                if (!IsPaused)
                {
                    if (!PlaybackController.StepForward(Repo))
                    {
                        IsPaused = true;
                    }
                }
            }

            // 3. Render
            if (ctx != null)
            {
                _renderer.Render(ctx);
            }
        }

        public void Cleanup()
        {
            DiskRecorder?.Dispose();
            
            if (DiskRecorder != null)
            {
                Console.WriteLine($"\nRecording Statistics:");
                Console.WriteLine($"  Total Frames Recorded: {DiskRecorder.RecordedFrames}");
                Console.WriteLine($"  Dropped Frames: {DiskRecorder.DroppedFrames}");
                Console.WriteLine($"  Recording saved to: {_recordingFilePath}");
            }
            
            Repo.Dispose();
        }

        public void SpawnUnit(UnitType type, float x, float y, float vx, float vy)
        {
            var e = Repo.CreateEntity();
            Repo.AddComponent(e, new Position { X = x, Y = y });
            Repo.AddComponent(e, new Velocity { X = vx, Y = vy });
            
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
            
            Repo.AddComponent(e, new RenderSymbol { Symbol = sym, Color = col });
            Repo.AddComponent(e, new UnitStats { Type = type, Health = 100, MaxHealth = 100 });
        }
        
        public void SpawnRandomUnit(UnitType type)
        {
            if (IsReplaying) return;
            
            var rand = new Random();
            float x = rand.Next(5, 55);
            float y = rand.Next(2, 18);
            float vx = rand.Next(-5, 6);
            float vy = rand.Next(-5, 6);
            
            SpawnUnit(type, x, y, vx, vy);
        }
        
        public void RemoveRandomUnit()
        {
            if (IsReplaying) return;
            
            var query = Repo.Query().Build();
            var entities = new List<Entity>();
            query.ForEach(e => entities.Add(e));
            
            if (entities.Count > 0)
            {
                var rand = new Random();
                var toRemove = entities[rand.Next(entities.Count)];
                Repo.DestroyEntity(toRemove);
            }
        }
    }
}
