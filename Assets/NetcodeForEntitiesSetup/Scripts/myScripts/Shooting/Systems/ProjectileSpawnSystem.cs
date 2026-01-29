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
    private ComponentLookup<WeaponData> weaponDataLookup;
    private ComponentLookup<WeaponWorkState> weaponStateLookup;
    private ComponentLookup<NetworkId> networkIdLookup;
    private ComponentLookup<LocalToWorld> ltwLookup;
    private ComponentLookup<LocalTransform> ltLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        weaponDataLookup = state.GetComponentLookup<WeaponData>(false);
        weaponStateLookup = state.GetComponentLookup<WeaponWorkState>(false);
        networkIdLookup = state.GetComponentLookup<NetworkId>(true);
        ltwLookup = state.GetComponentLookup<LocalToWorld>(true);
        ltLookup = state.GetComponentLookup<LocalTransform>(true);

        state.RequireForUpdate<ProjectilePrefab>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var prefab)) return;

        // Aktualizacja lookupów
        weaponDataLookup.Update(ref state);
        weaponStateLookup.Update(ref state);
        networkIdLookup.Update(ref state);
        ltwLookup.Update(ref state);
        ltLookup.Update(ref state);

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        double currentTime = SystemAPI.Time.ElapsedTime;

        foreach (var (input, inventory, playerEntity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<PlayerInventory>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // Podstawowe filtry
            if (inventory.ValueRO.ActiveSlotIndex == 3 || inventory.ValueRO.CurrentlySpawnedWeaponId == 0)
                continue;

            Entity wEntity = inventory.ValueRO.CurrentWeaponEntity;

            if (wEntity == Entity.Null || !weaponDataLookup.HasComponent(wEntity) || !weaponStateLookup.HasComponent(wEntity))
                continue;

            var weapon = weaponDataLookup[wEntity];
            var wState = weaponStateLookup[wEntity];

            // 1. LOGIKA STRZA£U
            if (input.ValueRO.leftMouseButton == 1 && !wState.IsReloading && weapon.currentAmmo > 0 && currentTime >= wState.NextShotTime)
            {
                // Aktualizacja stanu broni
                weapon.currentAmmo--;
                wState.NextShotTime = (float)currentTime + weapon.fireRate;

                weaponDataLookup[wEntity] = weapon;
                weaponStateLookup[wEntity] = wState;

                if (weapon.ProjectileSpawner != Entity.Null && ltwLookup.HasComponent(weapon.ProjectileSpawner))
                {
                    var spawnerLTW = ltwLookup[weapon.ProjectileSpawner];
                    Entity projectile = ecb.Instantiate(prefab.Value);

                    // Ustawienie w³aœciciela (GhostOwner) dla Netcode
                    int ownerNetId = -1;
                    if (networkIdLookup.TryGetComponent(playerEntity, out var netId))
                        ownerNetId = netId.Value;

                    ecb.SetComponent(projectile, new GhostOwner { NetworkId = ownerNetId });

                    // Kierunek i rotacja
                    float3 direction = input.ValueRO.AimDirection;
                    if (math.all(direction == 0)) direction = new float3(0, 0, 1);

                    var rotation = quaternion.LookRotationSafe(direction, math.up());

                    // Transformacja startowa
                    float prefabScale = ltLookup.HasComponent(prefab.Value) ? ltLookup[prefab.Value].Scale : 1.0f;
                    ecb.SetComponent(projectile, LocalTransform.FromPositionRotationScale(spawnerLTW.Position, rotation, prefabScale));

                    // NOWA LOGIKA: ProjectileComponent z DeathTime
                    ecb.SetComponent(projectile, new ProjectileComponent
                    {
                        Damage = weapon.damage,
                        Velocity = direction * weapon.projectileSpeed,
                        DeathTime = currentTime + 3.0, // Pocisk zginie dok³adnie za 3 sekundy od teraz
                        Owner = playerEntity
                    });
                }
            }

            // 2. LOGIKA PRZE£ADOWANIA (Uproszczona: tylko jeœli ammo puste i mamy z czego dobraæ)
            if (weapon.currentAmmo <= 0 && !wState.IsReloading && weapon.maxAmmo > 0)
            {
                wState.IsReloading = true;
                wState.ReloadTimer = (float)currentTime + weapon.reloadTime;
                weaponStateLookup[wEntity] = wState;
            }
        }
    }
}