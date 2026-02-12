using System.Numerics;
using Raylib_cs;
using Fdp.Kernel;
using FDP.Toolkit.Vis2D.Abstractions;
using CarKinem.Core;
using ModuleHost.Core.Abstractions;

namespace Fdp.Examples.CarKinem.Visualization;

public class VehicleVisualizer : IVisualizerAdapter
{
    public Vector2? GetPosition(ISimulationView view, Entity entity)
    {
        if (view.HasComponent<VehicleState>(entity))
        {
            return view.GetComponentRO<VehicleState>(entity).Position;
        }
        return null;
    }

    public float GetHitRadius(ISimulationView view, Entity entity)
    {
        if (view.HasComponent<VehicleParams>(entity))
        {
             return view.GetComponentRO<VehicleParams>(entity).Length / 2f;
        }
        return 1.0f;
    }

    public void Render(ISimulationView view, Entity entity, Vector2 position, RenderContext ctx, bool isSelected, bool isHovered)
    {
        if (!view.HasComponent<VehicleState>(entity) || !view.HasComponent<VehicleParams>(entity))
            return;

        ref readonly var state = ref view.GetComponentRO<VehicleState>(entity);
        ref readonly var parameters = ref view.GetComponentRO<VehicleParams>(entity);

        var (r, g, b) = VehiclePresets.GetColor(parameters.Class);
        var color = new Color(r, g, b, (byte)255);

        // Highlight if selected
        if (isSelected)
        {
            color = Color.Green;
        }
        else if (isHovered)
        {
            color = new Color((byte)Math.Min(r + 50, 255), (byte)Math.Min(g + 50, 255), (byte)Math.Min(b + 50, 255), (byte)255);
        }

        // Draw rotated rectangle
        float rotationDeg = MathF.Atan2(state.Forward.Y, state.Forward.X) * (180.0f / MathF.PI);
        
        Rectangle rec = new Rectangle(position.X, position.Y, parameters.Length, parameters.Width);
        Vector2 origin = new Vector2(parameters.Length / 2, parameters.Width / 2); // Center of rotation
        
        Raylib.DrawRectanglePro(rec, origin, rotationDeg, color);

        // Draw front indicator (a line or triangle)
        // Let's just draw a small line from center to front
        Vector2 front = position + state.Forward * (parameters.Length / 2);
        Raylib.DrawLineV(position, front, Color.Black);
        
        // Draw selection ring
        if (isSelected)
        {
            Raylib.DrawCircleLines((int)position.X, (int)position.Y, parameters.Length * 0.7f, Color.White);
        }
    }
    
    public string? GetHoverLabel(ISimulationView view, Entity entity)
    {
        if (view.HasComponent<VehicleParams>(entity))
        {
            var p = view.GetComponentRO<VehicleParams>(entity);
            return $"{p.Class} #{entity.Index}";
        }
        return null;
    }
}
