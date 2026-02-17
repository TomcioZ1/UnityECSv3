using Unity.Burst;
using Unity.Entities;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct ExplosionLifetimeSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // U¿ywamy EndSimulationEntityCommandBufferSystem, aby usun¹æ encjê po zakoñczeniu obliczeñ w klatce
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        float dt = SystemAPI.Time.DeltaTime;

        // Szukamy wszystkich encji z komponentem Lifetime
        foreach (var (lifetime, entity) in SystemAPI.Query<RefRW<Lifetime>>().WithEntityAccess())
        {
            lifetime.ValueRW.RemainingTime -= dt;

            if (lifetime.ValueRW.RemainingTime <= 0)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}