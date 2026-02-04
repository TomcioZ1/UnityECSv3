using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct WeaponPickupSystem : ISystem
{
    private ComponentLookup<WeaponPickup> pickupLookup;
    private ComponentLookup<PlayerInventory> inventoryLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        pickupLookup = state.GetComponentLookup<WeaponPickup>(true);
        inventoryLookup = state.GetComponentLookup<PlayerInventory>(false);

        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        pickupLookup.Update(ref state);
        inventoryLookup.Update(ref state);

        // Uruchamiamy Job fizyki
        var job = new PickupTriggerJob
        {
            PickupLookup = pickupLookup,
            InventoryLookup = inventoryLookup,
            ECB = ecb
        };

        state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    [BurstCompile]
    struct PickupTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<WeaponPickup> PickupLookup;
        public ComponentLookup<PlayerInventory> InventoryLookup;
        public EntityCommandBuffer ECB;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            // Sprawdzamy, która encja to gracz, a która to pickup
            bool isPlayerA = InventoryLookup.HasComponent(entityA);
            bool isPickupB = PickupLookup.HasComponent(entityB);

            if (isPlayerA && isPickupB)
            {
                ProcessPickup(entityA, entityB);
            }
            else if (InventoryLookup.HasComponent(entityB) && PickupLookup.HasComponent(entityA))
            {
                ProcessPickup(entityB, entityA);
            }
        }

        private void ProcessPickup(Entity player, Entity pickupEntity)
        {
            var inventory = InventoryLookup[player];
            var pickup = PickupLookup[pickupEntity];
            bool pickedUp = false;

            // Logika slotów (identyczna jak u Ciebie)
            if (pickup.WeaponId >= 10) // Granaty
            {
                inventory.Slot4_GrenadeId = pickup.WeaponId;
                inventory.ActiveSlotIndex = 4;
                pickedUp = true;
            }
            else
            {
                if (inventory.Slot1_WeaponId == 0)
                {
                    inventory.Slot1_WeaponId = pickup.WeaponId;
                    inventory.ActiveSlotIndex = 1;
                    pickedUp = true;
                }
                else if (inventory.Slot2_WeaponId == 0)
                {
                    inventory.Slot2_WeaponId = pickup.WeaponId;
                    inventory.ActiveSlotIndex = 2;
                    pickedUp = true;
                }
            }

            if (pickedUp)
            {
                InventoryLookup[player] = inventory;
                ECB.DestroyEntity(pickupEntity); // Usuwamy pickup
            }
        }
    }
}