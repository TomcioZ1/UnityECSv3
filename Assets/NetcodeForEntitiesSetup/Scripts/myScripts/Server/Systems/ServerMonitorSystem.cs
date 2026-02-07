using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct ServerMonitorSystem : ISystem
{
    private double _nextLogTime;
    private const double LogInterval = 20.0;

    public void OnCreate(ref SystemState state)
    {
        // Upewniamy siê, ¿e œwiat Netcode jest gotowy
        state.RequireForUpdate<NetworkTime>();
        _nextLogTime = 0;
    }

    public void OnUpdate(ref SystemState state)
    {
        // U¿ywamy czasu œwiata (World.Time), który w ECS jest standardem
        // W ServerSimulation jest on to¿samy z czasem serwera
        double currentTime = state.World.Time.ElapsedTime;

        if (currentTime >= _nextLogTime)
        {
            LogServerStatus(ref state);
            _nextLogTime = currentTime + LogInterval;
        }
    }

    private void LogServerStatus(ref SystemState state)
    {
        float currentDelta = state.World.Time.DeltaTime;
        float actualTickRate = currentDelta > 0 ? 1.0f / currentDelta : 0;

        // Liczymy graczy
        var playerQuery = state.GetEntityQuery(ComponentType.ReadOnly<NetworkId>());
        int playerCount = playerQuery.CalculateEntityCount();

        // Sprawdzamy obci¹¿enie (dla TickRate 30)
        float targetDelta = 1.0f / 30.0f;
        bool isOverloaded = currentDelta > (targetDelta * 1.15f);

        string statusIndicator = isOverloaded ? "[OVERLOADED]" : "[STABLE]";

        Debug.Log($"<color=#00FF00>[SERVER INFO]</color> {statusIndicator}\n" +
                  $"Graczy online: {playerCount} | " +
                  $"TickRate: {actualTickRate:F1} Hz | " +
                  $"FrameTime: {currentDelta * 1000:F2} ms");
    }
}