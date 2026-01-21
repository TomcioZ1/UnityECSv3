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
        public float3 MouseWorldPos; // Przechowuje punkt celowania w œwiecie
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
                // Zak³adam, ¿e PlayerShootInput jest zdefiniowany gdzie indziej
                AddComponent<PlayerShootInput>(entity); 
            }
        }
    }

    [UpdateInGroup(typeof(GhostInputSystemGroup))]
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

            // 1. OBLICZANIE POZYCJI MYSZY W ŒWIECIE (Raycast do p³aszczyzny Y=0)
            float3 worldMousePos = float3.zero;
            if (Camera.main != null)
            {
                Vector2 screenMousePos = Mouse.current.position.ReadValue();
                Ray ray = Camera.main.ScreenPointToRay(screenMousePos);

                // Matematyczne przeciêcie promienia z p³aszczyzn¹ Y=0 (pod³oga)
                if (ray.direction.y != 0)
                {
                    float distance = -ray.origin.y / ray.direction.y;
                    worldMousePos = (float3)ray.GetPoint(distance);
                }
            }

#if ENABLE_INPUT_SYSTEM
            var left = Keyboard.current.aKey.isPressed;
            var right = Keyboard.current.dKey.isPressed;
            var down = Keyboard.current.sKey.isPressed;
            var up = Keyboard.current.wKey.isPressed;
#else
            var left = UnityEngine.Input.GetKey(KeyCode.A);
            var right = UnityEngine.Input.GetKey(KeyCode.D);
            var down = UnityEngine.Input.GetKey(KeyCode.S);
            var up = UnityEngine.Input.GetKey(KeyCode.W);
#endif

            foreach (var playerInput in SystemAPI.Query<RefRW<MyPlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                // Resetujemy tylko kierunki, zachowuj¹c dane o myszy jeœli potrzebne, 
                // ale tutaj nadpisujemy wszystko dla czystoœci.
                var input = new MyPlayerInput();

                if (left) input.Horizontal -= 1;
                if (right) input.Horizontal += 1;
                if (down) input.Vertical -= 1;
                if (up) input.Vertical += 1;

                input.MouseWorldPos = worldMousePos;
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
            var moveSpeed = 5f; // Sta³a prêdkoœæ

            foreach (var (input, trans) in
                     SystemAPI.Query<RefRO<MyPlayerInput>, RefRW<LocalTransform>>()
                     .WithAll<Simulate>())
            {
                // 1. RUCH (Niezale¿ny od obrotu - koordynaty œwiata)
                float2 moveInput = new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical);
                moveInput = math.normalizesafe(moveInput) * moveSpeed * dt;
                trans.ValueRW.Position += new float3(moveInput.x, 0, moveInput.y);

                // 2. ROTACJA (Patrzenie w stronê myszki)
                float3 dirToMouse = input.ValueRO.MouseWorldPos - trans.ValueRO.Position;
                dirToMouse.y = 0; // Blokujemy pochylanie siê kapsu³y

                if (math.lengthsq(dirToMouse) > 0.01f)
                {
                    // Ustawiamy rotacjê kapsu³y tak, by patrzy³a na punkt myszy
                    trans.ValueRW.Rotation = quaternion.LookRotationSafe(dirToMouse, math.up());
                }
            }
        }
    }
}