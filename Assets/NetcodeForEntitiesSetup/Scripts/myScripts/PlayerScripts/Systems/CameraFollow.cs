using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

// U¿ywamy standardowej grupy transformacji
[UpdateInGroup(typeof(TransformSystemGroup))]
// Kluczowe: czekamy, a¿ LocalToWorld zostanie obliczone dla tej klatki
[UpdateAfter(typeof(LocalToWorldSystem))]
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

        // Reszta kodu pozostaje bez zmian
        foreach (var (ltw, localTag) in SystemAPI.Query<RefRO<LocalToWorld>, EnabledRefRO<GhostOwnerIsLocal>>()
                     .WithAll<PlayerTag>())
        {
            float3 playerPos = ltw.ValueRO.Position;
            float3 targetPos = playerPos + (float3)_cachedProxy.Offset;

            Transform camTransform = _cachedProxy.transform;
            float dt = SystemAPI.Time.DeltaTime;

            // Wyg³adzanie wyk³adnicze (Exponential Decay)
            // Im wy¿szy Smoothness, tym szybciej kamera reaguje
            float smoothingStrength = 1.0f - math.exp(-_cachedProxy.Smoothness * dt);

            float3 currentPos = camTransform.position;
            float3 newPos = math.lerp(currentPos, targetPos, smoothingStrength);

            camTransform.position = newPos;
            camTransform.rotation = Quaternion.Euler(_cachedProxy.PitchAngle, 0, 0);

            break;
        }
    }
}