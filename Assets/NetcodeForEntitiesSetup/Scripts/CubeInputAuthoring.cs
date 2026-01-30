using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
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
                AddComponent(entity, new MyPlayerInput { choosenWeapon = 3 });
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
                    var weapon = playerInput.ValueRO.choosenWeapon;
                    playerInput.ValueRW = default;
                    playerInput.ValueRW.choosenWeapon = weapon;
                }
                return;
            }

            float3 worldMousePos = float3.zero;
            bool hasValidMousePos = false;

            if (Camera.main != null)
            {
                Vector2 screenMousePos = Mouse.current.position.ReadValue();
                UnityEngine.Ray ray = Camera.main.ScreenPointToRay(screenMousePos);
                UnityEngine.Plane groundPlane = new UnityEngine.Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    worldMousePos = (float3)ray.GetPoint(enter);
                    hasValidMousePos = true;
                }
            }

            var left = Keyboard.current.aKey.isPressed;
            var right = Keyboard.current.dKey.isPressed;
            var down = Keyboard.current.sKey.isPressed;
            var up = Keyboard.current.wKey.isPressed;
            var leftMouse = Mouse.current.leftButton.isPressed;

            var choosenWeapon = Keyboard.current.digit1Key.isPressed ? (byte)1 :
                               Keyboard.current.digit2Key.isPressed ? (byte)2 :
                               Keyboard.current.digit3Key.isPressed ? (byte)3 :
                               Keyboard.current.digit4Key.isPressed ? (byte)4 : (byte)0;

            foreach (var playerInput in SystemAPI.Query<RefRW<MyPlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                playerInput.ValueRW.leftMouseButton = leftMouse ? (byte)1 : (byte)0;
                if (choosenWeapon != 0) playerInput.ValueRW.choosenWeapon = choosenWeapon;

                playerInput.ValueRW.Horizontal = 0;
                if (left) playerInput.ValueRW.Horizontal -= 1;
                if (right) playerInput.ValueRW.Horizontal += 1;

                playerInput.ValueRW.Vertical = 0;
                if (down) playerInput.ValueRW.Vertical -= 1;
                if (up) playerInput.ValueRW.Vertical += 1;

                if (hasValidMousePos) playerInput.ValueRW.MouseWorldPos = worldMousePos;
            }
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct CubeMovementSystem : ISystem
    {
        private ComponentLookup<GhostOwnerIsLocal> ghostOwnerLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            ghostOwnerLookup = state.GetComponentLookup<GhostOwnerIsLocal>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var moveSpeed = 5f;
            ghostOwnerLookup.Update(ref state);

            // Zmieniamy zapytanie: dodajemy PhysicsVelocity, usuwamy modyfikacjê LocalTransform.Position
            foreach (var (input, velocity, trans, entity) in
                     SystemAPI.Query<RefRO<MyPlayerInput>, RefRW<PhysicsVelocity>, RefRW<LocalTransform>>()
                     .WithAll<Simulate>()
                     .WithEntityAccess())
            {
                // 1. RUCH FIZYCZNY
                float2 moveInput = new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical);

                if (math.lengthsq(moveInput) > 0.001f)
                {
                    moveInput = math.normalize(moveInput) * moveSpeed;
                    // Ustawiamy prêdkoœæ zamiast zmieniaæ pozycjê. 
                    // Unity Physics samo przesunie obiekt w oparciu o tê prêdkoœæ, uwzglêdniaj¹c kolizje.
                    velocity.ValueRW.Linear = new float3(moveInput.x, 0, moveInput.y);
                }
                else
                {
                    // Zatrzymujemy postaæ, gdy nie ma inputu (inaczej bêdzie siê œlizgaæ)
                    velocity.ValueRW.Linear = new float3(0, 0, 0);
                }

                // 2. ROTACJA (Pozostaje bez zmian, bo rotacja zwykle nie koliduje tak samo jak pozycja)
                if (ghostOwnerLookup.HasComponent(entity))
                {
                    float3 dirToMouse = input.ValueRO.MouseWorldPos - trans.ValueRO.Position;
                    dirToMouse.y = 0;

                    if (math.lengthsq(dirToMouse) > 0.1f)
                    {
                        trans.ValueRW.Rotation = quaternion.LookRotationSafe(dirToMouse, math.up());
                    }
                }
            }
        }
    }


}