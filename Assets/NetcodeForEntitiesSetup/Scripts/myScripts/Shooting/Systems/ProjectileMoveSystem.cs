/*using Unity.Entities;
using Unity.NetCode;
using Unity.Rendering;
using UnityEngine;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
public partial struct ProjectileVisualSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // U¿ywamy czasu systemowego
        double time = SystemAPI.Time.ElapsedTime;

        foreach (var (projectile, entity) in
                 SystemAPI.Query<RefRO<ProjectileComponent>>()
                 .WithAll<DisableRendering>()
                 .WithEntityAccess())
        {
            // WARUNEK: Usuñ tag TYLKO jeœli min¹³ offset
            if (time - projectile.ValueRO.SpawnTime > projectile.ValueRO.TimeOffset)
            {
                ecb.RemoveComponent<DisableRendering>(entity);
                //Debug.Log($"[KLIENT] Pokazujê pocisk {entity.Index}. Offset: {projectile.ValueRO.TimeOffset}");
            }
        }
    }
}*/