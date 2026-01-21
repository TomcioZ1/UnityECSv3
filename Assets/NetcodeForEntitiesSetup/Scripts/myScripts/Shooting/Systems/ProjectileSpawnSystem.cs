using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Pobieramy singleton prefaba
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var prefab)) return;

        // 2. Pobieramy domyœlny LocalTransform z prefaba (zapisana tam skala 0.3)
        // Robimy to raz poza pêtl¹ dla wydajnoœci
        var prefabTransform = state.EntityManager.GetComponentData<LocalTransform>(prefab.Value);

        // 3. Przygotowanie Command Buffera
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 4. Pêtla po graczach, którzy mog¹ strzelaæ
        foreach (var (input, transform, entity) in
                 SystemAPI.Query<RefRO<PlayerShootInput>, RefRO<LocalTransform>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // Sprawdzamy czy oddano strza³
            if (input.ValueRO.ShootPrimary == 0) continue;

            // SPAWNOWANIE
            Entity projectile = ecb.Instantiate(prefab.Value);

            // Obliczamy pozycjê wylotu pocisku
            float3 spawnPos = transform.ValueRO.Position + new float3(0, 0.2f, 0);

            // Bezpieczna normalizacja kierunku (zapobiega b³êdom NaN)
            float3 direction = math.normalizesafe(input.ValueRO.AimDirection);

            // 5. Ustawiamy transformacjê pocisku
            // Kopiujemy dane z prefaba (skala!) i podmieniamy tylko pozycjê i rotacjê
            ecb.SetComponent(projectile, new LocalTransform
            {
                Position = spawnPos,
                Rotation = quaternion.LookRotationSafe(direction, math.up()),
                Scale = prefabTransform.Scale // To wymusza skalê 0.3 z inspektora
            });

            // 6. Inicjalizacja logiki pocisku
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