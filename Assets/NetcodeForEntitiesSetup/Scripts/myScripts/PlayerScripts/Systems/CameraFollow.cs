using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CameraFollowSystem : SystemBase
{
    private CameraTargetProxy _cachedProxy;

    protected override void OnUpdate()
    {
        // 1. ZnajdŸ proxy kamery (jeœli nie ma w cache)
        if (_cachedProxy == null)
        {
            _cachedProxy = Object.FindFirstObjectByType<CameraTargetProxy>();
            if (_cachedProxy == null) return;
        }

        // 2. Bezpieczne znalezienie lokalnego gracza
        // Query automatycznie odfiltruje encje, gdzie GhostOwnerIsLocal jest wy³¹czony
        foreach (var (ltw, localTag) in SystemAPI.Query<RefRO<LocalToWorld>, EnabledRefRO<GhostOwnerIsLocal>>())
        {
            // 3. Obliczenia pozycji
            Vector3 targetPos = (Vector3)ltw.ValueRO.Position + _cachedProxy.Offset;
            float dt = SystemAPI.Time.DeltaTime;

            Transform camTransform = _cachedProxy.transform;

            // P³ynne pod¹¿anie
            camTransform.position = Vector3.Lerp(camTransform.position, targetPos, dt * _cachedProxy.Smoothness);

            // Sztywna rotacja
            camTransform.rotation = Quaternion.Euler(_cachedProxy.PitchAngle, 0, 0);

            // Skoro znaleŸliœmy lokalnego gracza i zaktualizowaliœmy kamerê, mo¿emy wyjœæ z pêtli
            break;
        }
    }
}