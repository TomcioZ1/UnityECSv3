using Unity.Burst;
using Unity.Entities;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Transforms;
// System jak prze³¹czamy bronie np 1 póŸniej klikamy 2 to sie bron zmienia

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct WeaponControlSystem : ISystem
{
    private ComponentLookup<GhostOwner> _ghostOwnerLookup;

    public void OnCreate(ref SystemState state)
    {
        _ghostOwnerLookup = state.GetComponentLookup<GhostOwner>(true);

        // Krytyczne: System nie wykona OnUpdate, dopóki te dane nie istniej¹
        state.RequireForUpdate<WeaponResources>();
        state.RequireForUpdate<PlayerInventory>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Bezpieczne sprawdzanie singletonów - jeœli ich nie ma, wychodzimy natychmiast
        if (!SystemAPI.TryGetSingleton<WeaponResources>(out var resources)) return;
        if (!SystemAPI.TryGetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>(out var ecbSingleton)) return;

        // 2. Tworzenie ECB
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 3. Aktualizacja Lookupów
        _ghostOwnerLookup.Update(ref state);

        // 4. Glówna pêtla systemu
        foreach (var (inventory, input, socket, playerEntity) in
                 SystemAPI.Query<RefRW<PlayerInventory>, RefRO<MyPlayerInput>, RefRO<WeaponSocket>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // Aktualizacja wybranego slotu
            if (input.ValueRO.choosenWeapon >= 1 && input.ValueRO.choosenWeapon <= 4)
            {
                inventory.ValueRW.ActiveSlotIndex = input.ValueRO.choosenWeapon;
            }

            // Wyznaczanie ID broni (ID musi odpowiadaæ Twojej logice w grze)
            byte targetWeaponId = inventory.ValueRO.ActiveSlotIndex switch
            {
                1 => inventory.ValueRO.Slot1_WeaponId,
                2 => inventory.ValueRO.Slot2_WeaponId,
                3 => inventory.ValueRO.Slot3_HandsId,
                4 => inventory.ValueRO.Slot4_GrenadeId,
                _ => 0
            };

            // Logika zmiany broni
            if (targetWeaponId != inventory.ValueRO.CurrentlySpawnedWeaponId)
            {
                // Usuwanie starej encji
                if (inventory.ValueRO.CurrentWeaponEntity != Entity.Null)
                {
                    if (state.EntityManager.Exists(inventory.ValueRO.CurrentWeaponEntity))
                    {
                        ecb.DestroyEntity(inventory.ValueRO.CurrentWeaponEntity);
                    }
                }

                // Mapowanie ID na prefab z WeaponResources
                Entity prefabToSpawn = Entity.Null;
                switch (targetWeaponId)
                {
                    case 1: prefabToSpawn = resources.Pistol; break;
                    case 2: prefabToSpawn = resources.Shotgun; break;
                    case 3: prefabToSpawn = resources.ak47; break;
                    case 4: prefabToSpawn = resources.m4a1; break;
                    case 5: prefabToSpawn = resources.mp5; break;
                    case 6: prefabToSpawn = resources.uzi; break;
                    case 7: prefabToSpawn = resources.gun; break;
                    case 8: prefabToSpawn = resources.awp; break;
                    case 9: prefabToSpawn = resources.PKM; break;
                }

                // Aktualizacja stanu inwentarza
                var updatedInventory = inventory.ValueRO;
                updatedInventory.CurrentlySpawnedWeaponId = targetWeaponId;

                if (prefabToSpawn != Entity.Null)
                {
                    // Spawnowanie nowej broni
                    Entity newWeaponSpawned = ecb.Instantiate(prefabToSpawn);

                    // Ustawienie rodzica (Socket)
                    ecb.AddComponent(newWeaponSpawned, new Parent { Value = socket.ValueRO.WeaponSocketEntity });

                    // Przypisanie w³aœciciela (NetworkId)
                    if (_ghostOwnerLookup.HasComponent(playerEntity))
                    {
                        var playerOwner = _ghostOwnerLookup[playerEntity];
                        ecb.SetComponent(newWeaponSpawned, new GhostOwner { NetworkId = playerOwner.NetworkId });
                    }

                    // Rejestracja w grupie i przypisanie WeaponOwner
                    ecb.AppendToBuffer(playerEntity, new LinkedEntityGroup { Value = newWeaponSpawned });
                    ecb.AddComponent(newWeaponSpawned, new WeaponOwner { Entity = playerEntity });

                    updatedInventory.CurrentWeaponEntity = newWeaponSpawned;
                }
                else
                {
                    updatedInventory.CurrentWeaponEntity = Entity.Null;
                }

                // Zapisanie zmian w inwentarzu gracza
                ecb.SetComponent(playerEntity, updatedInventory);
            }
        }
    }
}