using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(ProjectileMoveSystem))]
// DODAJEMY FILTR - bez tego system mo¿e dzia³aæ w pustym œwiecie bakingu
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
[BurstCompile]
public partial struct ProjectileDestroySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Upewniamy siê, ¿e system czeka na czas sieciowy
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. SprawdŸ czy mamy czas (bezpiecznik)
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime)) return;

        // 2. Pobierz ECB (u¿ywamy najprostszego sposobu dla Unity 6)
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var isServer = state.WorldUnmanaged.IsServer();

        // 3. PÊTLA - usun¹³em Simulate i doda³em logowanie na samym pocz¹tku
        foreach (var (proj, entity) in SystemAPI.Query<RefRO<ProjectileComponent>>()
                     .WithEntityAccess()) // USUNIÊTO: Simulate, Disabled
        {
            // Ten log MUSI siê pojawiæ, jeœli pocisk istnieje w tym œwiecie
            //Debug.Log($"[DestroySystem] Widzê encjê {entity.Index}. Lifetime: {proj.ValueRO.Lifetime}");

            if (proj.ValueRO.Lifetime <= 0)
            {
                // Niszczymy tylko w pierwszym ticku predykcji (standard Netcode)
                if (networkTime.IsFirstTimeFullyPredictingTick)
                {
                    if (isServer)
                    {
                        ecb.DestroyEntity(entity);
                    }
                    else
                    {
                        // Dodajemy disabled, ¿eby klient go nie widzia³
                        ecb.AddComponent<Disabled>(entity);
                    }
                }
            }
        }
    }
}