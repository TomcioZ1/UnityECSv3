using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using Unity.Burst;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile] // Komentujemy Burst, aby Debug.Log zadzia³a³ w konsoli
public partial struct PerformanceMonitorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float currentFps = deltaTime > 0 ? 1.0f / deltaTime : 0;

        if (SystemAPI.TryGetSingletonRW<PerformanceStats>(out var stats))
        {
            // FPS - wyg³adzanie
            stats.ValueRW.FPS = math.lerp(stats.ValueRO.FPS, currentFps, 0.1f);

            // Pobieranie Pingu
            if (SystemAPI.TryGetSingleton<NetworkSnapshotAck>(out var networkAck))
            {
                stats.ValueRW.Ping = networkAck.EstimatedRTT;
            }
            

            // DEBUG LOG - Wyœwietli siê w konsoli Unity
            // Zaokr¹glamy FPS do 1 miejsca po przecinku dla czytelnoœci
            //Debug.Log($"[Performance] FPS: {stats.ValueRO.FPS:F1} | Ping: {stats.ValueRO.Ping}ms");
        }
    }
}