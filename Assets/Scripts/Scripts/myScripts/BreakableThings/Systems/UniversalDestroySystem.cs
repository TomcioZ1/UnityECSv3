using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct UniversalDestroySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (healthPoints, config, transform, entity) in
                 SystemAPI.Query<RefRO<HealthComponent>, RefRO<DestroyConfig>, RefRO<LocalTransform>>()
                 .WithNone<AlreadyProcessedTag>()
                 .WithEntityAccess())
        {
            if (healthPoints.ValueRO.HealthPoints <= 0)
            {
                ecb.AddComponent<AlreadyProcessedTag>(entity);

                // 1. Pobieramy prefab z konfiguracji
                Entity prefabEntity = config.ValueRO.DropPrefab;

                // 2. ODCZYTUJEMY SKALÊ Z PREFABA (tê zapisan¹ przez Baker)
                // Zak³adam, ¿e BaseScale ma pole .Value (float lub float3)
                var prefabScale = SystemAPI.GetComponent<BaseScale>(prefabEntity).Value;

                var random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + (uint)entity.Index);

                for (int i = 0; i < config.ValueRO.Amount; i++)
                {
                    Entity drop = ecb.Instantiate(prefabEntity);

                    float3 launchVel = random.NextFloat3(new float3(-5, 5, -5), new float3(5, 15, 5));
                    float lifeTime = random.NextFloat(2f, 5f);

                    // 3. Ustawiamy pozycjê, ALE zachowujemy skale wyci¹gniêt¹ z prefaba
                    // Jeœli Twoja skala to float3, u¿ywamy .x (dla jednolitej) lub odpowiedniej metody
                    float uniformScale = prefabScale.x;

                    ecb.SetComponent(drop, LocalTransform.FromPositionRotationScale(
                        transform.ValueRO.Position,
                        quaternion.identity,
                        uniformScale));

                    // 4. Inicjalizujemy dane kropelki
                    ecb.SetComponent(drop, new DestroyedDrop
                    {
                        Velocity = launchVel,
                        MaxLife = lifeTime,
                        RemainingLife = lifeTime,
                        // Przypisujemy wartoœæ, któr¹ wyci¹gnêliœmy z prefaba
                        BaseScale = uniformScale
                    });
                }
            }
        }
    }
}