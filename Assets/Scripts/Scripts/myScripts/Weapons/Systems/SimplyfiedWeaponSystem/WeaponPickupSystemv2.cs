/*using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Collections; // To naprawi b³¹d [ReadOnly]

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
public partial struct WeaponPickupSystemv2 : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var invLookup = state.GetComponentLookup<PlayerInventory>(false);
        var pickupLookup = state.GetComponentLookup<WeaponPickup>(true);

        // Lookup do sprawdzania czy to serwer czy klient
        var job = new PickupJob
        {
            InventoryLookup = invLookup,
            PickupLookup = pickupLookup,
            ECB = ecb,
            IsServer = state.WorldUnmanaged.IsServer()
        };
        state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    [BurstCompile]
    struct PickupJob : ITriggerEventsJob
    {
        public ComponentLookup<PlayerInventory> InventoryLookup;
        [ReadOnly] public ComponentLookup<WeaponPickup> PickupLookup;
        public EntityCommandBuffer ECB;
        public bool IsServer;

        public void Execute(TriggerEvent triggerEvent)
        {
            Process(triggerEvent.EntityA, triggerEvent.EntityB);
            Process(triggerEvent.EntityB, triggerEvent.EntityA);
        }

        void Process(Entity player, Entity item)
        {
            if (InventoryLookup.HasComponent(player) && PickupLookup.HasComponent(item))
            {
                var inv = InventoryLookup[player];
                inv.Slot1_WeaponId = PickupLookup[item].WeaponId;
                inv.ActiveSlotIndex = 1;
                ECB.SetComponent(player, inv);

                if (IsServer)
                {
                    // TYLKO SERWER niszczy encjê
                    ECB.DestroyEntity(item);
                }
                else
                {
                    // KLIENT tylko ukrywa, ¿eby nie by³o b³êdu "Despawn Ghost"
                    ECB.AddComponent<Unity.Rendering.DisableRendering>(item);
                }
            }
        }
    }
}*/