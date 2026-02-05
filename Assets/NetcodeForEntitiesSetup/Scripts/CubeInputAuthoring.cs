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
        // Przygotowujemy lookup, aby sprawdziæ, czy encja nale¿y do lokalnego gracza
        ghostOwnerLookup = state.GetComponentLookup<GhostOwnerIsLocal>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Prêdkoœæ poruszania siê
        var moveSpeed = 3f;
        ghostOwnerLookup.Update(ref state);

        // SystemAPI.Query jest bardzo wydajne w Unity 6
        foreach (var (input, velocity, trans, entity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRW<PhysicsVelocity>, RefRW<LocalTransform>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // --- 1. RUCH LINIOWY (Fizyka) ---
            float2 moveInput = new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical);
            float3 newLinearVelocity = float3.zero;

            if (math.lengthsq(moveInput) > 0.001f)
            {
                // Normalizacja zapobiega szybszemu bieganiu "na ukos"
                float2 normalizedInput = math.normalize(moveInput);
                newLinearVelocity = new float3(normalizedInput.x * moveSpeed, 0, normalizedInput.y * moveSpeed);
            }

            // Aplikujemy prêdkoœæ, zachowuj¹c obecn¹ prêdkoœæ w osi Y (wa¿ne dla grawitacji!)
            velocity.ValueRW.Linear = new float3(newLinearVelocity.x, velocity.ValueRO.Linear.y, newLinearVelocity.z);

            // --- 2. ROTACJA (Zoptymalizowana pod Burst) ---
            // Obracamy postaæ tylko jeœli steruje ni¹ lokalny gracz
            if (ghostOwnerLookup.HasComponent(entity))
            {
                float3 targetPoint = input.ValueRO.MouseWorldPos;
                float3 currentPos = trans.ValueRO.Position;

                // Obliczamy wektor kierunku od gracza do myszy
                float3 direction = targetPoint - currentPos;

                // KLUCZ: Zerujemy ró¿nicê wysokoœci (Y), aby wymusiæ obrót tylko wokó³ osi pionowej
                direction.y = 0;

                if (math.lengthsq(direction) > 0.001f)
                {
                    // LookRotationSafe tworzy rotacjê patrz¹c¹ w stronê znormalizowanego wektora.
                    // math.up() jako "world up" gwarantuje, ¿e osie X i Z rotacji pozostan¹ na 0.
                    trans.ValueRW.Rotation = quaternion.LookRotationSafe(math.normalize(direction), math.up());
                }
            }

            // --- 3. BLOKADA K¥TOWA (Zabezpieczenie fizyki) ---
            // Wymuszamy, aby fizyka nie obraca³a kapsu³¹ w osiach X i Z (zapobiega przewracaniu)
            // Jeœli Twoja postaæ nie musi siê krêciæ jak b¹czek przy kolizjach, zerujemy te¿ Y.
            velocity.ValueRW.Angular = float3.zero;
        }
    }
}