using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct WeaponControlSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<WeaponResources>(out var resources)) return;

        // U¿ywamy EndSimulation, aby mieæ pewnoœæ, ¿e wszystko zostanie zrealizowane przed nastêpn¹ klatk¹ Netcode
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var ghostOwnerLookup = state.GetComponentLookup<GhostOwner>(true);

        foreach (var (inventory, input, socket, playerEntity) in
                 SystemAPI.Query<RefRW<PlayerInventory>, RefRO<MyPlayerInput>, RefRO<WeaponSocket>>()
                 .WithEntityAccess())
        {
            // 1. Aktualizacja slotu
            if (input.ValueRO.choosenWeapon >= 1 && input.ValueRO.choosenWeapon <= 4)
            {
                inventory.ValueRW.ActiveSlotIndex = input.ValueRO.choosenWeapon;
            }

            // 2. Wyznaczenie ID broni
            byte targetWeaponId = inventory.ValueRO.ActiveSlotIndex switch
            {
                1 => inventory.ValueRO.Slot1_WeaponId,
                2 => inventory.ValueRO.Slot2_WeaponId,
                3 => inventory.ValueRO.Slot3_HandsId,
                4 => inventory.ValueRO.Slot4_GrenadeId,
                _ => 0
            };

            // 3. Logika zmiany
            if (targetWeaponId != inventory.ValueRO.CurrentlySpawnedWeaponId)
            {
                // Usuwamy star¹ broñ
                if (inventory.ValueRO.CurrentWeaponEntity != Entity.Null)
                {
                    ecb.DestroyEntity(inventory.ValueRO.CurrentWeaponEntity);
                }

                Entity prefabToSpawn = targetWeaponId switch
                {
                    1 => resources.Pistol,
                    2 => resources.Shotgun,
                    3 => resources.ak47,
                    4 => resources.m4a1,
                    5 => resources.mp5,
                    6 => resources.uzi,
                    7 => resources.gun,
                    8 => resources.awp,
                    9 => resources.PKM,
                    _ => Entity.Null
                };

                // Tworzymy now¹ instancjê inwentarza do nadpisania przez ECB
                var updatedInventory = inventory.ValueRO;
                updatedInventory.CurrentlySpawnedWeaponId = targetWeaponId;

                if (prefabToSpawn != Entity.Null)
                {
                    Entity newWeaponSpawned = ecb.Instantiate(prefabToSpawn);

                    ecb.AddComponent(newWeaponSpawned, new Parent { Value = socket.ValueRO.WeaponSocketEntity });

                    if (ghostOwnerLookup.HasComponent(playerEntity))
                    {
                        var playerOwner = ghostOwnerLookup[playerEntity];
                        ecb.SetComponent(newWeaponSpawned, new GhostOwner { NetworkId = playerOwner.NetworkId });
                    }

                    ecb.AppendToBuffer(playerEntity, new LinkedEntityGroup { Value = newWeaponSpawned });
                    ecb.AddComponent(newWeaponSpawned, new WeaponOwner { Entity = playerEntity });

                    // KLUCZ: Przypisujemy encjê przez ECB, a nie bezpoœrednio!
                    updatedInventory.CurrentWeaponEntity = newWeaponSpawned;
                }
                else
                {
                    updatedInventory.CurrentWeaponEntity = Entity.Null;
                }

                // Aktualizujemy ca³y komponent inwentarza przez ECB
                ecb.SetComponent(playerEntity, updatedInventory);
            }
        }
    }
}