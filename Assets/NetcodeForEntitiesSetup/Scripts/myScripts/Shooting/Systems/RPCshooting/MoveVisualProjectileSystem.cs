using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct MoveVisualProjectileSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        Entity explosionEffectPrefab = Entity.Null;
        if (SystemAPI.HasSingleton<ExplosionPrefab>())
            explosionEffectPrefab = SystemAPI.GetSingleton<ExplosionPrefab>().Value;

        foreach (var (transform, proj, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<VisualProjectile>>()
                 .WithEntityAccess())
        {
            float3 currentPos = transform.ValueRO.Position;
            float3 nextPos = currentPos + (proj.ValueRO.Velocity * dt);

            // Sprawdzamy czy pocisk min¹³ ju¿ TargetPos
            float distToTargetSq = math.distancesq(currentPos, proj.ValueRO.TargetPos);
            float frameDistSq = math.distancesq(currentPos, nextPos);

            // Jeœli odleg³oœæ do celu jest mniejsza ni¿ dystans, który pokonamy w tej klatce -> TRAFIENIE
            if (distToTargetSq <= frameDistSq)
            {
                if (proj.ValueRO.IsNew) // Zabezpieczenie przed znikniêciem w 1 klatce
                {
                    proj.ValueRW.IsNew = false;
                    continue;
                }

                // Logika wybuchu wizualnego
                if (proj.ValueRO.IsExplosive && explosionEffectPrefab != Entity.Null)
                {
                    Entity exp = ecb.Instantiate(explosionEffectPrefab);
                    ecb.SetComponent(exp, LocalTransform.FromPosition(proj.ValueRO.TargetPos));
                    // Tutaj mo¿esz dodaæ komponent usuwaj¹cy wybuch po 1 sekundzie (np. Lifetime)
                }

                transform.ValueRW.Position = proj.ValueRO.TargetPos;
                ecb.DestroyEntity(entity);
            }
            else
            {
                transform.ValueRW.Position = nextPos;
                proj.ValueRW.IsNew = false;
            }
        }
    }
}