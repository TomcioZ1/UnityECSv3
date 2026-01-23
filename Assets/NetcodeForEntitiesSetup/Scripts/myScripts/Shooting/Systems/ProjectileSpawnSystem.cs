using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var prefab)) return;

        // U¿ywamy bezpieczniejszego sposobu na pobranie transformacji prefaba
        var prefabTransform = state.EntityManager.GetComponentData<LocalTransform>(prefab.Value);

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // POPRAWKA: Kolejnoœæ w pêtli musi odpowiadaæ Query + Entity na koñcu
        foreach (var (input, transform, entity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<LocalTransform>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // Zmiana: strza³ wyzwalany gdy wartoœæ jest 1 (zak³adaj¹c InputEvent lub int)
            if (input.ValueRO.leftMouseButton == 0) continue;

            Entity projectile = ecb.Instantiate(prefab.Value);

            // Obliczamy pozycjê wylotu
            float3 spawnPos = transform.ValueRO.Position + new float3(0, 0.2f, 0);
            float3 direction = math.normalizesafe(input.ValueRO.AimDirection);

            // Zamiast tworzyæ "new LocalTransform", lepiej zmodyfikowaæ kopiê z prefaba
            var projectileTransform = prefabTransform;
            projectileTransform.Position = spawnPos;
            projectileTransform.Rotation = quaternion.LookRotationSafe(direction, math.up());

            ecb.SetComponent(projectile, projectileTransform);

            ecb.SetComponent(projectile, new ProjectileComponent
            {
                Damage = 10,
                Velocity = direction * 2f,
                Lifetime = 3.0f,
                Owner = entity
            });
        }
    }
}