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

        // Pobieramy lookupy - one lepiej radz¹ sobie w systemach predykcji
        var transformLookup = state.GetComponentLookup<LocalTransform>(false);
        var parentLookup = state.GetComponentLookup<Parent>(true);

        foreach (var (inventory, hands, socket) in
                 SystemAPI.Query<RefRO<PlayerInventory>, RefRO<ActiveHands>, RefRO<WeaponSocket>>())
        {
            Entity currentWeapon = inventory.ValueRO.CurrentWeaponEntity;

            // KLUCZOWE: Jeœli encja jest nowa (Index < 0), skipujemy tê klatkê.
            // Czekamy, a¿ ECB "zmaterializuje" broñ w œwiecie.
            if (currentWeapon == Entity.Null || currentWeapon.Index < 0) continue;

            bool showHands = (inventory.ValueRO.ActiveSlotIndex == 3);
            bool showWeapon = !showHands && (inventory.ValueRO.CurrentlySpawnedWeaponId > 0);

            // Sprawdzanie Parenta przez Lookup, nie przez EntityManager
            if (transformLookup.HasComponent(currentWeapon))
            {
                if (!parentLookup.HasComponent(currentWeapon))
                {
                    ecb.AddComponent(currentWeapon, new Parent { Value = socket.ValueRO.WeaponSocketEntity });
                    ecb.SetComponent(currentWeapon, LocalTransform.FromPosition(float3.zero));
                }
            }

            // Aktualizacja skali
            UpdateVisibility(ecb, currentWeapon, showWeapon, ref transformLookup);
            UpdateVisibility(ecb, hands.ValueRO.LeftHandEntity, showHands, ref transformLookup);
            UpdateVisibility(ecb, hands.ValueRO.RightHandEntity, showHands, ref transformLookup);
        }
    }

    private void UpdateVisibility(EntityCommandBuffer ecb, Entity e, bool isVisible, ref ComponentLookup<LocalTransform> lookup)
    {
        // Ponownie: zabezpieczenie przed deferred entities
        if (e == Entity.Null || e.Index < 0 || !lookup.HasComponent(e)) return;

        var trans = lookup[e];
        float targetScale = isVisible ? 1.0f : 0.0f;

        if (math.abs(trans.Scale - targetScale) > 0.001f)
        {
            trans.Scale = targetScale;
            ecb.SetComponent(e, trans);
        }
    }
}*/