using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms; // Potrzebne do pobrania pozycji gracza
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class FireInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Pobieramy pozycjê myszy raz na klatkê
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        // Obliczamy punkt przeciêcia z ziemi¹ (Y=0)
        // Matematyka: dystans do ziemi = -wysokoœæ_startowa / kierunek_y
        float distance = -ray.origin.y / ray.direction.y;
        float3 worldMousePoint = (float3)ray.GetPoint(distance);

        // Szukamy lokalnego gracza i aktualizujemy jego input
        foreach (var (input, transform) in SystemAPI.Query<RefRW<PlayerShootInput>, RefRO<LocalTransform>>().WithAll<GhostOwnerIsLocal>())
        {
            bool isPressed = Mouse.current.leftButton.isPressed;
            input.ValueRW.ShootPrimary = (byte)(isPressed ? 1 : 0);

            // KLUCZ: AimDirection to wektor od gracza do punktu myszy na ziemi
            float3 direction = worldMousePoint - transform.ValueRO.Position;
            direction.y = 0; // Zerujemy Y, ¿eby pocisk nie lecia³ w ziemiê ani w górê

            // Zapisujemy znormalizowany kierunek
            if (math.lengthsq(direction) > 0.001f)
            {
                input.ValueRW.AimDirection = math.normalize(direction);
            }
        }
    }
}