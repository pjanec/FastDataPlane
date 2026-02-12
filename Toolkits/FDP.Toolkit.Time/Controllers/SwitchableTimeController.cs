using System;
using Fdp.Kernel;
using ModuleHost.Core.Time;

namespace FDP.Toolkit.Time.Controllers
{
    /// <summary>
    /// A proxy controller that allows hotswapping the underlying time strategy
    /// even if the Kernel locks the controller instance reference.
    /// </summary>
    public class SwitchableTimeController : ITimeController
    {
        private ITimeController _activeController;
        
        public SwitchableTimeController(ITimeController initial)
        {
            _activeController = initial ?? throw new ArgumentNullException(nameof(initial));
        }

        public void SwitchTo(ITimeController newController)
        {
            if (newController == null) throw new ArgumentNullException(nameof(newController));
            if (_activeController == newController) return;

            // Seed the new controller with current state
            var currentState = _activeController.GetCurrentState();
            newController.SeedState(currentState);
            
            _activeController = newController;
        }

        public ITimeController ActiveController => _activeController;

        // --- Proxy Implementation ---

        public GlobalTime Update() => _activeController.Update();
        
        public void SetTimeScale(float scale) => _activeController.SetTimeScale(scale);
        
        public float GetTimeScale() => _activeController.GetTimeScale();
        
        public TimeMode GetMode() => _activeController.GetMode();
        
        public GlobalTime GetCurrentState() => _activeController.GetCurrentState();
        
        public void SeedState(GlobalTime state) => _activeController.SeedState(state);
        
        public void Dispose()
        {
            _activeController.Dispose();
        }
    }
}
