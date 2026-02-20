using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct ServerShotSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        double currentTime = SystemAPI.Time.ElapsedTime;
        var healthLookup = SystemAPI.GetComponentLookup<HealthComponent>(false);

        foreach (var (input, inventory, shotEvent, transform, entity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<PlayerInventory>, RefRW<ShotEvent>, RefRO<LocalTransform>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            Entity weaponEntity = inventory.ValueRO.CurrentWeaponEntity;
            if (weaponEntity == Entity.Null || !SystemAPI.HasComponent<WeaponData>(weaponEntity)) continue;

            var weaponData = SystemAPI.GetComponent<WeaponData>(weaponEntity);
            var workState = SystemAPI.GetComponent<WeaponWorkState>(weaponEntity);

            // --- LOGIKA RELOADU ---
            if (!workState.IsReloading && (input.ValueRO.reloadRequested == 1 || weaponData.currentAmmo <= 0) && weaponData.currentAmmo < weaponData.magSize)
            {
                workState.IsReloading = true;
                workState.ReloadTimer = (float)currentTime + weaponData.reloadTime;
                SystemAPI.SetComponent(weaponEntity, workState);
            }

            if (workState.IsReloading)
            {
                if (currentTime >= workState.ReloadTimer)
                {
                    weaponData.currentAmmo = weaponData.magSize;
                    workState.IsReloading = false;
                    SystemAPI.SetComponent(weaponEntity, weaponData);
                    SystemAPI.SetComponent(weaponEntity, workState);
                }
                else continue;
            }

            // --- STRZA£ ---
            if (input.ValueRO.leftMouseButton == 1 && currentTime >= workState.NextShotTime && weaponData.currentAmmo > 0)
            {
                workState.NextShotTime = (float)currentTime + weaponData.fireRate;
                weaponData.currentAmmo -= 1;
                SystemAPI.SetComponent(weaponEntity, workState);
                SystemAPI.SetComponent(weaponEntity, weaponData);

                float3 rayStart = transform.ValueRO.Position + math.mul(transform.ValueRO.Rotation, weaponData.ProjectileSpawner);
                float3 baseDirection = input.ValueRO.AimDirection;

                CollisionFilter filter = new CollisionFilter
                {
                    BelongsTo = 1u << 4,
                    CollidesWith = (1u << 0) | (1u << 3),
                    GroupIndex = 0
                };

                if (weaponData.isNormalGun)
                {
                    ExecuteRaycast(rayStart, baseDirection, weaponData.maxRange, entity, weaponData.damage, physicsWorld, ref healthLookup, out float3 hitPos);
                    UpdateShotEvent(shotEvent, hitPos, baseDirection);
                    //DrawDebugLine(rayStart, hitPos, Color.red, 0.5f);
                }
                else if (weaponData.isShotgun)
                {
                    float3 up = math.select(new float3(0, 1, 0), new float3(1, 0, 0), math.abs(baseDirection.y) > 0.9f);
                    float3 right = math.normalize(math.cross(baseDirection, up));
                    float3 actualUp = math.cross(right, baseDirection);

                    float spreadIntensity = 0.05f;

                    // POPRAWKA BURST: stackalloc zamiast new float3[]
                    System.Span<float3> offsets = stackalloc float3[5];
                    offsets[0] = float3.zero;
                    offsets[1] = right * spreadIntensity;
                    offsets[2] = -right * spreadIntensity;
                    offsets[3] = actualUp * spreadIntensity;
                    offsets[4] = -actualUp * spreadIntensity;

                    float3 centerHit = rayStart + (baseDirection * weaponData.maxRange);

                    for (int i = 0; i < 5; i++)
                    {
                        float3 spreadDir = math.normalize(baseDirection + offsets[i]);
                        ExecuteRaycast(rayStart, spreadDir, weaponData.maxRange, entity, weaponData.damage, physicsWorld, ref healthLookup, out float3 individualHit);

                        if (i == 0) centerHit = individualHit; // Œrodek dla ShotEvent
                        //DrawDebugLine(rayStart, individualHit, Color.yellow, 0.5f);
                    }
                    UpdateShotEvent(shotEvent, centerHit, baseDirection);
                }
                else if (weaponData.isGranadeLauncher)
                {
                    float3 endPos = rayStart + (baseDirection * 20f);
                    float3 explosionPos = endPos;

                    RaycastInput rayInput = new RaycastInput { Start = rayStart, End = endPos, Filter = filter };
                    if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
                    {
                        explosionPos = hit.Position;
                    }

                    // Logika Wybuchu
                    float explosionRadius = 2f;
                    NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(Allocator.Temp);

                    if (physicsWorld.OverlapSphere(explosionPos, explosionRadius, ref distanceHits, filter))
                    {
                        for (int i = 0; i < distanceHits.Length; i++)
                        {
                            Entity hitEnt = distanceHits[i].Entity;
                            if (healthLookup.HasComponent(hitEnt))
                            {
                                var health = healthLookup[hitEnt];
                                health.HealthPoints -= weaponData.damage;
                                health.LastHitBy = entity;
                                healthLookup[hitEnt] = health;
                            }
                        }
                    }

                    //DrawDebugLine(rayStart, explosionPos, Color.green, 0.2f);
                    //DrawDebugExplosion(explosionPos, explosionRadius, Color.white);
                    UpdateShotEvent(shotEvent, explosionPos, baseDirection);
                }
            }
        }
    }

    private void ExecuteRaycast(float3 start, float3 dir, float dist, Entity owner, int damage, in PhysicsWorld world, ref ComponentLookup<HealthComponent> healthLookup, out float3 finalHitPos)
    {
        // Offset startowy (0.2m), aby nie trafiæ we w³asny collider
        float3 offsetStart = start + (dir * 0.2f);
        float3 end = offsetStart + (dir * dist);
        finalHitPos = end;

        RaycastInput input = new RaycastInput
        {
            Start = offsetStart,
            End = end,
            Filter = new CollisionFilter { BelongsTo = 1u << 4, CollidesWith = (1u << 0) | (1u << 3) }
        };

        if (world.CastRay(input, out Unity.Physics.RaycastHit hit))
        {
            if (hit.Entity != owner)
            {
                finalHitPos = hit.Position;
                if (healthLookup.HasComponent(hit.Entity))
                {
                    var health = healthLookup[hit.Entity];
                    health.HealthPoints -= damage;
                    health.LastHitBy = owner;
                    healthLookup[hit.Entity] = health;
                }
            }
        }
    }

    private void UpdateShotEvent(RefRW<ShotEvent> shotEvent, float3 hitPos, float3 dir)
    {
        shotEvent.ValueRW.ShotCount++;
        shotEvent.ValueRW.TargetPos = hitPos;
        shotEvent.ValueRW.Direction = dir;
    }

    [BurstDiscard]
    private void DrawDebugLine(float3 start, float3 end, Color color, float time)
    {
        Debug.DrawLine(start, end, color, time);

    }

    [BurstDiscard]
    private void DrawDebugExplosion(float3 pos, float radius, Color color)
    {
        Debug.DrawLine(pos + new float3(radius, 0, 0), pos + new float3(-radius, 0, 0), color, 0.5f);
        Debug.DrawLine(pos + new float3(0, radius, 0), pos + new float3(0, -radius, 0), color, 0.5f);
        Debug.DrawLine(pos + new float3(0, 0, radius), pos + new float3(0, 0, -radius), color, 0.5f);
    }
}