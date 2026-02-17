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
                    ExecuteRaycast(rayStart, baseDirection, 10f, entity, weaponData.damage, physicsWorld, ref healthLookup, out float3 hitPos);
                    UpdateShotEvent(shotEvent, hitPos, baseDirection);

                    // DEBUG: Czerwona linia dla zwyk³ego strza³u
                    //DrawDebugLine(rayStart, hitPos, Color.red, 0.2f);
                }
                else if (weaponData.isShotgun)
                {
                    float3 up = math.select(new float3(0, 1, 0), new float3(1, 0, 0), math.abs(baseDirection.y) > 0.9f);
                    float3 right = math.normalize(math.cross(baseDirection, up));
                    float3 actualUp = math.cross(right, baseDirection);

                    float spreadIntensity = 0.05f;
                    float3[] offsets = new float3[5] {
                        float3.zero,
                        right * spreadIntensity,
                        -right * spreadIntensity,
                        actualUp * spreadIntensity,
                        -actualUp * spreadIntensity
                    };

                    for (int i = 0; i < 5; i++)
                    {
                        float3 spreadDir = math.normalize(baseDirection + offsets[i]);
                        ExecuteRaycast(rayStart, spreadDir, 5f, entity, weaponData.damage, physicsWorld, ref healthLookup, out float3 individualHit);

                        // DEBUG: ¯ó³te linie dla œrutu strzelby
                        //DrawDebugLine(rayStart, individualHit, Color.yellow, 5f);
                    }
                    UpdateShotEvent(shotEvent, rayStart + baseDirection * 5f, baseDirection);
                }
                else if (weaponData.isGranadeLauncher)
                {
                    float3 endPos = rayStart + (baseDirection * 15f);
                    float3 explosionPos = endPos;

                    RaycastInput rayInput = new RaycastInput { Start = rayStart, End = endPos, Filter = filter };
                    if (physicsWorld.CastRay(rayInput, out Unity.Physics.RaycastHit hit))
                    {
                        explosionPos = hit.Position;
                    }

                    // Logika Wybuchu
                    float explosionRadius = 1.0f;
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

                    // DEBUG: Zielona linia lotu i bia³y "krzy¿" w miejscu wybuchu
                    //DrawDebugLine(rayStart, explosionPos, Color.green, 0.2f);
                    //DrawDebugExplosion(explosionPos, explosionRadius, Color.white);

                    UpdateShotEvent(shotEvent, explosionPos, baseDirection);
                }
            }
        }
    }

    private void ExecuteRaycast(float3 start, float3 dir, float dist, Entity owner, int damage, in PhysicsWorld world, ref ComponentLookup<HealthComponent> healthLookup, out float3 finalHitPos)
    {
        float3 end = start + (dir * dist);
        finalHitPos = end;

        RaycastInput input = new RaycastInput
        {
            Start = start,
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

    // --- METODY DEBUGOWANIA ---

    [BurstDiscard]
    private void DrawDebugLine(float3 start, float3 end, Color color, float time)
    {
        Debug.DrawLine(start, end, color, time);
    }

    [BurstDiscard]
    private void DrawDebugExplosion(float3 pos, float radius, Color color)
    {
        // Rysuje prosty krzy¿ w miejscu wybuchu o zadanym promieniu
        Debug.DrawLine(pos + new float3(radius, 0, 0), pos + new float3(-radius, 0, 0), color, 0.5f);
        Debug.DrawLine(pos + new float3(0, radius, 0), pos + new float3(0, -radius, 0), color, 0.5f);
        Debug.DrawLine(pos + new float3(0, 0, radius), pos + new float3(0, 0, -radius), color, 0.5f);
    }
}