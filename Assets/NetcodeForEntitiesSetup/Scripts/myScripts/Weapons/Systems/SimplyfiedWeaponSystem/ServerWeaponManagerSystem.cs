/*using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerWeaponManagerSystemv2 : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<WeaponResources>(out var res)) return;
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var ghostOwnerLookup = state.GetComponentLookup<GhostOwner>(true);

        foreach (var (inventory, input, socket, playerEntity) in
                 SystemAPI.Query<RefRW<PlayerInventory>, RefRO<MyPlayerInput>, RefRO<WeaponSocket>>()
                 .WithAll<Simulate>().WithEntityAccess())
        {
            // 1. Zmiana slotu
            if (input.ValueRO.choosenWeapon >= 1 && input.ValueRO.choosenWeapon <= 4)
                inventory.ValueRW.ActiveSlotIndex = input.ValueRO.choosenWeapon;

            // 2. Logika wyboru ID (Slot 3 to zawsze rêce - ID 99)
            byte targetId = inventory.ValueRO.ActiveSlotIndex switch
            {
                1 => inventory.ValueRO.Slot1_WeaponId,
                2 => inventory.ValueRO.Slot2_WeaponId,
                3 => 99, // Rêce
                4 => inventory.ValueRO.Slot4_GrenadeId,
                _ => 0
            };

            // 3. Spawnowanie jeœli ID siê zmieni³o
            if (inventory.ValueRO.CurrentlySpawnedWeaponId != targetId)
            {
                if (inventory.ValueRO.CurrentWeaponEntity != Entity.Null)
                    ecb.DestroyEntity(inventory.ValueRO.CurrentWeaponEntity);

                Entity prefab = targetId switch
                {
                    99 => res.gun, // U¿yj res.gun lub dodaj 'Hands' do WeaponResources
                    1 => res.Pistol,
                    2 => res.Shotgun,
                    3 => res.ak47,
                    _ => Entity.Null
                };

                if (prefab != Entity.Null)
                {
                    Entity newWep = ecb.Instantiate(prefab);
                    ecb.AddComponent(newWep, new Parent { Value = socket.ValueRO.WeaponSocketEntity });

                    // Wa¿ne: Przypisanie w³aœciciela
                    if (ghostOwnerLookup.TryGetComponent(playerEntity, out var owner))
                        ecb.SetComponent(newWep, new GhostOwner { NetworkId = owner.NetworkId });

                    // ¯eby broñ zniknê³a razem z graczem
                    ecb.AppendToBuffer(playerEntity, new LinkedEntityGroup { Value = newWep });
                    inventory.ValueRW.CurrentWeaponEntity = newWep;
                }

                inventory.ValueRW.CurrentlySpawnedWeaponId = targetId;
            }
        }
    }
}*/