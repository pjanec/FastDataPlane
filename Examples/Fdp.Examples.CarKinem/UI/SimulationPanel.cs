using ImGuiNET;
using Fdp.Kernel;
using ModuleHost.Core;

namespace Fdp.Examples.CarKinem.UI;

public class SimulationPanel
{
    private float _timeScale = 1.0f;
    private bool _paused = false;

    public void Draw(ModuleHostKernel kernel)
    {
        if (!ImGui.Begin("Simulation"))
        {
            ImGui.End();
            return;
        }

        var time = kernel.CurrentTime;
        
        ImGui.Text($"Frame: {time.FrameNumber}");
        ImGui.Text($"Time: {time.TotalTime:F2}s");
        ImGui.Text($"Delta: {time.DeltaTime*1000:F1}ms");

        ImGui.Separator();

        bool paused = _paused;
        if (ImGui.Checkbox("Pause", ref paused))
        {
            _paused = paused;
            kernel.SetTimeScale(_paused ? 0.0f : _timeScale);
        }

        float scale = _timeScale;
        if (ImGui.SliderFloat("Time Scale", ref scale, 0.1f, 10.0f))
        {
            _timeScale = scale;
            if (!_paused)
            {
                kernel.SetTimeScale(_timeScale);
            }
        }
        
        if (ImGui.Button("Reset Scale"))
        {
            _timeScale = 1.0f;
            _paused = false;
            kernel.SetTimeScale(1.0f);
        }

        ImGui.End();
    }
}
