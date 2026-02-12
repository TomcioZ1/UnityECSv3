using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var projectilePrefab)) return;
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();

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
                workState.NextShotTime = (float)currentTime + weaponData.fireRate;
                weaponData.currentAmmo -= 1;

                SystemAPI.SetComponent(weaponEntity, workState);
                SystemAPI.SetComponent(weaponEntity, weaponData);

               

                Entity projectile = ecb.Instantiate(projectilePrefab.Value);

                LocalTransform spawnerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);

                // 1. Pobieramy rotacjê (np. gracza)
                quaternion spawnRotation = spawnerTransform.Rotation;

                // 2. Pobieramy offset lokalny (np. zdefiniowany w ScriptableObject jako 0, 1, 1)
                float3 localOffset = weaponData.ProjectileSpawner;

                // 3. Mno¿ymy rotacjê przez offset (USUÑ DODAWANIE weaponData tutaj)
                // math.mul obraca Twój lokalny wektor tak, by pasowa³ do kierunku, w którym patrzy gracz
                float3 worldOffset = math.mul(spawnRotation, localOffset);

                // 4. Dodajemy obrócony offset do pozycji startowej
                float3 spawnPos = spawnerTransform.Position + worldOffset;


                ecb.SetComponent(projectile, LocalTransform.FromPositionRotationScale(
                    spawnPos,
                    quaternion.identity,
                    0.1f
                ));



                float3 playerVelocity = float3.zero;
                if (SystemAPI.HasComponent<PhysicsVelocity>(playerEntity))
                {
                    playerVelocity = SystemAPI.GetComponent<PhysicsVelocity>(playerEntity).Linear;
                }

                // Obliczamy prêdkoœæ bazow¹ pocisku
                float3 projectileBaseVelocity = input.ValueRO.AimDirection * weaponData.projectileSpeed;

                // Finalna prêdkoœæ to suma obu wektorów
                float3 finalVelocity = projectileBaseVelocity + playerVelocity;


                ecb.SetComponent(projectile, new ProjectileComponent
                {
                    Velocity = finalVelocity,
                    DeathTime = currentTime + 3f,
                    Owner = playerEntity,
                    Damage = weaponData.damage
                });
            }
        }
    }
}