using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    public struct MyPlayerInput : IInputComponentData
    {
        public int Horizontal;
        public int Vertical;
        public byte leftMouseButton;
        public byte rightMouseButton;
        public float3 MouseWorldPos;
        public float3 AimDirection;
        public byte choosenWeapon;
    }

    [DisallowMultipleComponent]
    public class CubeInputAuthoring : MonoBehaviour
    {
        class Baking : Baker<CubeInputAuthoring>
        {
            public override void Bake(CubeInputAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MyPlayerInput>(entity);
                // Strzelanie zostaje nietkniête zgodnie z instrukcj¹
                //AddComponent<PlayerShootInput>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [BurstCompile]
    public partial struct SampleCubeInput : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalPauseState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.GetSingleton<LocalPauseState>().IsPaused)
            {
                foreach (var playerInput in SystemAPI.Query<RefRW<MyPlayerInput>>().WithAll<GhostOwnerIsLocal>())
                {
                    playerInput.ValueRW = default;
                }
                return;
            }

            // --- POPRAWKA ROTACJI: STABILNY RAYCAST ---
            float3 worldMousePos = float3.zero;
            bool hasValidMousePos = false;

            if (Camera.main != null)
            {
                Vector2 screenMousePos = Mouse.current.position.ReadValue();
                Ray ray = Camera.main.ScreenPointToRay(screenMousePos);

                // U¿ywamy p³aszczyzny matematycznej - to rozwi¹zuje problem "dziwnego" obracania w buildzie
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    worldMousePos = (float3)ray.GetPoint(enter);
                    hasValidMousePos = true;
                }
            }

#if ENABLE_INPUT_SYSTEM
            var left = Keyboard.current.aKey.isPressed;
            var right = Keyboard.current.dKey.isPressed;
            var down = Keyboard.current.sKey.isPressed;
            var up = Keyboard.current.wKey.isPressed;
            var leftMouse = Mouse.current.leftButton.isPressed;
            var rightMouse = Mouse.current.rightButton.isPressed;
            var choosenWeapon = Keyboard.current.digit1Key.isPressed ? (byte)1 :
                               Keyboard.current.digit2Key.isPressed ? (byte)2 :
                               Keyboard.current.digit3Key.isPressed ? (byte)3 :
                               Keyboard.current.digit3Key.isPressed ? (byte)4 :
                               (byte)0;
#else
            var left = UnityEngine.Input.GetKey(KeyCode.A);
            var right = UnityEngine.Input.GetKey(KeyCode.D);
            var down = UnityEngine.Input.GetKey(KeyCode.S);
            var up = UnityEngine.Input.GetKey(KeyCode.W);
            var leftMouse = UnityEngine.Input.GetMouseButton(0);
            var rightMouse = UnityEngine.Input.GetMouseButton(1);

#endif

            foreach (var playerInput in SystemAPI.Query<RefRW<MyPlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                var input = playerInput.ValueRW; // Zachowujemy obecny stan
                if(leftMouse) input.leftMouseButton = 1; else input.leftMouseButton = 0;
                if (leftMouse) input.leftMouseButton = 1; else input.leftMouseButton = 0;
                input.choosenWeapon = choosenWeapon;

                input.Horizontal = 0;
                if (left) input.Horizontal -= 1;
                if (right) input.Horizontal += 1;

                input.Vertical = 0;
                if (down) input.Vertical -= 1;
                if (up) input.Vertical += 1;

                // Aktualizujemy pozycjê myszy tylko jeœli raycast trafi³ w ziemiê
                // Zapobiega to gwa³townemu odwracaniu siê postaci do (0,0,0) gdy mysz ucieknie z okna
                if (hasValidMousePos)
                {
                    input.MouseWorldPos = worldMousePos;
                }

                playerInput.ValueRW = input;
            }
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct CubeMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var moveSpeed = 5f;

            foreach (var (input, trans) in
                     SystemAPI.Query<RefRO<MyPlayerInput>, RefRW<LocalTransform>>()
                     .WithAll<Simulate>())
            {
                // 1. RUCH
                float2 moveInput = new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical);
                if (math.lengthsq(moveInput) > 0.001f)
                {
                    moveInput = math.normalize(moveInput) * moveSpeed * dt;
                    trans.ValueRW.Position += new float3(moveInput.x, 0, moveInput.y);
                }

                // 2. ROTACJA (Stabilizacja dla Builda)
                float3 dirToMouse = input.ValueRO.MouseWorldPos - trans.ValueRO.Position;
                dirToMouse.y = 0;

                // Zwiêkszony próg (0.1f zamiast 0.01f) eliminuje drgania wynikaj¹ce z precyzji w buildzie
                if (math.lengthsq(dirToMouse) > 0.1f)
                {
                    trans.ValueRW.Rotation = quaternion.LookRotationSafe(dirToMouse, math.up());
                }
            }
        }
    }
}