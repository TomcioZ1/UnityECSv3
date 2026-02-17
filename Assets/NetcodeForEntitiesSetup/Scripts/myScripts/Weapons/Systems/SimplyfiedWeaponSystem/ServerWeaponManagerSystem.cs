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
        if (!SystemAPI.TryGetSingleton<WeaponResourcesv2>(out var res)) return;
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var ghostOwnerLookup = state.GetComponentLookup<GhostOwner>(true);

        foreach (var (inventory, input, socket, playerEntity) in
                 SystemAPI.Query<RefRW<PlayerInventoryv2>, RefRO<MyPlayerInput>, RefRO<WeaponSocket>>()
                 .WithAll<Simulate>().WithEntityAccess())
        {
            // Prze³¹czanie slotów
            if (input.ValueRO.choosenWeapon >= 1 && input.ValueRO.choosenWeapon <= 2)
                inventory.ValueRW.ActiveSlotIndex = input.ValueRO.choosenWeapon;

            // Logika wyboru prefabu
            byte currentTargetId = (inventory.ValueRO.ActiveSlotIndex == 2) ? (byte)99 : inventory.ValueRO.WeaponId;
            // 99 = techniczne ID dla r¹k

            if (inventory.ValueRO.LastActiveSlot != inventory.ValueRO.ActiveSlotIndex ||
                inventory.ValueRO.LastSpawnedId != currentTargetId)
            {
                if (inventory.ValueRO.CurrentWeaponEntity != Entity.Null)
                    ecb.DestroyEntity(inventory.ValueRO.CurrentWeaponEntity);

                Entity prefabToSpawn = Entity.Null;

                if (inventory.ValueRO.ActiveSlotIndex == 2)
                    prefabToSpawn = res.Hands; // Spawn r¹k
                else
                    prefabToSpawn = inventory.ValueRO.WeaponId switch
                    {
                        1 => res.Pistol,
                        2 => res.Shotgun,
                        3 => res.AK47,
                        _ => Entity.Null
                    };

                if (prefabToSpawn != Entity.Null)
                {
                    Entity newWep = ecb.Instantiate(prefabToSpawn);
                    ecb.AddComponent(newWep, new Parent { Value = socket.ValueRO.WeaponSocketEntity });

                    if (ghostOwnerLookup.TryGetComponent(playerEntity, out var owner))
                        ecb.SetComponent(newWep, new GhostOwner { NetworkId = owner.NetworkId });

                    ecb.AppendToBuffer(playerEntity, new LinkedEntityGroup { Value = newWep });
                    inventory.ValueRW.CurrentWeaponEntity = newWep;
                }

                inventory.ValueRW.LastActiveSlot = inventory.ValueRO.ActiveSlotIndex;
                inventory.ValueRW.LastSpawnedId = currentTargetId;
            }
        }
    }
}*/