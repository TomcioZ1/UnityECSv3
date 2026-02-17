using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
//[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct WeaponPickupSystem : ISystem
{
    private ComponentLookup<WeaponPickup> pickupLookup;
    private ComponentLookup<PlayerInventory> inventoryLookup;
    private ComponentLookup<GhostState> ghostStateLookup;
    private BufferLookup<LinkedEntityGroup> linkedEntityLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        pickupLookup = state.GetComponentLookup<WeaponPickup>(true);
        inventoryLookup = state.GetComponentLookup<PlayerInventory>(false);
        ghostStateLookup = state.GetComponentLookup<GhostState>(false);
        linkedEntityLookup = state.GetBufferLookup<LinkedEntityGroup>(true);

        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()) return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        pickupLookup.Update(ref state);
        inventoryLookup.Update(ref state);
        ghostStateLookup.Update(ref state);
        linkedEntityLookup.Update(ref state);

        var job = new PickupTriggerJob
        {
            PickupLookup = pickupLookup,
            InventoryLookup = inventoryLookup,
            GhostStateLookup = ghostStateLookup,
            LinkedEntityLookup = linkedEntityLookup,
            ECB = ecb
        };

        state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    [BurstCompile]
    struct PickupTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<WeaponPickup> PickupLookup;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedEntityLookup;
        public ComponentLookup<PlayerInventory> InventoryLookup;
        public ComponentLookup<GhostState> GhostStateLookup;
        public EntityCommandBuffer ECB;

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
            if (!GhostStateLookup.HasComponent(pickupEntity)) return;

            var ghostState = GhostStateLookup[pickupEntity];
            // Jeœli ju¿ zniszczone (IsDestroyed) lub posiada tag (przetworzone), przerywamy
            if (ghostState.IsDestroyed) return;

            var inventory = InventoryLookup[player];
            var pickup = PickupLookup[pickupEntity];
            bool pickedUp = false;

            if (pickup.WeaponId >= 10)
            {
                inventory.Slot4_GrenadeId = pickup.WeaponId;
                pickedUp = true;
            }
            else if (inventory.Slot1_WeaponId == 0)
            {
                inventory.Slot1_WeaponId = pickup.WeaponId;
                pickedUp = true;
            }

            if (pickedUp)
            {
                InventoryLookup[player] = inventory;

                // 1. Ustawiamy stan dla NetCode (synchronizacja z klientem)
                ghostState.IsDestroyed = true;
                GhostStateLookup[pickupEntity] = ghostState;

                // 2. Dodajemy tag blokuj¹cy ponowne wejœcie w logikê na serwerze
                //ECB.AddComponent<AlreadyProcessedTag>(pickupEntity);

                // 3. Czyœcimy wizualia i fizykê (na serwerze g³ównie fizykê)
                if (LinkedEntityLookup.HasBuffer(pickupEntity))
                {
                    var children = LinkedEntityLookup[pickupEntity];
                    for (int i = 0; i < children.Length; i++)
                    {
                        DisableEntity(children[i].Value);
                    }
                }
                else
                {
                    DisableEntity(pickupEntity);
                }
            }
        }

        private void DisableEntity(Entity e)
        {
            ECB.AddComponent<DisableRendering>(e);
            ECB.RemoveComponent<PhysicsCollider>(e);
        }
    }
}