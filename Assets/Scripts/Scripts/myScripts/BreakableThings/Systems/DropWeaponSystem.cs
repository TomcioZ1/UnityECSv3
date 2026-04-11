using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine; // Potrzebne do Debug.Log

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DropWeaponSystem : ISystem
{
    private Unity.Mathematics.Random _random;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WeaponUIPrefabsConfig>();
        _random = new Unity.Mathematics.Random(1234);
    }

    // Usunąłem [BurstCompile], żeby Debug.Log mógł wysyłać wiadomości do konsoli Unity
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<WeaponUIPrefabsConfig>())
        {
            Debug.LogWarning("DropWeaponSystem: Brak Singletona WeaponUIPrefabsConfig na scenie!");
            return;
        }

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var weaponConfig = SystemAPI.GetSingleton<WeaponUIPrefabsConfig>();

        foreach (var (dropWeapon, health, transform, entity) in
                 SystemAPI.Query<RefRO<DropWeapon>, RefRO<HealthComponent>, RefRO<LocalTransform>>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.HealthPoints <= 0)
            {
                Debug.Log($"[DropSystem] Przeciwnik {entity.Index} zginął. Sprawdzam drop...");

                // Losowanie szansy
                int roll = _random.NextInt(1, 101);

                Debug.Log($"[DropSystem] Rzut: {roll}, Szansa na drop: {dropWeapon.ValueRO.DropChance}");

                if (roll <= dropWeapon.ValueRO.DropChance)
                {
                    int weaponIndex = _random.NextInt(0, 5);
                    Entity prefabToSpawn = Entity.Null;
                    string weaponName = "";

                    switch (weaponIndex)
                    {
                        case 0: prefabToSpawn = weaponConfig.MP5Prefab; weaponName = "MP5"; break;
                        case 1: prefabToSpawn = weaponConfig.ShotgunPrefab; weaponName = "Shotgun"; break;
                        case 2: prefabToSpawn = weaponConfig.AK47Prefab; weaponName = "AK47"; break;
                        case 3: prefabToSpawn = weaponConfig.AWPPrefab; weaponName = "AWP"; break;
                        case 4: prefabToSpawn = weaponConfig.RocketLauncherPrefab; weaponName = "RocketLauncher"; break;
                    }

                    if (prefabToSpawn != Entity.Null)
                    {
                        Debug.Log($"[DropSystem] Spawnowanie broni: {weaponName}");

                        Entity droppedWeapon = ecb.Instantiate(prefabToSpawn);

                        float3 spawnPos = transform.ValueRO.Position;
                        spawnPos.y = 5.9f;

                        ecb.SetComponent(droppedWeapon, LocalTransform.FromPositionRotationScale(
                            spawnPos,
                            quaternion.identity,
                            1.0f));

                        ecb.AddComponent(droppedWeapon, new GhostOwner { NetworkId = -1 });
                        ecb.SetComponent(droppedWeapon, new GhostState { IsDestroyed = false });
                    }
                    else
                    {
                        Debug.LogError($"[DropSystem] Wylosowano {weaponName}, ale prefab w WeaponUIPrefabsConfig jest NULL!");
                    }
                }
                else
                {
                    Debug.Log("[DropSystem] Rzut nieudany - brak dropu.");
                }

                // Usuwamy komponent
                ecb.RemoveComponent<DropWeapon>(entity);
            }
        }
    }
}