using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct IceDestroyedSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        if (!SystemAPI.TryGetSingleton<IcePrefabConfig>(out var config)) return;

        foreach (var (health, ice, transform, entity) in
                 SystemAPI.Query<RefRO<HealthComponent>, RefRW<IceTag>, RefRO<LocalTransform>>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.HealthPoints <= 0 && !ice.ValueRO.IsDestroyed)
            {
                ice.ValueRW.IsDestroyed = true;

                // UNIKALNE ZIARNO: ElapsedTime + Index encji gwarantuje ró¿norodnoœæ
                var random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + (uint)entity.Index);

                for (int i = 0; i < 60; i++)
                {
                    Entity drop = ecb.Instantiate(config.IceDropPrefab);

                    // Offset startowy
                    float3 spawnPos = transform.ValueRO.Position + new float3(0, -2f, 0);

                    // Rozrzucona prêdkoœæ
                    float3 launchVel = new float3(
                        random.NextFloat(-2.5f, 2.5f),
                        random.NextFloat(6f, 12f),
                        random.NextFloat(-2.5f, 2.5f)
                    );

                    ecb.SetComponent(drop, LocalTransform.FromPosition(spawnPos).WithScale(1f));
                    ecb.SetComponent(drop, new IceDrop
                    {
                        Velocity = launchVel,
                        RemainingLife = random.NextFloat(1.0f, 2.5f)
                    });
                }
            }
        }
    }
}