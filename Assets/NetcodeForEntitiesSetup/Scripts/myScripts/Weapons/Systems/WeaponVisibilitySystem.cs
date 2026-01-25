using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct WeaponVisibilitySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var baseScaleLookup = state.GetComponentLookup<BaseScale>(true);
        var postMatrixLookup = state.GetComponentLookup<PostTransformMatrix>(false);
        var transformLookup = state.GetComponentLookup<LocalTransform>(false);

        foreach (var (activeWeapon, activeHands) in
                 SystemAPI.Query<RefRO<ActiveWeapon>, RefRO<ActiveHands>>())
        {
            byte choice = activeWeapon.ValueRO.SelectedWeaponId;
            bool weaponVisible = (choice == 1 || choice == 2);
            bool handsVisible = (choice == 3);

            // KLUCZOWA POPRAWKA: Sprawdzamy czy Index > -1
            if (activeWeapon.ValueRO.WeaponEntity != Entity.Null && activeWeapon.ValueRO.WeaponEntity.Index >= 0)
                UpdateVisibility(activeWeapon.ValueRO.WeaponEntity, weaponVisible, ref postMatrixLookup, ref transformLookup, baseScaleLookup);

            if (activeHands.ValueRO.LeftHandEntity != Entity.Null && activeHands.ValueRO.LeftHandEntity.Index >= 0)
                UpdateVisibility(activeHands.ValueRO.LeftHandEntity, handsVisible, ref postMatrixLookup, ref transformLookup, baseScaleLookup);

            if (activeHands.ValueRO.RightHandEntity != Entity.Null && activeHands.ValueRO.RightHandEntity.Index >= 0)
                UpdateVisibility(activeHands.ValueRO.RightHandEntity, handsVisible, ref postMatrixLookup, ref transformLookup, baseScaleLookup);
        }
    }

    [BurstCompile]
    private void UpdateVisibility(Entity e, bool isVisible, ref ComponentLookup<PostTransformMatrix> matrixLookup, ref ComponentLookup<LocalTransform> transformLookup, ComponentLookup<BaseScale> scaleLookup)
    {
        // Tutaj HasComponent jest ju¿ bezpieczne, bo sprawdziliœmy Index powy¿ej
        if (!transformLookup.HasComponent(e) || !scaleLookup.HasComponent(e)) return;

        float3 originalScale = scaleLookup[e].Value;

        var trans = transformLookup[e];
        trans.Scale = isVisible ? math.max(originalScale.x, math.max(originalScale.y, originalScale.z)) : 0.0001f;
        transformLookup[e] = trans;

        if (matrixLookup.HasComponent(e))
        {
            float3 currentTargetScale = isVisible ? originalScale : new float3(0.0001f);
            // Wyjaœnienie 4x4 poni¿ej
            matrixLookup[e] = new PostTransformMatrix { Value = float4x4.Scale(currentTargetScale) };
        }
    }
}