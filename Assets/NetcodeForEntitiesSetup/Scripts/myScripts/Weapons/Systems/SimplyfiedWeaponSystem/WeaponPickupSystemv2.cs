/*using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))] // Wa¿ne: po symulacji fizyki
public partial struct WeaponPickupSystemv2 : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Upewniamy siê, ¿e systemy i dane s¹ gotowe
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Przygotowujemy lookupy dla Joba
        var invLookup = state.GetComponentLookup<PlayerInventoryv2>(false);
        var pickupLookup = state.GetComponentLookup<WeaponPickup>(true);

        // Tworzymy i planujemy Joba
        var pickupJob = new PickupTriggerJob
        {
            InventoryLookup = invLookup,
            PickupLookup = pickupLookup,
            ECB = ecb
        };

        // Harmonogramowanie Joba na zdarzeniach z SimulationSingleton
        state.Dependency = pickupJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    [BurstCompile]
    struct PickupTriggerJob : ITriggerEventsJob
    {
        public ComponentLookup<PlayerInventoryv2> InventoryLookup;
        [ReadOnly] public ComponentLookup<WeaponPickup> PickupLookup;
        public EntityCommandBuffer ECB;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            // Sprawdzamy parê: Gracz + Przedmiot
            if (InventoryLookup.HasComponent(entityA) && PickupLookup.HasComponent(entityB))
            {
                ProcessPickup(entityA, entityB);
            }
            else if (InventoryLookup.HasComponent(entityB) && PickupLookup.HasComponent(entityA))
            {
                ProcessPickup(entityB, entityA);
            }
        }

        private void ProcessPickup(Entity player, Entity item)
        {
            var inv = InventoryLookup[player];
            var pickup = PickupLookup[item];

            // 1. Aktualizacja danych inwentarza
            inv.WeaponId = pickup.WeaponId;
            inv.ActiveSlotIndex = 1;

            // 2. Zapisanie zmian w graczu
            // Wewn¹trz ITriggerEventsJob musimy u¿yæ ECB, bo nie mo¿emy bezpoœrednio modyfikowaæ danych
            ECB.SetComponent(player, inv);

            // 3. Usuniêcie przedmiotu z ziemi
            ECB.DestroyEntity(item);
        }
    }
}*/