using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct WeaponPickupSystem : ISystem
{
    private ComponentLookup<WeaponPickup> pickupLookup;
    private ComponentLookup<PlayerInventory> inventoryLookup;
    private ComponentLookup<GhostInstance> ghostInstanceLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        pickupLookup = state.GetComponentLookup<WeaponPickup>(true);
        inventoryLookup = state.GetComponentLookup<PlayerInventory>(false);
        ghostInstanceLookup = state.GetComponentLookup<GhostInstance>(true);

        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<DestroyedGhostElement>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        if (!SystemAPI.TryGetSingletonEntity<DestroyedGhostElement>(out Entity bufferEntity))
            return;

        var destroyedBuffer = SystemAPI.GetBuffer<DestroyedGhostElement>(bufferEntity);

        pickupLookup.Update(ref state);
        inventoryLookup.Update(ref state);
        ghostInstanceLookup.Update(ref state);

        var job = new PickupTriggerJob
        {
            PickupLookup = pickupLookup,
            InventoryLookup = inventoryLookup,
            GhostInstanceLookup = ghostInstanceLookup,
            ECB = ecb,
            DestroyedBuffer = destroyedBuffer
        };

        state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    [BurstCompile]
    struct PickupTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<WeaponPickup> PickupLookup;
        [ReadOnly] public ComponentLookup<GhostInstance> GhostInstanceLookup;
        public ComponentLookup<PlayerInventory> InventoryLookup;

        public EntityCommandBuffer ECB;
        public DynamicBuffer<DestroyedGhostElement> DestroyedBuffer;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            if (InventoryLookup.HasComponent(entityA) && PickupLookup.HasComponent(entityB))
                ProcessPickup(entityA, entityB);
            else if (InventoryLookup.HasComponent(entityB) && PickupLookup.HasComponent(entityA))
                ProcessPickup(entityB, entityA);
        }

        private void ProcessPickup(Entity player, Entity pickupEntity)
        {
            var inventory = InventoryLookup[player];
            var pickup = PickupLookup[pickupEntity];
            bool pickedUp = false;

            // Logika podnoszenia (uproszczona dla czytelnoœci)
            if (pickup.WeaponId >= 10) { inventory.Slot4_GrenadeId = pickup.WeaponId; pickedUp = true; }
            else if (inventory.Slot1_WeaponId == 0) { inventory.Slot1_WeaponId = pickup.WeaponId; pickedUp = true; }
            else if (inventory.Slot2_WeaponId == 0) { inventory.Slot2_WeaponId = pickup.WeaponId; pickedUp = true; }

            if (pickedUp)
            {
                InventoryLookup[player] = inventory;

                ECB.DestroyEntity(pickupEntity);
            }
        }
    }
}