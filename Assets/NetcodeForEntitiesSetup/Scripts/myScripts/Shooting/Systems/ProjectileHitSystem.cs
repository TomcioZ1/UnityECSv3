using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
//[BurstCompile]
public partial struct ProjectileHitSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Pobieramy dane celów
        var healthQuery = SystemAPI.QueryBuilder().WithAll<HealthComponent, LocalTransform>().Build();
        var healthEntities = healthQuery.ToEntityArray(Allocator.TempJob);
        var healthTransforms = healthQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var healthDatas = healthQuery.ToComponentDataArray<HealthComponent>(Allocator.TempJob);

        // Szukamy pocisków, które jeszcze "¿yj¹"
        foreach (var (trans, proj, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<ProjectileComponent>>()
                     .WithAll<Simulate>()
                     .WithEntityAccess())
        {
            // Jeœli Lifetime ju¿ jest <= 0, ignorujemy (pocisk ju¿ w coœ trafi³ lub wygas³)
            if (proj.ValueRO.Lifetime <= 0) continue;

            float3 projPos = trans.ValueRO.Position;
            Entity owner = proj.ValueRO.Owner;

            for (int i = 0; i < healthEntities.Length; i++)
            {
                if (healthEntities[i] == owner) continue;

                if (math.distancesq(projPos, healthTransforms[i].Position) <= 0.09f)
                {
                    // TRAFIENIE
                    var health = healthDatas[i];
                    health.HealthPoints -= proj.ValueRO.Damage;
                    ecb.SetComponent(healthEntities[i], health);
                    Debug.Log($"Entity {entity} hit Entity {healthEntities[i]}. New Health: {health.HealthPoints}");

                    // Zamiast niszczyæ, ustawiamy Lifetime na 0
                    // To jest nasz "sygna³" dla drugiego systemu
                    proj.ValueRW.Lifetime = 0;

                    if (state.WorldUnmanaged.IsServer() && health.HealthPoints <= 0)
                    {
                        if (!networkTime.IsFirstTimeFullyPredictingTick) return;
                        ecb.DestroyEntity(healthEntities[i]);
                    }
                    break;
                }
            }
        }

        healthEntities.Dispose();
        healthTransforms.Dispose();
        healthDatas.Dispose();
    }
}