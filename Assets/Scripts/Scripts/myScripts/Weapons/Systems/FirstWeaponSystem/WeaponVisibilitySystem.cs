using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]

// Ukrywa niepotrzebne modele broni i r¹k
public partial struct WeaponVisibilitySystem : ISystem
{
    private ComponentLookup<BaseScale> _baseScaleLookup;
    private ComponentLookup<PostTransformMatrix> _postMatrixLookup;
    private ComponentLookup<LocalTransform> _transformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _baseScaleLookup = state.GetComponentLookup<BaseScale>(true);
        _postMatrixLookup = state.GetComponentLookup<PostTransformMatrix>(false);
        _transformLookup = state.GetComponentLookup<LocalTransform>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _baseScaleLookup.Update(ref state);
        _postMatrixLookup.Update(ref state);
        _transformLookup.Update(ref state);

        foreach (var (inventory, activeHands) in
                 SystemAPI.Query<RefRO<PlayerInventory>, RefRO<ActiveHands>>())
        {
            byte activeSlot = inventory.ValueRO.ActiveSlotIndex;

            // Sprawdzamy, czy na aktualnym slocie (1 lub 2) faktycznie mamy broñ (ID > 0)
            bool hasWeaponInActiveSlot = activeSlot switch
            {
                1 => inventory.ValueRO.Slot1_WeaponId > 0,
                2 => inventory.ValueRO.Slot2_WeaponId > 0,
                4 => inventory.ValueRO.Slot4_GrenadeId > 0,
                _ => false
            };

            // LOGIKA WIDOCZNOŒCI:
            // 1. Model broni widoczny tylko gdy: wybrany slot broni (1,2,4) ORAZ mamy tam przypisane ID
            bool weaponModelVisible = (activeSlot == 1 || activeSlot == 2 || activeSlot == 4) && hasWeaponInActiveSlot;

            // 2. Rêce widoczne gdy:
            // - Wybrany slot 3 (dedykowane rêce)
            // - LUB wybrany slot broni (1,2,4), ale ten slot jest PUSTY (brak broni)
            bool handsVisible = (activeSlot == 3) || ((activeSlot == 1 || activeSlot == 2 || activeSlot == 4) && !hasWeaponInActiveSlot);

            // Aktualizacja modelu broni
            if (inventory.ValueRO.CurrentWeaponEntity != Entity.Null)
            {
                UpdateVisibility(inventory.ValueRO.CurrentWeaponEntity, weaponModelVisible);
            }

            // Aktualizacja r¹k
            if (activeHands.ValueRO.LeftHandEntity != Entity.Null)
                UpdateVisibility(activeHands.ValueRO.LeftHandEntity, handsVisible);

            if (activeHands.ValueRO.RightHandEntity != Entity.Null)
                UpdateVisibility(activeHands.ValueRO.RightHandEntity, handsVisible);
        }
    }

    [BurstCompile]
    private void UpdateVisibility(Entity e, bool isVisible)
    {
        // Sprawdzamy czy encja jest poprawna (nie jest w stanie "deferred")
        if (e == Entity.Null || e.Index < 0) return;
        if (!_transformLookup.HasComponent(e) || !_baseScaleLookup.HasComponent(e)) return;

        float3 originalScale = _baseScaleLookup[e].Value;
        var trans = _transformLookup[e];

        // Ukrywamy ustawiaj¹c skalê na 0 (lub prawie 0)
        float targetUniformScale = isVisible ? math.max(originalScale.x, math.max(originalScale.y, originalScale.z)) : 0f;

        if (trans.Scale != targetUniformScale)
        {
            trans.Scale = targetUniformScale;
            _transformLookup[e] = trans;
        }

        if (_postMatrixLookup.HasComponent(e))
        {
            float3 targetScale3D = isVisible ? originalScale : float3.zero;
            _postMatrixLookup[e] = new PostTransformMatrix { Value = float4x4.Scale(targetScale3D) };
        }
    }
}