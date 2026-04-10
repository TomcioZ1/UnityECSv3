using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public struct MyPlayerInput : IInputComponentData
{
    public int Horizontal;
    public int Vertical;
    public byte leftMouseButton;
    public byte rightMouseButton;
    public byte reloadRequested;
    public float3 MouseWorldPos;
    public float3 AimDirection;
    public byte choosenWeapon;
}

public struct PlayerCharacterProperties : IComponentData
{
    public float Radius;           // Szerokoœæ kolizji (np. 0.4f)
    public float VerticalVelocity; // Aktualna prêdkoœæ spadania
    public float Gravity;          // Wartoœæ grawitacji (np. -15.0f)
    public float CharacterHeightOffset;
    public float StepHeight;
}

[DisallowMultipleComponent]
public class PlayerInputAuthoring : MonoBehaviour
{
    class PlayerInputAuthoringBaker : Baker<PlayerInputAuthoring>
    {
        public override void Bake(PlayerInputAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MyPlayerInput { choosenWeapon = 3 });
            AddComponent(entity, new PlayerCharacterProperties { Radius = 0.125f, StepHeight = 0.0f, Gravity = -2, CharacterHeightOffset = 0.2f });
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
//[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial struct MyPlayerInputSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. SprawdŸ czy to na pewno œwiat klienta (dodatkowe zabezpieczenie)
        if (!state.WorldUnmanaged.IsClient()) return;


        // 2. Pobierz urz¹dzenia bezpiecznie
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null)
            return;
        

        float3 worldMousePos = float3.zero;
        bool hasValidMousePos = false;

        // 3. Bezpieczny dostêp do kamery - Camera.main nie zawsze dzia³a w ECS
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector2 screenMousePos = mouse.position.ReadValue();
            UnityEngine.Ray ray = mainCam.ScreenPointToRay(screenMousePos);
            UnityEngine.Plane groundPlane = new UnityEngine.Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float enter))
            {
                worldMousePos = (float3)ray.GetPoint(enter);
                hasValidMousePos = true;
            }
        }

        // 4. Odczyt klawiszy przez lokalne zmienne (bezpieczniej dla Burst)
        var left = keyboard.aKey.isPressed;
        var right = keyboard.dKey.isPressed;
        var down = keyboard.sKey.isPressed;
        var up = keyboard.wKey.isPressed;
        var leftMouse = mouse.leftButton.isPressed;
        var rkeypressed = keyboard.rKey.isPressed;

        var weapon1 = keyboard.digit1Key.isPressed;
        var weapon2 = keyboard.digit2Key.isPressed;
        var weapon3 = keyboard.digit3Key.isPressed;
        var weapon4 = keyboard.digit4Key.isPressed;

        byte choosenWeapon = weapon1 ? (byte)1 : weapon2 ? (byte)2 : weapon3 ? (byte)3 : weapon4 ? (byte)4 : (byte)0;
        

        // 5. Query - u¿ywamy GhostOwnerIsLocal, aby wype³niæ input tylko dla naszego gracza
        foreach (var playerInput in SystemAPI.Query<RefRW<MyPlayerInput>>().WithAll<GhostOwnerIsLocal>())
        {
            playerInput.ValueRW.leftMouseButton = leftMouse ? (byte)1 : (byte)0;
            playerInput.ValueRW.reloadRequested = rkeypressed ? (byte)1 : (byte)0;
            if (choosenWeapon != 0) playerInput.ValueRW.choosenWeapon = choosenWeapon;

            int h = 0;
            if (left) h -= 1;
            if (right) h += 1;
            playerInput.ValueRW.Horizontal = h;

            int v = 0;
            if (down) v -= 1;
            if (up) v += 1;
            playerInput.ValueRW.Vertical = v;
            if (hasValidMousePos) playerInput.ValueRW.MouseWorldPos = worldMousePos;


        }
    }
}






