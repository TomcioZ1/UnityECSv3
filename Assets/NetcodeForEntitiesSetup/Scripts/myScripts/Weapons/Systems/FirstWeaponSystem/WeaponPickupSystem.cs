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
public partial struct WeaponPickupSystem : ISystem
{
    private ComponentLookup<WeaponPickup> pickupLookup;
    private ComponentLookup<PlayerInventory> inventoryLookup;
    private ComponentLookup<GhostState> ghostStateLookup;
    private BufferLookup<LinkedEntityGroup> linkedEntityLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Inicjalizacja Lookupów
        pickupLookup = state.GetComponentLookup<WeaponPickup>(true);
        inventoryLookup = state.GetComponentLookup<PlayerInventory>(false);
        ghostStateLookup = state.GetComponentLookup<GhostState>(false);
        linkedEntityLookup = state.GetBufferLookup<LinkedEntityGroup>(true);

        // System wymaga danych fizyki do dzia³ania
        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()) return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Aktualizacja danych w ka¿dej klatce przed uruchomieniem Joba
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

        // Podpiêcie pod zdarzenia Triggerów z silnika fizyki Unity Physics
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

            // Sprawdzanie która encja to gracz, a która to pickup
            if (InventoryLookup.HasComponent(entityA) && PickupLookup.HasComponent(entityB))
                ProcessPickup(entityA, entityB);
            else if (InventoryLookup.HasComponent(entityB) && PickupLookup.HasComponent(entityA))
                ProcessPickup(entityB, entityA);
        }

        private void ProcessPickup(Entity player, Entity pickupEntity)
        {
            // Podstawowe zabezpieczenie przed Ghostami i duplikacj¹ pickupu
            if (!GhostStateLookup.HasComponent(pickupEntity)) return;

            var ghostState = GhostStateLookup[pickupEntity];
            if (ghostState.IsDestroyed) return;

            var inventory = InventoryLookup[player];
            var pickup = PickupLookup[pickupEntity];
            bool pickedUp = false;

            // LOGIKA PODMIANY BRONI:

            // 1. Jeœli to granat (ID >= 10), po prostu przypisz do slotu granatów
            if (pickup.WeaponId >= 10)
            {
                inventory.Slot4_GrenadeId = pickup.WeaponId;
                pickedUp = true;
            }
            // 2. Jeœli to broñ palna (ID < 10), NADPISZ obecn¹ broñ
            else
            {
                // Tutaj usuwamy warunek "if == 0", aby nowa broñ zawsze wchodzi³a na miejsce starej
                inventory.Slot1_WeaponId = pickup.WeaponId;
                pickedUp = true;

                // UWAGA: Jeœli chcia³byœ wyrzucaæ star¹ broñ na ziemiê, 
                // musia³byœ tutaj wys³aæ ¿¹danie zmaterializowania nowego pickupa 
                // z ID, które w³aœnie nadpisujesz.
            }

            if (pickedUp)
            {
                // Zapisujemy zmiany w ekwipunku gracza
                InventoryLookup[player] = inventory;

                // 1. Oznaczamy dla NetCode, ¿e ten obiekt na serwerze "nie ¿yje"
                ghostState.IsDestroyed = true;
                GhostStateLookup[pickupEntity] = ghostState;

                // 2. Wy³¹czamy renderowanie i fizykê, aby obiekt znikn¹³ natychmiastowo
                // Sprawdzamy dzieci (np. modele 3D, efekty), jeœli istniej¹ w LinkedEntityGroup
                if (LinkedEntityLookup.HasBuffer(pickupEntity))
                {
                    var children = LinkedEntityLookup[pickupEntity];
                    for (int i = 0; i < children.Length; i++)
                    {
                        DisableEntity(children[i].Value);
                    }
                }

                DisableEntity(pickupEntity);
            }
        }

        private void DisableEntity(Entity e)
        {
            // Zapobiega rysowaniu obiektu na ekranie
            ECB.AddComponent<DisableRendering>(e);

            // Usuwa collider, aby gracz nie "odbija³" siê od niewidzialnej broni 
            // i nie wyzwala³ triggera ponownie przed pe³nym usuniêciem
            if (PickupLookup.HasComponent(e))
            {
                ECB.RemoveComponent<PhysicsCollider>(e);
            }
        }
    }
}