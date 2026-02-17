/*using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
public partial struct WeaponVisualsSystemv2 : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var transformLookup = state.GetComponentLookup<LocalTransform>();

        foreach (var (inventory, hands, socket) in
                 SystemAPI.Query<RefRO<PlayerInventoryv2>, RefRO<ActiveHands>, RefRO<WeaponSocket>>())
        {
            Entity currentWeapon = inventory.ValueRO.CurrentWeaponEntity;

            // Logika widocznoœci:
            // Poka¿ broñ: Slot 1 ORAZ mamy przypisane ID broni
            bool showWeapon = (inventory.ValueRO.ActiveSlotIndex == 1 && inventory.ValueRO.WeaponId > 0);
            // Poka¿ rêce: Slot 2 LUB (Slot 1 ale brak broni)
            bool showHands = (inventory.ValueRO.ActiveSlotIndex == 2) || (inventory.ValueRO.ActiveSlotIndex == 1 && inventory.ValueRO.WeaponId == 0);

            // 1. Obs³uga "przyklejenia" broni do socketu
            if (currentWeapon != Entity.Null && transformLookup.HasComponent(currentWeapon))
            {
                if (!state.EntityManager.HasComponent<Parent>(currentWeapon))
                {
                    ecb.AddComponent(currentWeapon, new Parent { Value = socket.ValueRO.WeaponSocketEntity });
                    ecb.SetComponent(currentWeapon, LocalTransform.FromPosition(float3.zero));
                }
            }

            // 2. Aktualizacja skalowania (widocznoœci)
            UpdateVisibility(ref state, currentWeapon, showWeapon);
            UpdateVisibility(ref state, hands.ValueRO.LeftHandEntity, showHands);
            UpdateVisibility(ref state, hands.ValueRO.RightHandEntity, showHands);
        }
    }

    private void UpdateVisibility(ref SystemState state, Entity e, bool isVisible)
    {
        if (e == Entity.Null || !state.EntityManager.HasComponent<LocalTransform>(e)) return;

        var trans = state.EntityManager.GetComponentData<LocalTransform>(e);
        float targetScale = isVisible ? 1.0f : 0.0f;

        if (math.abs(trans.Scale - targetScale) > 0.001f)
        {
            trans.Scale = targetScale;
            state.EntityManager.SetComponentData(e, trans);
        }
    }
}*/