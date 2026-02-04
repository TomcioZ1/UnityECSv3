using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Physics;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileSpawnSystem : ISystem
{
    private ComponentLookup<WeaponData> weaponDataLookup;
    private ComponentLookup<WeaponWorkState> weaponStateLookup;
    private ComponentLookup<NetworkId> networkIdLookup;
    private ComponentLookup<LocalToWorld> ltwLookup;
    private ComponentLookup<LocalTransform> ltLookup;
    private ComponentLookup<PhysicsVelocity> velocityLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        weaponDataLookup = state.GetComponentLookup<WeaponData>(false);
        weaponStateLookup = state.GetComponentLookup<WeaponWorkState>(false);
        networkIdLookup = state.GetComponentLookup<NetworkId>(true);
        ltwLookup = state.GetComponentLookup<LocalToWorld>(true);
        ltLookup = state.GetComponentLookup<LocalTransform>(true);
        velocityLookup = state.GetComponentLookup<PhysicsVelocity>(true);

        state.RequireForUpdate<ProjectilePrefab>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var prefab)) return;

        state.Dependency.Complete();

        weaponDataLookup.Update(ref state);
        weaponStateLookup.Update(ref state);
        networkIdLookup.Update(ref state);
        ltwLookup.Update(ref state);
        ltLookup.Update(ref state);
        velocityLookup.Update(ref state);

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        double currentTime = SystemAPI.Time.ElapsedTime;

        foreach (var (input, inventory, playerEntity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<PlayerInventory>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            if (inventory.ValueRO.ActiveSlotIndex == 3 || inventory.ValueRO.CurrentlySpawnedWeaponId == 0)
                continue;

            Entity wEntity = inventory.ValueRO.CurrentWeaponEntity;

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

                    // --- TWOJA PROŒBA: RÊCZNA KOREKTA POZYCJI NA PODSTAWIE INPUTU ---
                    // Pobieramy aktualn¹ pozycjê lufy
                    float3 spawnPos = spawnerLTW.Position;

                    // Dodajemy przesuniêcie w zale¿noœci od kierunku ruchu (Horizontal/Vertical)
                    // Wartoœci 1.0f (zgodnie z Twoj¹ proœb¹) koryguj¹ lag klatki
                    float3 inputOffset = new float3(input.ValueRO.Horizontal, 0, input.ValueRO.Vertical);

                    // Normalizujemy, aby ruch po skosie nie dodawa³ zbyt du¿ej poprawki
                    if (math.lengthsq(inputOffset) > 0.001f)
                    {
                        // Poprawka: mno¿ymy przez ok 0.1-0.2 (poniewa¿ 1.0 to bardzo du¿o w ECS na klatkê)
                        // Jeœli pocisk jest wci¹¿ za Tob¹, zwiêksz tê wartoœæ.
                        spawnPos += math.normalize(inputOffset) * 0.30f;
                    }

                    Entity projectile = ecb.Instantiate(prefab.Value);

                    int ownerNetId = -1;
                    if (networkIdLookup.TryGetComponent(playerEntity, out var netId))
                        ownerNetId = netId.Value;

                    ecb.SetComponent(projectile, new GhostOwner { NetworkId = ownerNetId });

                    float3 direction = input.ValueRO.AimDirection;
                    if (math.all(direction == 0)) direction = new float3(0, 0, 1);
                    var rotation = quaternion.LookRotationSafe(direction, math.up());

                    // DZIEDZICZENIE PÊDU (Velocity gracza)
                    float3 playerVel = float3.zero;
                    if (velocityLookup.TryGetComponent(playerEntity, out var pVel))
                        playerVel = pVel.Linear;

                    float prefabScale = ltLookup.HasComponent(prefab.Value) ? ltLookup[prefab.Value].Scale : 1.0f;

                    ecb.SetComponent(projectile, LocalTransform.FromPositionRotationScale(spawnPos, rotation, prefabScale));

                    ecb.SetComponent(projectile, new ProjectileComponent
                    {
                        Damage = weapon.damage,
                        Velocity = (direction * weapon.projectileSpeed) + playerVel,
                        DeathTime = currentTime + 3.0,
                        Owner = playerEntity
                    });
                }
            }

            // Logika reloadu
            if (weapon.currentAmmo <= 0 && !wState.IsReloading && weapon.maxAmmo > 0)
            {
                wState.IsReloading = true;
                wState.ReloadTimer = (float)currentTime + weapon.reloadTime;
                weaponStateLookup[wEntity] = wState;
            }
        }
    }
}