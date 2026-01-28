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
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var prefab)) return;

        weaponDataLookup.Update(ref state);
        weaponStateLookup.Update(ref state);
        networkIdLookup.Update(ref state);
        ltwLookup.Update(ref state);
        ltLookup.Update(ref state);

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        double currentTime = SystemAPI.Time.ElapsedTime;

        // ZMIANA: Szukamy PlayerInventory zamiast ActiveWeapon
        foreach (var (input, inventory, playerEntity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<PlayerInventory>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // Pominiecie r¹k (Slot 3) lub gdy gracz nie ma nic w d³oni
            if (inventory.ValueRO.ActiveSlotIndex == 3 || inventory.ValueRO.CurrentlySpawnedWeaponId == 0)
                continue;

            // Pobieramy encjê aktualnie trzymanej broni z inwentarza
            Entity wEntity = inventory.ValueRO.CurrentWeaponEntity;

            if (wEntity == Entity.Null || !weaponDataLookup.HasComponent(wEntity) || !weaponStateLookup.HasComponent(wEntity))
                continue;

            var weapon = weaponDataLookup[wEntity];
            var wState = weaponStateLookup[wEntity];

            if (weapon.maxAmmo <= 0) continue;

            // Logika strza³u
            if (input.ValueRO.leftMouseButton == 1 && !wState.IsReloading && weapon.currentAmmo > 0 && currentTime >= wState.NextShotTime)
            {
                weapon.currentAmmo--;
                weapon.maxAmmo--;
                wState.NextShotTime = (float)currentTime + weapon.fireRate;

                weaponDataLookup[wEntity] = weapon;
                weaponStateLookup[wEntity] = wState;

                if (weapon.ProjectileSpawner != Entity.Null && ltwLookup.HasComponent(weapon.ProjectileSpawner))
                {
                    var spawnerLTW = ltwLookup[weapon.ProjectileSpawner];

                    Entity projectile = ecb.Instantiate(prefab.Value);

                    if (networkIdLookup.TryGetComponent(playerEntity, out var netId))
                    {
                        ecb.SetComponent(projectile, new GhostOwner { NetworkId = netId.Value });
                    }
                    else
                    {
                        ecb.SetComponent(projectile, new GhostOwner { NetworkId = -1 });
                    }

                    float3 direction = input.ValueRO.AimDirection;
                    if (math.all(direction == 0)) direction = new float3(0, 0, 1);

                    var transform = LocalTransform.FromPositionRotation(
                        spawnerLTW.Position,
                        quaternion.LookRotationSafe(direction, math.up())
                    );

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