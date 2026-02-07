using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial struct WorldMonitorSystem : ISystem
{
    private double _nextLogTime;
    private const double LogInterval = 5.0;

    public void OnUpdate(ref SystemState state)
    {
        // W ECS World.Time.ElapsedTime to domyœlnie double
        double currentTime = state.World.Time.ElapsedTime;

        if (currentTime >= _nextLogTime)
        {
            LogActiveWorlds();
            _nextLogTime = currentTime + LogInterval;
        }
    }

    private void LogActiveWorlds()
    {
        string report = "<color=#00FFFF>[WORLD MONITOR]</color> Aktywne instancje œwiatów:\n";

        foreach (var world in World.All)
        {
            string type = GetWorldTypeDescription(world);
            report += $"- <b>{world.Name}</b> | Typ: {type}\n";
        }

        Debug.Log(report);
    }

    private string GetWorldTypeDescription(World world)
    {
        // U¿ywamy metod rozszerzaj¹cych z Unity.NetCode
        if (world.IsServer())
        {
            return "<color=#FF4444>SERVER WORLD</color>";
        }

        if (world.IsClient())
        {
            if (world.IsThinClient())
                return "<color=#FFFF00>THIN CLIENT</color>";

            return "<color=#4444FF>CLIENT WORLD</color>";
        }

        return "<color=#AAAAAA>LOCAL/OTHER</color>";
    }
}