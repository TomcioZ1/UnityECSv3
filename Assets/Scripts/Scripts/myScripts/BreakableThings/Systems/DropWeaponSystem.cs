/*using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))] // Zmiana na standardową grupę symulacji
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)] // Zdecydowanie TYLKO serwer
public partial struct DropWeaponSystem : ISystem
{
    private Unity.Mathematics.Random _random;
    private bool _randomInitialized;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WeaponUIPrefabsConfig>();
        _randomInitialized = false;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!_randomInitialized)
        {
            _random = new Unity.Mathematics.Random((uint)(System.DateTime.Now.Ticks & 0xFFFFFFFF) + 1u);
            _randomInitialized = true;
        }

        // Używamy BeginSimulation na kolejną klatkę, aby mieć pewność, 
        // że transformacja zostanie zaaplikowana zanim system wysyłania Ghostów (GhostSendSystem) ją przechwyci.
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var weaponConfig = SystemAPI.GetSingleton<WeaponUIPrefabsConfig>();

        foreach (var (health, transform, dropWeapon, entity) in
                 SystemAPI.Query<RefRO<HealthComponent>, RefRO<LocalTransform>, RefRO<DropWeapon>>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.HealthPoints <= 0)
            {
                if (_random.NextInt(1, 101) <= dropWeapon.ValueRO.DropChance)
                {
                    Entity prefabToSpawn = GetRandomWeapon(weaponConfig, ref _random);

                    if (prefabToSpawn != Entity.Null)
                    {
                        // 1. Instantiate na serwerze - NetCode automatycznie zleci klientowi zespawnowanie jego wersji
                        Entity droppedWeapon = ecb.Instantiate(prefabToSpawn);

                        // 2. Ustawiamy pozycję
                        float3 spawnPos = transform.ValueRO.Position;
                        spawnPos.y = 5.9f; // Bezpieczna wysokość

                        ecb.SetComponent(droppedWeapon, LocalTransform.FromPositionRotationScale(
                            spawnPos,
                            quaternion.identity,
                            1.0f));
                    }
                }

                // Usuwamy komponent natychmiast
                ecb.RemoveComponent<DropWeapon>(entity);
            }
        }
    }

    private Entity GetRandomWeapon(WeaponUIPrefabsConfig config, ref Unity.Mathematics.Random random)
    {
        int weaponIndex = random.NextInt(0, 5);
        return weaponIndex switch
        {
            0 => config.MP5Prefab,
            1 => config.ShotgunPrefab,
            2 => config.AK47Prefab,
            3 => config.AWPPrefab,
            4 => config.RocketLauncherPrefab,
            _ => Entity.Null
        };
    }
}*/