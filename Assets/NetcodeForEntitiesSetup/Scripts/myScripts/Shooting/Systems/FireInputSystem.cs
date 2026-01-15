using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class FireInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Pobieramy kamerê (standardowe podejœcie w ECS)
        var cam = Camera.main;
        if (cam == null) return;

        // Szukamy tylko NASZEJ encji gracza
        foreach (var input in SystemAPI.Query<RefRW<PlayerShootInput>>().WithAll<GhostOwnerIsLocal>())
        {
            bool isPressed = Mouse.current.leftButton.isPressed;
            input.ValueRW.ShootPrimary = (byte)(isPressed ? 1 : 0);

            // Obliczamy kierunek strza³u z pozycji myszy
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            input.ValueRW.AimDirection = ray.direction;
        }
    }
}