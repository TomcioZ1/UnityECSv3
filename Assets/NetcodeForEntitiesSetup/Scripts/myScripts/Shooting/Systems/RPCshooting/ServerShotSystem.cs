using Unity.Burst;
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

        // Szukamy graczy, którzy maj¹ komponent ShotEvent (musi byæ dodany w Authoring/Baking)
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



            if (input.ValueRO.leftMouseButton == 1 && currentTime >= workState.NextShotTime)
            {
                workState.NextShotTime = (float)currentTime + weaponData.fireRate;
                weaponData.currentAmmo -= 1;
                SystemAPI.SetComponent(weaponEntity, workState);
                SystemAPI.SetComponent(weaponEntity, weaponData);


                // 1. Logika Raycastu (tak jak mia³eœ wczeœniej)
                float3 rayStart = transform.ValueRO.Position + math.mul(transform.ValueRO.Rotation, weaponData.ProjectileSpawner);
                float3 rayEnd = rayStart + (input.ValueRO.AimDirection * 10f);

                float3 finalHitPos = rayEnd;
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

                float3 shotDirection = input.ValueRO.AimDirection;

                // 2. Wykonujemy Raycast
                if (physicsWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit hit))
                {
                    if (hit.Entity != entity) // Nie trafiamy samych siebie
                    {
                        finalHitPos = hit.Position;
                        // Tutaj obliczamy kierunek od lufy do punktu trafienia (bardziej precyzyjne dla grafiki)
                        shotDirection = math.normalize(finalHitPos - rayStart);

                        // --- LOGIKA OBRA¯EÑ ---
                        if (healthLookup.HasComponent(hit.Entity))
                        {
                            var health = healthLookup[hit.Entity];
                            health.HealthPoints -= weaponData.damage;
                            health.LastHitBy = entity;
                            healthLookup[hit.Entity] = health;
                        }
                    }
                }

                // 3. AKTUALIZACJA EVENTU (Musi byæ poza IFem trafienia!)
                // Dziêki temu nawet strza³ w niebo zespawnuje pocisk u klientów
                shotEvent.ValueRW.ShotCount++;
                shotEvent.ValueRW.TargetPos = finalHitPos;
                shotEvent.ValueRW.Direction = shotDirection;
                //Debug.DrawLine(rayStart, rayEnd, Color.red, 0.1f);
            }
        }
    }
}