using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct WeaponVisibilitySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var baseScaleLookup = state.GetComponentLookup<BaseScale>(true);
        var postMatrixLookup = state.GetComponentLookup<PostTransformMatrix>(false);
        var transformLookup = state.GetComponentLookup<LocalTransform>(false);

        // USUNIÊTO MyPlayerInput - teraz czytamy TYLKO z danych zsynchronizowanych (Ghost)
        foreach (var (activeWeapon, activeHands) in
                 SystemAPI.Query<RefRO<ActiveWeapon>, RefRO<ActiveHands>>())
        {
            // Teraz 'choice' pochodzi z GhostField, wiêc ka¿dy klient ma tê sam¹ wartoœæ
            byte choice = activeWeapon.ValueRO.SelectedWeaponId;

            bool weaponVisible = (choice == 1 || choice == 2);
            bool handsVisible = (choice == 3);

            // AKTUALIZACJA BRONI
            UpdateVisibility(activeWeapon.ValueRO.WeaponEntity, weaponVisible,
                ref postMatrixLookup, ref transformLookup, baseScaleLookup);

            // AKTUALIZACJA R¥K
            UpdateVisibility(activeHands.ValueRO.LeftHandEntity, handsVisible,
                ref postMatrixLookup, ref transformLookup, baseScaleLookup);
            UpdateVisibility(activeHands.ValueRO.RightHandEntity, handsVisible,
                ref postMatrixLookup, ref transformLookup, baseScaleLookup);
        }
    }

    [BurstCompile]
    private void UpdateVisibility(Entity e, bool isVisible,
        ref ComponentLookup<PostTransformMatrix> matrixLookup,
        ref ComponentLookup<LocalTransform> transformLookup,
        ComponentLookup<BaseScale> scaleLookup)
    {
        if (e == Entity.Null || !scaleLookup.HasComponent(e)) return;

        float3 originalScale = scaleLookup[e].Value;
        float3 currentTargetScale = isVisible ? originalScale : new float3(0.0001f);

        if (transformLookup.HasComponent(e))
        {
            var trans = transformLookup[e];
            // Skala jednolita dla transformu (wymagana przez silnik)
            trans.Scale = isVisible ? math.max(originalScale.x, math.max(originalScale.y, originalScale.z)) : 0.0001f;
            transformLookup[e] = trans;
        }

        if (matrixLookup.HasComponent(e))
        {
            // Skala 3D dla wizualizacji (obs³uguje non-uniform scale)
            matrixLookup[e] = new PostTransformMatrix { Value = float4x4.Scale(currentTargetScale) };
        }
    }
}