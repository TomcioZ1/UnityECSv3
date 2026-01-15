using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Pobieramy singleton prefaba
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var prefab)) return;

        // U¿ywamy ECB, aby bezpiecznie tworzyæ encje wewn¹trz pêtli
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (input, transform, entity) in
                 SystemAPI.Query<RefRO<PlayerShootInput>, RefRO<LocalTransform>>()
                 .WithAll<Simulate>() // Simulate oznacza, ¿e to encja podlegaj¹ca predykcji
                 .WithEntityAccess())
        {
            if (input.ValueRO.ShootPrimary == 0) continue;

            // SPAWNOWANIE
            Entity projectile = ecb.Instantiate(prefab.Value);

            // Obliczamy pozycjê (np. na wysokoœci klatki piersiowej)
            float3 spawnPos = transform.ValueRO.Position + new float3(0, 1.2f, 0);
            float3 direction = math.normalize(input.ValueRO.AimDirection);

            // Ustawiamy transformacjê pocisku
            ecb.SetComponent(projectile, LocalTransform.FromPosition(spawnPos));

            // Inicjalizujemy dane pocisku
            ecb.SetComponent(projectile, new ProjectileComponent
            {
                Velocity = direction * 25f,
                Lifetime = 3.0f,
                Owner = entity // Zapisujemy kto strzeli³
            });
        }
    }
}