using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

using MaterialPropertyBaseColor = Unity.Rendering.URPMaterialPropertyBaseColor;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation)]
[BurstCompile]
public partial struct FootprintDestroySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (lifetime, color, entity) in
                 SystemAPI.Query<RefRW<FootprintLifeTime>, RefRW<MaterialPropertyBaseColor>>()
                 .WithEntityAccess())
        {
            lifetime.ValueRW.Value -= deltaTime; 

            // Obliczamy ile % ¿ycia zosta³o (1.0 -> 0.0)
            float lifeRatio = lifetime.ValueRO.Value / lifetime.ValueRO.MaxValue;

            float3 targetColor = new float3(0, 0, 0);

            // Jeœli zosta³o mniej ni¿ 40% czasu
            if (lifeRatio <= 0.4f)
            {
                // Obliczamy postêp szarzenia (0.0 przy 40% ¿ycia -> 1.0 przy 0% ¿ycia)
                // Odwracamy ratio: 1.0 - (lifeRatio / 0.4f)
                float grayProgress = 1.0f - math.saturate(lifeRatio / 0.4f);

                // Interpolujemy od czarnego (0) do szarego (np. 0.5f)
                // Jeœli chcesz do bia³ego, zmieñ 0.5f na 1.0f
                float grayValue = math.lerp(0f, 0.5f, grayProgress);
                targetColor = new float3(grayValue, grayValue, grayValue);
            }

            // Ustawiamy kolor RGB, zostawiaj¹c Alfê na 1 (brak przezroczystoœci)
            color.ValueRW.Value = new float4(targetColor, 1.0f);

            if (lifetime.ValueRO.Value <= 0f)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}