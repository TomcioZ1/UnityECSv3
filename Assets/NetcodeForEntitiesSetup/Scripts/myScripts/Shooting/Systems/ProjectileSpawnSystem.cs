using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using Unity.Transforms;
// Zeby zrobic predicted usunac server simulation filter, dodac predicted tick, zmienic w predicted na ghoscie
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var projectilePrefab)) return;
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        SystemAPI.TryGetSingleton<PerformanceStats>(out var pingStats);


        if (!networkTime.IsFirstTimeFullyPredictingTick)
            return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        double currentTime = SystemAPI.Time.ElapsedTime;

        foreach (var (input, inventory, playerTransform, playerEntity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<PlayerInventory>, RefRO<LocalTransform>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            if (!SystemAPI.TryGetSingleton<BulletSpawnTimer>(out var bulletSpawnTimer)) return; // AAAAAAAAAAAA



            Entity weaponEntity = inventory.ValueRO.CurrentWeaponEntity;

            if (weaponEntity == Entity.Null ||
                !SystemAPI.HasComponent<WeaponData>(weaponEntity) ||
                !SystemAPI.HasComponent<WeaponWorkState>(weaponEntity))
                continue;

            var weaponData = SystemAPI.GetComponent<WeaponData>(weaponEntity);
            var workState = SystemAPI.GetComponent<WeaponWorkState>(weaponEntity);

            // --- LOGIKA RELOADU ---
            // 1. SprawdŸ czy gracz chce prze³adowaæ (np. klawisz R) lub czy magazynek jest pusty
            byte reloadRequested = input.ValueRO.reloadRequested; // Zak³adam, ¿e masz to w MyPlayerInput

            if (!workState.IsReloading && (reloadRequested == 1 || weaponData.currentAmmo <= 0) && weaponData.currentAmmo < weaponData.magSize)
            {
                workState.IsReloading = true;
                workState.ReloadTimer = (float)currentTime + weaponData.reloadTime;
                SystemAPI.SetComponent(weaponEntity, workState);
            }

            // 2. SprawdŸ czy czas prze³adowania dobieg³ koñca
            if (workState.IsReloading)
            {
                if (currentTime >= workState.ReloadTimer)
                {
                    weaponData.currentAmmo = weaponData.magSize; // Uzupe³nij amunicjê
                    workState.IsReloading = false;

                    SystemAPI.SetComponent(weaponEntity, weaponData);
                    SystemAPI.SetComponent(weaponEntity, workState);
                }
                else
                {
                    // Jeœli wci¹¿ siê prze³adowuje, nie pozwól strzelaæ
                    continue;
                }
            }

            // --- LOGIKA STRZA£U ---
            if (input.ValueRO.leftMouseButton == 1 &&
                currentTime >= workState.NextShotTime &&
                weaponData.currentAmmo > 0)
            {

                /*if(input.ValueRO.SpawnBulletTime >= currentTime)
                {
                    continue;
                }*/


                workState.NextShotTime = (float)currentTime + weaponData.fireRate;
                weaponData.currentAmmo -= 1;

                SystemAPI.SetComponent(weaponEntity, workState);
                SystemAPI.SetComponent(weaponEntity, weaponData);



                // 1. Podstawowa rotacja i offset lufy
                quaternion spawnRotation = playerTransform.ValueRO.Rotation;
                float3 worldOffset = math.mul(spawnRotation, weaponData.ProjectileSpawner);

                // 2. Pobranie aktualnej prêdkoœci gracza
                float3 playerVelocity = float3.zero;
                if (SystemAPI.HasComponent<PhysicsVelocity>(playerEntity))
                {
                    playerVelocity = SystemAPI.GetComponent<PhysicsVelocity>(playerEntity).Linear;
                }

                // 3. KOMPENSACJA RUCHU:
                // Przesuwamy punkt spawnu o dystans, który gracz pokona³ w trakcie podró¿y pakietu (RTT)
                // Pomaga to "dogoniæ" pozycjê gracza, któr¹ widzia³ u siebie na ekranie
                float3 movementCompensation = math.normalize(playerVelocity) * (0.2f);
                //Debug.Log($"Ping: {pingStats.Ping} ms, Player Velocity: {playerVelocity}, Movement Compensation: {movementCompensation}");
                float3 spawnPos = playerTransform.ValueRO.Position + worldOffset + movementCompensation;


                // 4. Instancjonowanie
                Entity projectile = ecb.Instantiate(projectilePrefab.Value);

                // 5. Ustawienie transformacji
                ecb.SetComponent(projectile, LocalTransform.FromPositionRotationScale(
                    spawnPos,
                    spawnRotation,
                    1f
                ));

                // 6. Obliczenie prêdkoœci pocisku (prêdkoœæ wylotowa + wp³yw ruchu gracza)
                float3 projectileBaseVelocity = input.ValueRO.AimDirection * weaponData.projectileSpeed;
                float3 finalVelocity = projectileBaseVelocity + playerVelocity;

                // 7. Inicjalizacja danych pocisku
                ecb.SetComponent(projectile, new ProjectileComponent
                {
                    Velocity = finalVelocity,
                    SpawnTime = currentTime,
                    DeathTime = currentTime + 3f,
                    Owner = playerEntity,
                    Damage = weaponData.damage,
                });

            }
        }
    }
}