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

        // Pobieramy dane gracza. Jeœli gracz ma IsDestroyedTag, pêtla siê nie wykona.
        // U¿ywamy SystemAPI.Query, co jest standardem w Unity 6.
        foreach (var (ltw, localTag) in SystemAPI.Query<RefRO<LocalToWorld>, EnabledRefRO<GhostOwnerIsLocal>>()
                     .WithAll<PlayerTag>()
                     .WithNone<IsDestroyedTag>())
        {
            float3 playerPos = ltw.ValueRO.Position;
            float3 targetPos = playerPos + (float3)_cachedProxy.Offset;
            Transform camTransform = _cachedProxy.transform;

            float3 currentPos = camTransform.position;
            float dt = SystemAPI.Time.DeltaTime;

            // --- ZABEZPIECZENIE PRZED DRGANIEM I RESPAWNEM ---
            float distSq = math.distancesq(currentPos, targetPos);

            // Jeœli dystans jest ogromny (> 15m), to znaczy, ¿e gracz siê zrespawnowa³ 
            // lub w³aœnie zgin¹³ (teleportacja pod mapê). 
            // Wtedy robimy natychmiastowy skok pozycji.
            if (distSq > 225f)
            {
                camTransform.position = targetPos;
            }
            else
            {
                // Wyk³adnicze wyg³adzanie
                float smoothingStrength = 1.0f - math.exp(-_cachedProxy.Smoothness * dt);
                camTransform.position = math.lerp(currentPos, targetPos, smoothingStrength);
            }

            // Sztywne ustawienie rotacji kamery
            camTransform.rotation = Quaternion.Euler(_cachedProxy.PitchAngle, 0, 0);

            // ZnaleŸliœmy naszego gracza, koñczymy
            break;
        }
    }
}