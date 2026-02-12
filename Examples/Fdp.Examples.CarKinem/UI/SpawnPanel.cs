using System.Numerics;
using ImGuiNET;
using Fdp.Kernel;
using CarKinem.Core;
using CarKinem.Trajectory;

namespace Fdp.Examples.CarKinem.UI;

public class SpawnPanel
{
    private VehicleClass _selectedClass = VehicleClass.PersonalCar;
    private int _spawnCount = 1;

    public void Draw(EntityRepository world, TrajectoryPoolManager? trajPool = null)
    {
        if (!ImGui.Begin("Spawn Controls"))
        {
            ImGui.End();
            return;
        }

        // Vehicle Class Selection
        string[] names = System.Enum.GetNames(typeof(VehicleClass));
        int current = (int)_selectedClass;
        if (ImGui.Combo("Vehicle Class", ref current, names, names.Length))
        {
            _selectedClass = (VehicleClass)current;
        }

        ImGui.Separator();

        ImGui.InputInt("Count", ref _spawnCount);
        if (_spawnCount < 1) _spawnCount = 1;

        if (ImGui.Button("Spawn at Center"))
        {
            for (int i = 0; i < _spawnCount; i++)
            {
                SpawnVehicle(world, new Vector2(0, 0) + new Vector2(i * 5, 0));
            }
        }
        
        if (ImGui.Button("Spawn Random"))
        {
            for (int i = 0; i < _spawnCount; i++)
            {
                var pos = new Vector2(
                    (float)(System.Random.Shared.NextDouble() * 100 - 50), 
                    (float)(System.Random.Shared.NextDouble() * 100 - 50));
                
                SpawnVehicle(world, pos);
            }
        }
        
        ImGui.End();
    }

    private void SpawnVehicle(EntityRepository repo, Vector2 pos)
    {
        var e = repo.CreateEntity();
        
        // Random Heading
        float angle = (float)(System.Random.Shared.NextDouble() * MathF.PI * 2);
        Vector2 fwd = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        
        repo.AddComponent(e, new VehicleState 
        { 
            Position = pos,
            Forward = fwd,
            Speed = 0
        });
        
        repo.AddComponent(e, VehiclePresets.GetPreset(_selectedClass));
        repo.AddComponent(e, new NavState());
    }
}
