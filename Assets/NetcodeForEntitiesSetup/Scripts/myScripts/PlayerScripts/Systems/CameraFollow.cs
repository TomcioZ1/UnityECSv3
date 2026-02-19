using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial class CameraFollowSystem : SystemBase
{
    private CameraTargetProxy _cachedProxy;

    protected override void OnUpdate()
    {
        if (_cachedProxy == null)
        {
            _cachedProxy = Object.FindFirstObjectByType<CameraTargetProxy>();
            if (_cachedProxy == null) return;
        }

        // U¿ywamy Entity w Query, aby mieæ dostêp do danych encji gracza
        foreach (var (transform, inventory, localTag) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<PlayerInventory>, EnabledRefRO<GhostOwnerIsLocal>>()
                     .WithAll<PlayerTag>()
                     .WithNone<IsDestroyedTag>())
        {
            Entity weaponEntity = inventory.ValueRO.CurrentWeaponEntity;
            float3 playerPos = transform.ValueRO.Position;
            float3 targetPos = playerPos;

            // --- NAPRAWA: Bezpieczne pobieranie komponentu broni ---
            // Najpierw sprawdzamy, czy encja nie jest Null i czy posiada komponent WeaponData
            if (weaponEntity != Entity.Null && SystemAPI.HasComponent<WeaponData>(weaponEntity))
            {
                var weaponData = SystemAPI.GetComponent<WeaponData>(weaponEntity);

                // Teraz bezpiecznie modyfikujemy targetPos o offset kamery broni
                targetPos = new float3(targetPos.x, targetPos.y + weaponData.cameraOffset, targetPos.z);

                // Jeœli potrzebujesz weaponOffset (ProjectileSpawner), pobierz go tutaj:
                // var weaponOffset = weaponData.ProjectileSpawner; 
            }
            else
            {
                targetPos = playerPos + (float3)_cachedProxy.Offset;
            }

                Transform camTransform = _cachedProxy.transform;
            float3 currentPos = camTransform.position;
            float dt = SystemAPI.Time.DeltaTime;

            // --- ZABEZPIECZENIE PRZED DRGANIEM I RESPAWNEM ---
            float distSq = math.distancesq(currentPos, targetPos);

            if (distSq > 225f) // > 15m
            {
                camTransform.position = targetPos;
            }
            else
            {
                float smoothingStrength = 1.0f - math.exp(-_cachedProxy.Smoothness * dt);
                camTransform.position = math.lerp(currentPos, targetPos, smoothingStrength);
            }

            camTransform.rotation = Quaternion.Euler(_cachedProxy.PitchAngle, 0, 0);

            break; // ZnaleŸliœmy lokalnego gracza, wychodzimy z pêtli
        }
    }
}