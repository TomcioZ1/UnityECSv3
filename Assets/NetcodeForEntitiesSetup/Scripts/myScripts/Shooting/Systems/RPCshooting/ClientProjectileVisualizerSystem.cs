using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientProjectileVisualizerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ProjectilePrefabNoScary>();
        // Musimy mieæ dostêp do fizyki na kliencie
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var projPrefab = SystemAPI.GetSingleton<ProjectilePrefabNoScary>().Value;

        foreach (var (shotEvent, inventory, transform, entity) in
                 SystemAPI.Query<RefRO<ShotEvent>, RefRO<PlayerInventory>, RefRO<LocalTransform>>()
                 .WithEntityAccess())
        {
            if (!SystemAPI.HasComponent<LastProcessedShot>(entity))
            {
                ecb.AddComponent(entity, new LastProcessedShot { Count = shotEvent.ValueRO.ShotCount });
                continue;
            }

            var lastShot = SystemAPI.GetComponent<LastProcessedShot>(entity);
            if (lastShot.Count == shotEvent.ValueRO.ShotCount) continue;

            Entity weaponEntity = inventory.ValueRO.CurrentWeaponEntity;
            if (weaponEntity == Entity.Null || !SystemAPI.HasComponent<WeaponData>(weaponEntity)) continue;
            var weaponData = SystemAPI.GetComponent<WeaponData>(weaponEntity);

            float3 muzzlePos = transform.ValueRO.Position + math.mul(transform.ValueRO.Rotation, weaponData.ProjectileSpawner);
            float3 baseDir = shotEvent.ValueRO.Direction;

            // Filtr kolizji dla klienta (taki sam jak na serwerze)
            CollisionFilter filter = new CollisionFilter
            {
                BelongsTo = 1u << 4,
                CollidesWith = (1u << 0) | (1u << 3),
                GroupIndex = 0
            };

            if (weaponData.isNormalGun || weaponData.isGranadeLauncher)
            {
                // Dla zwyk³ego karabinu u¿ywamy TargetPos z serwera (jest precyzyjny)
                SpawnVisualProjectile(ecb, projPrefab, muzzlePos, baseDir, shotEvent.ValueRO.TargetPos, weaponData.isGranadeLauncher);
            }
            else if (weaponData.isShotgun)
            {
                float3 up = math.select(new float3(0, 1, 0), new float3(1, 0, 0), math.abs(baseDir.y) > 0.9f);
                float3 right = math.normalize(math.cross(baseDir, up));
                float3 actualUp = math.cross(right, baseDir);
                float spread = 0.05f;

                float3[] offsets = {
                    float3.zero,
                    right * spread, -right * spread,
                    actualUp * spread, -actualUp * spread
                };

                for (int i = 0; i < 5; i++)
                {
                    float3 spreadDir = math.normalize(baseDir + offsets[i]);
                    float maxDist = 15f; // Zasiêg wizualny strzelby
                    float3 rayEnd = muzzlePos + (spreadDir * maxDist);
                    float3 pelletTarget = rayEnd;

                    // KLIENT wykonuje w³asny raycast, ¿eby wiedzieæ gdzie zatrzymaæ œrucinê
                    RaycastInput rayInput = new RaycastInput
                    {
                        Start = muzzlePos + (spreadDir * 0.2f), // Offset startu
                        End = rayEnd,
                        Filter = filter
                    };

                    if (physicsWorld.CastRay(rayInput, out var hit))
                    {
                        pelletTarget = hit.Position;
                    }

                    SpawnVisualProjectile(ecb, projPrefab, muzzlePos, spreadDir, pelletTarget, false);
                }
            }

            ecb.SetComponent(entity, new LastProcessedShot { Count = shotEvent.ValueRO.ShotCount });
        }
    }

    private void SpawnVisualProjectile(EntityCommandBuffer ecb, Entity prefab, float3 pos, float3 dir, float3 target, bool explosive)
    {
        Entity vProj = ecb.Instantiate(prefab);
        ecb.SetComponent(vProj, LocalTransform.FromPositionRotation(pos, quaternion.LookRotationSafe(dir, math.up())));
        ecb.AddComponent(vProj, new VisualProjectile
        {
            Velocity = dir * 20f, // Prêdkoœæ wizualna œrutu (szybciej wygl¹da lepiej)
            TargetPos = target,
            IsNew = true,
            IsExplosive = explosive
        });
    }
}