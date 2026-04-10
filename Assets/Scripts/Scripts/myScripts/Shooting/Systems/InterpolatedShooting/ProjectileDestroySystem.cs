/*using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ProjectileSimulationSystem))]
[BurstCompile]
public partial struct ProjectileDestroySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime)) return;
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var isServer = state.WorldUnmanaged.IsServer();
        var currentTime = SystemAPI.Time.ElapsedTime;

        foreach (var (proj, entity) in
                 SystemAPI.Query<RefRO<ProjectileComponent>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            if (proj.ValueRO.DeathTime <= currentTime)
            {
                if (isServer)
                {
                    ecb.DestroyEntity(entity);
                }
                else
                {
                    // Na kliencie dodajemy Disabled. 
                    // Netcode sam zniszczy Ghosta po otrzymaniu info z serwera.
                    ecb.AddComponent<Disabled>(entity);
                }
            }
        }
    }
}*/