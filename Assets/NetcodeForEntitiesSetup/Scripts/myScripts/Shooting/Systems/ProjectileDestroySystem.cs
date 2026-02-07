using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ProjectileMoveSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
[BurstCompile]
public partial struct ProjectileDestroySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime)) return;

        // Kluczowe w Unity 6: Pobieramy ECB dedykowane dla koñca symulacji
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var isServer = state.WorldUnmanaged.IsServer();
        // Pobieramy aktualny czas ElapsedTime
        var currentTime = SystemAPI.Time.ElapsedTime;

        // Przeszukujemy wszystkie pociski
        foreach (var (proj, entity) in SystemAPI.Query<RefRO<ProjectileComponent>>()
                     .WithEntityAccess())
        {
            // Sprawdzamy, czy czas œmierci ju¿ nadszed³
            if (proj.ValueRO.DeathTime <= currentTime)
            {
                // W Netcode niszczenie/wy³¹czanie wykonujemy tylko w pierwszym ticku predykcji
                if (networkTime.IsFirstTimeFullyPredictingTick)
                {
                    if (isServer)
                    {
                        // Serwer faktycznie usuwa encjê z pamiêci
                        ecb.DestroyEntity(entity);
                    }
                    else
                    {
                        // Klient tylko ukrywa encjê (Disabled). 
                        // Netcode sam j¹ usunie, gdy dostanie potwierdzenie z serwera.
                        ecb.AddComponent<Disabled>(entity);
                    }
                }
            }
        }
    }
}