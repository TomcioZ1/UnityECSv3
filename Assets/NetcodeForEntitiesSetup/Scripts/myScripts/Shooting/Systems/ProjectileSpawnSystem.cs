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
    // Deklarujemy lookupy jako pola, aby ISystem móg³ je œledziæ
    private ComponentLookup<WeaponData> weaponDataLookup;
    private ComponentLookup<WeaponWorkState> weaponStateLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        weaponDataLookup = state.GetComponentLookup<WeaponData>(false);
        weaponStateLookup = state.GetComponentLookup<WeaponWorkState>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var prefab)) return;

        // ODŒWIE¯ANIE LOOKUPÓW - bez tego gra siê crashuje!
        weaponDataLookup.Update(ref state);
        weaponStateLookup.Update(ref state);
        var ltwLookup = state.GetComponentLookup<LocalToWorld>(true);
        var ltLookup = state.GetComponentLookup<LocalTransform>(true);

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        double currentTime = SystemAPI.Time.ElapsedTime;

        foreach (var (input, activeWeapon, playerEntity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<ActiveWeapon>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            if (activeWeapon.ValueRO.SelectedWeaponId == 0 || activeWeapon.ValueRO.SelectedWeaponId == 3)
                continue;

            Entity wEntity = activeWeapon.ValueRO.WeaponEntity;

            // Bardzo wa¿ne sprawdzenie istnienia encji przed dostêpem do lookupa
            if (wEntity == Entity.Null || !weaponDataLookup.HasComponent(wEntity) || !weaponStateLookup.HasComponent(wEntity))
                continue;

            var weapon = weaponDataLookup[wEntity];
            var wState = weaponStateLookup[wEntity];

            if (input.ValueRO.leftMouseButton == 1 && !wState.IsReloading && weapon.currentAmmo > 0 && currentTime >= wState.NextShotTime)
            {
                weapon.currentAmmo--;
                wState.NextShotTime = (float)currentTime + weapon.fireRate;

                weaponDataLookup[wEntity] = weapon;
                weaponStateLookup[wEntity] = wState;

                if (weapon.ProjectileSpawner != Entity.Null && ltwLookup.HasComponent(weapon.ProjectileSpawner))
                {
                    var spawnerLTW = ltwLookup[weapon.ProjectileSpawner];
                    Entity projectile = ecb.Instantiate(prefab.Value);

                    float3 direction = input.ValueRO.AimDirection;
                    if (math.all(direction == 0)) direction = new float3(0, 0, 1);

                    var transform = LocalTransform.FromPositionRotation(spawnerLTW.Position, quaternion.LookRotationSafe(direction, math.up()));
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

            if (weapon.currentAmmo <= 0 && !wState.IsReloading)
            {
                wState.IsReloading = true;
                wState.ReloadTimer = (float)currentTime + weapon.reloadTime;
                weaponStateLookup[wEntity] = wState;
            }
        }
    }
}