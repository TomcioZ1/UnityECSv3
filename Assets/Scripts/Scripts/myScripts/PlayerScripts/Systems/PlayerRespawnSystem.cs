using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerRespawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        double currentTime = SystemAPI.Time.ElapsedTime;

        // Szukamy punktu spawnu (zak³adaj¹c, ¿e masz PlayerSpawner na mapie)
        if (!SystemAPI.TryGetSingletonEntity<PlayerSpawner>(out var spawnerEntity)) return;
        var spawnerTransform = SystemAPI.GetComponent<LocalTransform>(spawnerEntity);

        // Szukamy martwych graczy, którym skoñczy³ siê czas kary
        foreach (var (timer, health, transform, entity) in
                 SystemAPI.Query<RefRO<RespawnTimer>, RefRW<HealthComponent>, RefRW<LocalTransform>>()
                 .WithEntityAccess())
        {
            if (currentTime >= timer.ValueRO.RespawnAtTime)
            {
                // 1. Przywracamy ¿ycie
                health.ValueRW.HealthPoints = 100; // Ustaw domyœlne HP

                // 2. Teleportujemy z powrotem na spawn
                transform.ValueRW.Position = spawnerTransform.Position;
                transform.ValueRW.Rotation = spawnerTransform.Rotation;

                // 3. Resetujemy fizykê (na wypadek gdyby pod map¹ nabra³ prêdkoœci)
                if (SystemAPI.HasComponent<PhysicsVelocity>(entity))
                {
                    ecb.SetComponent(entity, new PhysicsVelocity
                    {
                        Linear = float3.zero,
                        Angular = float3.zero
                    });
                }

                // 4. Usuwamy tagi œmierci i timer
                ecb.RemoveComponent<RespawnTimer>(entity);
                ecb.RemoveComponent<IsDestroyedTag>(entity);
            }
        }
    }
}



public struct RespawnTimer : IComponentData
{
    public double RespawnAtTime;
}