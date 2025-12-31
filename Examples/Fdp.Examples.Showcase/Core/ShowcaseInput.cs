using System;
using Fdp.Kernel;
using Fdp.Kernel.FlightRecorder;
using Fdp.Examples.Showcase.Components;

namespace Fdp.Examples.Showcase.Core
{
    public class ShowcaseInput
    {
        private readonly ShowcaseGame _game;

        public ShowcaseInput(ShowcaseGame game)
        {
            _game = game;
        }

        public void HandleInput()
        {
            if (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                
                if (_game.ShowInspector)
                {
                    bool inspectorHandled = _game.Inspector.HandleInput(keyInfo);
                    if (inspectorHandled) return;
                }
                
                var key = keyInfo.Key;
                var shift = (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0;
                var ctrl = (keyInfo.Modifiers & ConsoleModifiers.Control) != 0;
                
                switch(key)
                {
                    case ConsoleKey.Escape: 
                        _game.IsRunning = false; 
                        break;
                        
                    case ConsoleKey.R: 
                        _game.IsRecording = !_game.IsRecording; 
                        break;
                    
                    case ConsoleKey.I:
                        _game.ShowInspector = !_game.ShowInspector;
                        break;
                        
                    case ConsoleKey.Spacebar:
                        _game.IsPaused = !_game.IsPaused;
                        break;
                        
                    case ConsoleKey.P: 
                        if (!_game.IsReplaying)
                        {
                            _game.IsRecording = false;
                            _game.DiskRecorder?.Dispose();
                            _game.DiskRecorder = null;
                            
                            try
                            {
                                _game.PlaybackController = new PlaybackController(_game.RecordingFilePath);
                                _game.IsReplaying = true;
                                
                                if (_game.PlaybackController.TotalFrames > 0)
                                {
                                    _game.PlaybackController.Rewind(_game.Repo);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load recording: {ex.Message}");
                            }
                        }
                        else
                        {
                            _game.IsReplaying = false;
                            _game.PlaybackController?.Dispose();
                            _game.PlaybackController = null;
                        }
                        break;
                        
                    case ConsoleKey.LeftArrow:
                        if (_game.IsReplaying && _game.PlaybackController != null)
                        {
                            int step = ctrl ? 100 : (shift ? 10 : 1);
                            int targetFrame = Math.Max(0, _game.PlaybackController.CurrentFrame - step);
                            _game.PlaybackController.SeekToFrame(_game.Repo, targetFrame);
                        }
                        break;
                        
                    case ConsoleKey.RightArrow:
                        if (_game.IsReplaying && _game.PlaybackController != null)
                        {
                            int step = ctrl ? 100 : (shift ? 10 : 1);
                            int targetFrame = Math.Min(_game.PlaybackController.TotalFrames - 1, _game.PlaybackController.CurrentFrame + step);
                            _game.PlaybackController.SeekToFrame(_game.Repo, targetFrame);
                        }
                        else if (_game.IsPaused && !_game.IsReplaying)
                        {
                            _game.SingleStep = true;
                        }
                        break;
                        
                    case ConsoleKey.Home:
                        if (_game.IsReplaying && _game.PlaybackController != null)
                        {
                            _game.PlaybackController.Rewind(_game.Repo);
                        }
                        break;
                        
                    case ConsoleKey.End:
                        if (_game.IsReplaying && _game.PlaybackController != null)
                        {
                            _game.PlaybackController.SeekToFrame(_game.Repo, _game.PlaybackController.TotalFrames - 1);
                        }
                        break;
                        
                    case ConsoleKey.D1: _game.SpawnRandomUnit(UnitType.Infantry); break;
                    case ConsoleKey.D2: _game.SpawnRandomUnit(UnitType.Tank); break;
                    case ConsoleKey.D3: _game.SpawnRandomUnit(UnitType.Aircraft); break;
                    
                    case ConsoleKey.Delete:
                    case ConsoleKey.Backspace:
                        _game.RemoveRandomUnit();
                        break;
                }
            }
        }
    }
}
