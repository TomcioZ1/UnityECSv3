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

        // 1. Pobieramy spawner i jego bufor punktów
        if (!SystemAPI.TryGetSingletonEntity<PlayerSpawner>(out var spawnerEntity)) return;

        var spawnPoints = SystemAPI.GetBuffer<SpawnPointElement>(spawnerEntity);

        // Inicjalizacja losowoœci
        var random = new Unity.Mathematics.Random((uint)(currentTime * 1000) + 1);

        // Szukamy martwych graczy, którym skoñczy³ siê czas kary
        foreach (var (timer, health, transform, entity) in
                 SystemAPI.Query<RefRO<RespawnTimer>, RefRW<HealthComponent>, RefRW<LocalTransform>>()
                 .WithEntityAccess())
        {
            if (currentTime >= timer.ValueRO.RespawnAtTime)
            {
                // 2. Przywracamy ¿ycie
                health.ValueRW.HealthPoints = 100;

                // 3. Wybieramy now¹ pozycjê z listy punktów
                float3 respawnPos = float3.zero;
                if (spawnPoints.Length > 0)
                {
                    int randomIndex = random.NextInt(0, spawnPoints.Length);
                    respawnPos = spawnPoints[randomIndex].Position;
                }

                // 4. Teleportujemy na wylosowany spawn
                transform.ValueRW.Position = respawnPos;
                transform.ValueRW.Rotation = quaternion.identity; // Mo¿esz te¿ dodaæ rotacjê do SpawnPointElement

                // 5. Resetujemy fizykê
                if (SystemAPI.HasComponent<PhysicsVelocity>(entity))
                {
                    ecb.SetComponent(entity, new PhysicsVelocity
                    {
                        Linear = float3.zero,
                        Angular = float3.zero
                    });
                }

                // 6. Usuwamy komponenty stanu œmierci
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