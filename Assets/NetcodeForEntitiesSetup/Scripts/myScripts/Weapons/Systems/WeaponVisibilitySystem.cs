using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct WeaponVisibilitySystem : ISystem
{
    // 1. Deklarujemy pola Lookup jako pola struktury
    private ComponentLookup<BaseScale> _baseScaleLookup;
    private ComponentLookup<PostTransformMatrix> _postMatrixLookup;
    private ComponentLookup<LocalTransform> _transformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 2. Inicjalizujemy Lookup w OnCreate
        _baseScaleLookup = state.GetComponentLookup<BaseScale>(true);
        _postMatrixLookup = state.GetComponentLookup<PostTransformMatrix>(false);
        _transformLookup = state.GetComponentLookup<LocalTransform>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 3. Aktualizujemy stan Lookup na pocz¹tku OnUpdate
        _baseScaleLookup.Update(ref state);
        _postMatrixLookup.Update(ref state);
        _transformLookup.Update(ref state);

        foreach (var (activeWeapon, activeHands) in
                 SystemAPI.Query<RefRO<ActiveWeapon>, RefRO<ActiveHands>>())
        {
            byte choice = activeWeapon.ValueRO.SelectedWeaponId;
            bool weaponVisible = (choice == 1 || choice == 2);
            bool handsVisible = (choice == 3);

            if (activeWeapon.ValueRO.WeaponEntity != Entity.Null && activeWeapon.ValueRO.WeaponEntity.Index >= 0)
                UpdateVisibility(activeWeapon.ValueRO.WeaponEntity, weaponVisible);

            if (activeHands.ValueRO.LeftHandEntity != Entity.Null && activeHands.ValueRO.LeftHandEntity.Index >= 0)
                UpdateVisibility(activeHands.ValueRO.LeftHandEntity, handsVisible);

            if (activeHands.ValueRO.RightHandEntity != Entity.Null && activeHands.ValueRO.RightHandEntity.Index >= 0)
                UpdateVisibility(activeHands.ValueRO.RightHandEntity, handsVisible);
        }
    }

    [BurstCompile]
    private void UpdateVisibility(Entity e, bool isVisible)
    {
        // U¿ywamy pól klasy bezpoœrednio (bez przekazywania ich przez parametry)
        if (!_transformLookup.HasComponent(e) || !_baseScaleLookup.HasComponent(e)) return;

        float3 originalScale = _baseScaleLookup[e].Value;

        var trans = _transformLookup[e];
        trans.Scale = isVisible ? math.max(originalScale.x, math.max(originalScale.y, originalScale.z)) : 0.0001f;
        _transformLookup[e] = trans;

        if (_postMatrixLookup.HasComponent(e))
        {
            float3 currentTargetScale = isVisible ? originalScale : new float3(0.0001f);
            _postMatrixLookup[e] = new PostTransformMatrix { Value = float4x4.Scale(currentTargetScale) };
        }
    }
}