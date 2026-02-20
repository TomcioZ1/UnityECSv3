using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct ClientProjectileVisualizerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ProjectilePrefabNoScary>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // U¿ywamy bezpiecznego ECB
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var projPrefab = SystemAPI.GetSingleton<ProjectilePrefabNoScary>().Value;

        foreach (var (shotEvent, inventory, transform, entity) in
                 SystemAPI.Query<RefRO<ShotEvent>, RefRO<PlayerInventory>, RefRO<LocalTransform>>()
                 .WithEntityAccess())
        {
            // 1. Sprawdzenie duplikacji strza³ów
            if (!SystemAPI.HasComponent<LastProcessedShot>(entity))
            {
                ecb.AddComponent(entity, new LastProcessedShot { Count = shotEvent.ValueRO.ShotCount });
                continue;
            }

            if (SystemAPI.GetComponent<LastProcessedShot>(entity).Count == shotEvent.ValueRO.ShotCount)
                continue;

            // 2. Pobranie danych broni
            Entity weaponEntity = inventory.ValueRO.CurrentWeaponEntity;
            if (weaponEntity == Entity.Null || !SystemAPI.HasComponent<WeaponData>(weaponEntity))
                continue;

            var weaponData = SystemAPI.GetComponent<WeaponData>(weaponEntity);
            float3 muzzlePos = transform.ValueRO.Position + math.mul(transform.ValueRO.Rotation, weaponData.ProjectileSpawner);
            float3 baseDir = shotEvent.ValueRO.Direction;

            // Filtr kolizji
            CollisionFilter filter = new CollisionFilter
            {
                BelongsTo = 1u << 4,
                CollidesWith = (1u << 0) | (1u << 3),
                GroupIndex = 0
            };

            // 3. Logika strza³u
            if (weaponData.isNormalGun || weaponData.isGranadeLauncher)
            {
                SpawnVisualProjectile(ecb, projPrefab, muzzlePos, baseDir, shotEvent.ValueRO.TargetPos, weaponData.isGranadeLauncher);
                TriggerSound(ecb, 0, muzzlePos, false);
            }
            else if (weaponData.isShotgun)
            {
                ExecuteShotgunSpread(ref state, ecb, projPrefab, muzzlePos, baseDir, weaponData.maxRange, filter, physicsWorld);
                TriggerSound(ecb, 1, muzzlePos, false); // DŸwiêk raz dla ca³ego strza³u
            }

            // Aktualizacja licznika przetworzonych strza³ów
            ecb.SetComponent(entity, new LastProcessedShot { Count = shotEvent.ValueRO.ShotCount });
        }
    }

    [BurstCompile]
    private void ExecuteShotgunSpread(ref SystemState state, EntityCommandBuffer ecb, Entity prefab, float3 origin, float3 direction, float range, CollisionFilter filter, PhysicsWorld world)
    {
        float3 up = math.select(new float3(0, 1, 0), new float3(1, 0, 0), math.abs(direction.y) > 0.9f);
        float3 right = math.normalize(math.cross(direction, up));
        float3 actualUp = math.cross(right, direction);
        float spread = 0.05f;

        // Najbezpieczniejsza metoda dla Burst: Brak tablic, bezpoœrednie pêtle lub sta³e przesuniêcia
        for (int i = 0; i < 5; i++)
        {
            float3 offset = float3.zero;
            if (i == 1) offset = right * spread;
            else if (i == 2) offset = -right * spread;
            else if (i == 3) offset = actualUp * spread;
            else if (i == 4) offset = -actualUp * spread;

            float3 spreadDir = math.normalize(direction + offset);
            float3 rayEnd = origin + (spreadDir * range);
            float3 pelletTarget = rayEnd;

            RaycastInput rayInput = new RaycastInput
            {
                Start = origin + (spreadDir * 0.1f),
                End = rayEnd,
                Filter = filter
            };

            if (world.CastRay(rayInput, out var hit))
            {
                pelletTarget = hit.Position;
            }

            SpawnVisualProjectile(ecb, prefab, origin, spreadDir, pelletTarget, false);
        }
    }

    [BurstCompile]
    private void SpawnVisualProjectile(EntityCommandBuffer ecb, Entity prefab, float3 pos, float3 dir, float3 target, bool explosive)
    {
        Entity vProj = ecb.Instantiate(prefab);
        ecb.SetComponent(vProj, LocalTransform.FromPositionRotation(pos, quaternion.LookRotationSafe(dir, math.up())));
        ecb.AddComponent(vProj, new VisualProjectile
        {
            Velocity = dir * 25f,
            TargetPos = target,
            IsNew = true,
            IsExplosive = explosive
        });
    }

    [BurstCompile]
    private void TriggerSound(EntityCommandBuffer ecb, int id, float3 position, bool isLoop)
    {
        Entity soundEntity = ecb.CreateEntity();
        ecb.AddComponent(soundEntity, new PlaySoundRequest
        {
            SoundID = id,
            Position = position,
            IsLoop = isLoop
        });
    }
}