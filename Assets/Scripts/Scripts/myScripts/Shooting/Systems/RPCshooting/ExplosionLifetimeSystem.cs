using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct ExplosionLifetimeSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        float dt = SystemAPI.Time.DeltaTime;

        // Zapytanie o Lifetime ORAZ DissolveProperty
        foreach (var (lifetime, dissolve, entity) in
                 SystemAPI.Query<RefRW<Lifetime>, RefRW<DissolveProperty>>()
                 .WithEntityAccess())
        {
            lifetime.ValueRW.RemainingTime -= dt;

            // Obliczamy postêp: 1.0 - (0.7 / 0.7) = 0 na pocz¹tku
            // Pod koniec: 1.0 - (0 / 0.7) = 1.0 na koñcu
            float progress = 1.0f - math.saturate(lifetime.ValueRO.RemainingTime / lifetime.ValueRO.TotalDuration);

            // Przypisujemy do parametru shadera
            dissolve.ValueRW.Value = progress;

            if (lifetime.ValueRO.RemainingTime <= 0)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}