using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct BoxVisualSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var isServer = state.WorldUnmanaged.IsServer();
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (box, health, transform, entity) in
                 SystemAPI.Query<RefRO<BoxComponent>, RefRO<HealthComponent>, RefRW<LocalTransform>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            float currentHp = health.ValueRO.HealthPoints;
            float maxHp = health.ValueRO.MaxHealthPoints > 0 ? health.ValueRO.MaxHealthPoints : 100f;
            float healthPercent = math.saturate(currentHp / maxHp);

            if (currentHp <= 0)
            {
                if (isServer) ecb.DestroyEntity(entity);
                else ecb.AddComponent<Disabled>(entity);
                continue;
            }

            // 1. SKALA (0.5 - 1.0)
            float scaleMultiplier = math.lerp(0.75f, 1.0f, healthPercent);
            float targetScale = box.ValueRO.InitialScale * scaleMultiplier;

            // 2. KOREKTA POZYCJI - ROZWI¥ZANIE PROPORCJONALNE
            // Skoro przy skali 127 przesuniêcie o 6.35 jest za du¿e, 
            // musimy wiedzieæ jaka jest FIZYCZNA wysokoœæ modelu.
            // Jeœli nie masz zapisanego Height, spróbuj podzieliæ korektê przez 127 (bazê)

            float heightScaleRatio = 1.0f / box.ValueRO.InitialScale;
            float totalScaleLoss = box.ValueRO.InitialScale - targetScale;

            // Korekta pozycji Y:
            // (Utracona Skala * 0.5) * Ratio (¿eby dopasowaæ do skali œwiata)
            // Jeœli p³ot przy skali 127 ma np. 2 metry, to Ratio wynosi ok 0.015
            float offset = (totalScaleLoss * 0.5f) * heightScaleRatio;

            transform.ValueRW.Scale = targetScale;
            transform.ValueRW.Position.y = box.ValueRO.InitialY - offset;
        }
    }
}