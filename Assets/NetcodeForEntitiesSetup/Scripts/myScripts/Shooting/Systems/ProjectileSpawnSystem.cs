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
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Gwarantujemy, ¿e system nie ruszy bez potrzebnych danych
        state.RequireForUpdate<ProjectilePrefab>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Bezpieczne pobranie Singletonów
        if (!SystemAPI.TryGetSingleton<ProjectilePrefab>(out var prefab)) return;
        if (prefab.Value == Entity.Null) return;

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 2. POBIERAMY LOOKUPY BEZPOŒREDNIO TUTAJ (To naprawia NullRef w linii 15)
        var weaponDataLookup = SystemAPI.GetComponentLookup<WeaponData>(true);
        var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        // 3. Pobieramy transformacjê prefaba (z zachowaniem skali)
        if (!localTransformLookup.HasComponent(prefab.Value)) return;
        var prefabTransform = localTransformLookup[prefab.Value];

        // 4. G³ówna pêtla graczy
        foreach (var (input, activeWeapon, entity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<ActiveWeapon>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // Filtry wejœcia
            if (input.ValueRO.leftMouseButton == 0) continue;
            if (input.ValueRO.choosenWeapon == 3) continue;

            // Sprawdzenie broni
            Entity weaponEnt = activeWeapon.ValueRO.WeaponEntity;
            if (weaponEnt == Entity.Null || !weaponDataLookup.HasComponent(weaponEnt)) continue;

            // Sprawdzenie spawnera (punktu wylotu lufy)
            var weaponData = weaponDataLookup[weaponEnt];
            Entity spawnerEnt = weaponData.ProjectileSpawner;

            if (spawnerEnt == Entity.Null || !localToWorldLookup.HasComponent(spawnerEnt)) continue;

            // Pobieramy pozycjê ŒWIATOW¥ spawnera (LocalToWorld)
            var spawnerLTW = localToWorldLookup[spawnerEnt];

            // --- SPAWN ---
            Entity projectile = ecb.Instantiate(prefab.Value);

            float3 direction = math.normalizesafe(input.ValueRO.AimDirection);
            if (math.all(direction == float3.zero)) direction = new float3(0, 0, 1);

            // Ustawienie transformu na podstawie lufy
            var projectileTransform = prefabTransform;
            projectileTransform.Position = spawnerLTW.Position; // Pozycja z lufy
            projectileTransform.Rotation = quaternion.LookRotationSafe(direction, math.up());

            ecb.SetComponent(projectile, projectileTransform);

            ecb.SetComponent(projectile, new ProjectileComponent
            {
                Damage = 10,
                Velocity = direction * 2f,
                Lifetime = 3.0f,
                Owner = entity
            });
        }
    }
}