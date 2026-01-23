using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class FireInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var cam = Camera.main;
        // W BUILDZIE: Kamera MUSI mieæ tag "MainCamera", inaczej tu wyjdzie null
        if (cam == null) return;

        // Pobieramy pozycjê myszy
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        // STABILNE OBLICZANIE PUNKTU (Zamiast dzielenia przez kierunek y)
        // Tworzymy p³aszczyznê na wysokoœci Y=0
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float3 worldMousePoint;

        if (groundPlane.Raycast(ray, out float distance))
        {
            worldMousePoint = (float3)ray.GetPoint(distance);
        }
        else
        {
            // Jeœli promieñ nie trafi³ w pod³ogê (np. celujesz w niebo), przerywamy klatkê
            return;
        }

        // Szukamy lokalnego gracza i aktualizujemy jego input
        foreach (var (input, transform) in SystemAPI.Query<RefRW<MyPlayerInput>, RefRO<LocalTransform>>().WithAll<GhostOwnerIsLocal>())
        {
            if (input.ValueRO.leftMouseButton != 0)
            {
                
                // Obliczamy kierunek
                float3 direction = worldMousePoint - transform.ValueRO.Position;
                direction.y = 0;

                // Zapisujemy znormalizowany kierunek
                if (math.lengthsq(direction) > 0.001f)
                {
                    input.ValueRW.AimDirection = math.normalize(direction);
                }
            }

        }
    }
}