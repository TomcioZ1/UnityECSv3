using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct BloodSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Pobieramy ECB i konfiguracjê
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        if (!SystemAPI.TryGetSingleton<BloodPrefabConfig>(out var config)) return;

        // 2. Szukamy graczy z histori¹ zdrowia
        // U¿ywamy WithChangeFilter, ¿eby procesowaæ tylko te encje, których HP siê zmieni³o
        foreach (var (health, history, transform, entity) in
                 SystemAPI.Query<RefRO<HealthComponent>, RefRW<HealthComponentHistory>, RefRO<LocalTransform>>()
                 .WithAll<PlayerTag>()
                 .WithChangeFilter<HealthComponent>()
                 .WithEntityAccess())
        {
            int currentHP = health.ValueRO.HealthPoints;
            int lastHP = history.ValueRO.HealthPoints;

            // Sprawdzamy czy to spadek HP (obra¿enia)
            if (currentHP < lastHP)
            {
                int damageTaken = lastHP - currentHP;
                int count = math.min((int)(damageTaken * 5), 40); // Max 40 kropel na raz

                var random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + (uint)entity.Index);

                for (int i = 0; i < count; i++)
                {
                    Entity drop = ecb.Instantiate(config.BloodDropPrefab);

                    float3 spawnPos = transform.ValueRO.Position + new float3(0, 1.0f, 0);
                    float3 velocity = new float3(
                        random.NextFloat(-3f, 3f),
                        random.NextFloat(2f, 6f),
                        random.NextFloat(-3f, 3f)
                    );

                    ecb.SetComponent(drop, LocalTransform.FromPosition(spawnPos).WithScale(0.12f));
                    ecb.SetComponent(drop, new BloodDrop
                    {
                        Velocity = velocity,
                        RemainingLife = random.NextFloat(0.6f, 1.3f)
                    });
                }
            }

            // Zawsze aktualizujemy historiê, aby zsynchronizowaæ j¹ z obecnym HP
            history.ValueRW.HealthPoints = currentHP;
        }
    }
}