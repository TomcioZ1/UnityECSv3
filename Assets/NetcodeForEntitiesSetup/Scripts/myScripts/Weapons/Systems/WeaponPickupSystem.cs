using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct WeaponPickupSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // U¿ywamy tego samego ECB co w systemie broni, ¿eby unikn¹æ konfliktów synchronizacji
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (inventory, playerTransform, playerEntity) in
                 SystemAPI.Query<RefRW<PlayerInventory>, RefRO<LocalTransform>>()
                 .WithEntityAccess())
        {
            float3 playerPos = playerTransform.ValueRO.Position;

            // U¿ywamy pêtli Query po pickupach
            foreach (var (pickup, pickupTransform, pickupEntity) in
                     SystemAPI.Query<RefRO<WeaponPickup>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
            {
                float dist = math.distance(playerPos, pickupTransform.ValueRO.Position);

                // Sprawdzamy dystans (1.0f mo¿e byæ ma³y, zale¿y od skali modeli)
                if (dist < 1.5f)
                {
                    byte pickedWeaponId = pickup.ValueRO.WeaponId;
                    bool pickedUp = false;

                    // Logika przypisywania do slotów
                    if (pickedWeaponId >= 10) // Granaty
                    {
                        inventory.ValueRW.Slot4_GrenadeId = pickedWeaponId;
                        inventory.ValueRW.ActiveSlotIndex = 4;
                        pickedUp = true;
                    }
                    else
                    {
                        if (inventory.ValueRO.Slot1_WeaponId == 0)
                        {
                            inventory.ValueRW.Slot1_WeaponId = pickedWeaponId;
                            inventory.ValueRW.ActiveSlotIndex = 1;
                            pickedUp = true;
                        }
                        else if (inventory.ValueRO.Slot2_WeaponId == 0)
                        {
                            inventory.ValueRW.Slot2_WeaponId = pickedWeaponId;
                            inventory.ValueRW.ActiveSlotIndex = 2;
                            pickedUp = true;
                        }
                    }

                    if (pickedUp)
                    {
                        // To niszczy encjê pickupa na serwerze i automatycznie u wszystkich klientów (Netcode)
                        ecb.DestroyEntity(pickupEntity);

                        // Przerywamy wewnêtrzn¹ pêtlê, ¿eby gracz nie podniós³ 10 broni w jednej klatce
                        break;
                    }
                }
            }
        }
    }
}