using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileSpawnSystem : ISystem
{
    // Deklarujemy lookupy jako pola
    private ComponentLookup<WeaponData> weaponDataLookup;
    private ComponentLookup<WeaponWorkState> weaponStateLookup;
    private ComponentLookup<NetworkId> networkIdLookup;
    private ComponentLookup<LocalToWorld> ltwLookup;
    private ComponentLookup<LocalTransform> ltLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Inicjalizacja lookupów
        weaponDataLookup = state.GetComponentLookup<WeaponData>(false);
        weaponStateLookup = state.GetComponentLookup<WeaponWorkState>(false);
        networkIdLookup = state.GetComponentLookup<NetworkId>(true);
        ltwLookup = state.GetComponentLookup<LocalToWorld>(true);
        ltLookup = state.GetComponentLookup<LocalTransform>(true);

        state.RequireForUpdate<ProjectilePrefab>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var prefab)) return;

        // Odœwie¿anie lookupów w ka¿dej klatce
        weaponDataLookup.Update(ref state);
        weaponStateLookup.Update(ref state);
        networkIdLookup.Update(ref state);
        ltwLookup.Update(ref state);
        ltLookup.Update(ref state);

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        double currentTime = SystemAPI.Time.ElapsedTime;

        // Query: szukamy graczy, którzy maj¹ wejœcie (input) i aktywn¹ broñ
        foreach (var (input, activeWeapon, playerEntity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<ActiveWeapon>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // Pominiecie ID 0 i 3 (wg Twojej logiki)
            if (activeWeapon.ValueRO.SelectedWeaponId == 0 || activeWeapon.ValueRO.SelectedWeaponId == 3)
                continue;

            Entity wEntity = activeWeapon.ValueRO.WeaponEntity;

            // Walidacja encji broni
            if (wEntity == Entity.Null || !weaponDataLookup.HasComponent(wEntity) || !weaponStateLookup.HasComponent(wEntity))
                continue;

            var weapon = weaponDataLookup[wEntity];
            var wState = weaponStateLookup[wEntity];

            if (weapon.maxAmmo <= 0) continue;

            // Logika strza³u
            if (input.ValueRO.leftMouseButton == 1 && !wState.IsReloading && weapon.currentAmmo > 0 && currentTime >= wState.NextShotTime)
            {
                // Aktualizacja amunicji i czasu nastêpnego strza³u
                weapon.currentAmmo--;
                weapon.maxAmmo--;
                wState.NextShotTime = (float)currentTime + weapon.fireRate;

                weaponDataLookup[wEntity] = weapon;
                weaponStateLookup[wEntity] = wState;

                if (weapon.ProjectileSpawner != Entity.Null && ltwLookup.HasComponent(weapon.ProjectileSpawner))
                {
                    var spawnerLTW = ltwLookup[weapon.ProjectileSpawner];

                    // INSTANCJONOWANIE POCISKU
                    Entity projectile = ecb.Instantiate(prefab.Value);

                    // --- KLUCZOWE DLA OWNER PREDICTION ---
                    // Musimy pobraæ NetworkId gracza (playerEntity) i przypisaæ go do GhostOwner
                    if (networkIdLookup.TryGetComponent(playerEntity, out var netId))
                    {
                        ecb.SetComponent(projectile, new GhostOwner { NetworkId = netId.Value });
                    }
                    else
                    {
                        // Jeœli system nie znajdzie NetworkId, ustawiamy -1 (np. dla NPC lub serwera)
                        ecb.SetComponent(projectile, new GhostOwner { NetworkId = -1 });
                    }
                    // -------------------------------------

                    float3 direction = input.ValueRO.AimDirection;
                    if (math.all(direction == 0)) direction = new float3(0, 0, 1);

                    var transform = LocalTransform.FromPositionRotation(
                        spawnerLTW.Position,
                        quaternion.LookRotationSafe(direction, math.up())
                    );

                    // Ustawienie skali z prefabu
                    transform.Scale = ltLookup.HasComponent(prefab.Value) ? ltLookup[prefab.Value].Scale : 1.0f;

                    ecb.SetComponent(projectile, transform);
                    ecb.SetComponent(projectile, new ProjectileComponent
                    {
                        Damage = weapon.damage,
                        Velocity = direction * weapon.projectileSpeed,
                        Lifetime = 3.0f,
                        Owner = playerEntity
                    });
                }
            }

            // Logika prze³adowania
            if (weapon.currentAmmo <= 0 && !wState.IsReloading)
            {
                wState.IsReloading = true;
                wState.ReloadTimer = (float)currentTime + weapon.reloadTime;
                weaponStateLookup[wEntity] = wState;
            }
        }
    }
}