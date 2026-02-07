using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
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
                 SystemAPI.Query<RefRW<BoxComponent>, RefRO<HealthComponent>, RefRW<LocalTransform>>()
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

            // 1. OBLICZANIE MNO¯NIKA
            float scaleMultiplier = math.lerp(0.75f, 1.0f, healthPercent);
            float targetScale = box.ValueRO.InitialScale * scaleMultiplier;

            // 2. KOREKTA POZYCJI DLA DU¯YCH BUDYNKÓW
            // multiplierDiff mówi nam o ile procent (0.0 - 0.25) skurczy³ siê budynek
            float multiplierDiff = 1.0f - scaleMultiplier;

            // worldHeight to fizyczna wysokoœæ budynku w œwiecie
            float worldHeight = box.ValueRO.MeshHeight * box.ValueRO.InitialScale;

            // worldCenter to przesuniêcie œrodka w skali œwiata
            float worldCenter = box.ValueRO.CenterOffset * box.ValueRO.InitialScale;

            // NOWY WZÓR: Uwzglêdnia niesymetryczne modele. 
            // Przesuwa obiekt w dó³ o utracony procent odleg³oœci od Pivotu do podstawy.
            float offset = multiplierDiff * (worldHeight * 0.5f + worldCenter);

            transform.ValueRW.Scale = targetScale;
            transform.ValueRW.Position.y = box.ValueRO.InitialY - offset;
        }
    }
}