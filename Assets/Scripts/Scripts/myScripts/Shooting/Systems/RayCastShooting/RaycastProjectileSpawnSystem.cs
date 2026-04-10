/*using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
public partial struct RaycastProjectileSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();

        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var projectilePrefab)) return;
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        double currentTime = SystemAPI.Time.ElapsedTime;
        var healthLookup = SystemAPI.GetComponentLookup<HealthComponent>(false);

        foreach (var (input, inventory, playerTransform, playerEntity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<PlayerInventory>, RefRO<LocalTransform>>()
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

                // Start i koniec raycastu
                float3 rayStart = playerTransform.ValueRO.Position + math.mul(playerTransform.ValueRO.Rotation, weaponData.ProjectileSpawner);
                float3 rayEnd = rayStart + (input.ValueRO.AimDirection * 50f); // Dystans 50m

                float3 finalTargetPos = rayEnd; // Domyœlny cel jeœli nic nie trafimy

                RaycastInput raycastInput = new RaycastInput
                {
                    Start = rayStart,
                    End = rayEnd,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = 1u << 4,
                        CollidesWith = (1u << 0) | (1u << 3),
                        GroupIndex = 0
                    }
                };

                // Wykonujemy Raycast (Hitscan)
                if (physicsWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit hit))
                {
                    if (hit.Entity != playerEntity)
                    {
                        finalTargetPos = hit.Position; // Pocisk wizualny ma lecieæ do punktu trafienia

                        if (healthLookup.HasComponent(hit.Entity))
                        {
                            var health = healthLookup[hit.Entity];
                            health.HealthPoints -= weaponData.damage;
                            health.LastHitBy = playerEntity;
                            healthLookup[hit.Entity] = health;
                        }
                    }
                }

                // Debugowanie w edytorze
                Debug.DrawLine(rayStart, finalTargetPos, Color.red, 0.1f);

                // SPAWNOWANIE POCISKU WIZUALNEGO
                Entity projectile = ecb.Instantiate(projectilePrefab.Value);

                // Ustawiamy transformacjê startow¹ i rotacjê w stronê lotu
                ecb.SetComponent(projectile, LocalTransform.FromPositionRotation(
                    rayStart,
                    quaternion.LookRotationSafe(input.ValueRO.AimDirection, math.up())
                ));

                // Inicjalizacja komponentu ruchu
                ecb.AddComponent(projectile, new ProjectileComponent
                {
                    Velocity = input.ValueRO.AimDirection * 100f, // Prêdkoœæ wizualna 100m/s
                    TargetPosition = finalTargetPos,
                    SpawnTime = currentTime,
                    DeathTime = (float)currentTime + 2f, // Max 2 sekundy ¿ycia
                });
            }
        }
    }
}*/