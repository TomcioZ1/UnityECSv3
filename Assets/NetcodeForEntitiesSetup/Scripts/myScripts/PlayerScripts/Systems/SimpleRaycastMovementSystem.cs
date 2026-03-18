using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
[BurstCompile]
public partial struct SimpleRaycastMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Margines bezpieczeñstwa, aby gracz nie "dr¿a³" na styku z koliderem
        const float skinWidth = 0.01f;

        foreach (var (input, inventory, trans, props, entity) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRO<PlayerInventory>, RefRW<LocalTransform>, RefRW<PlayerCharacterProperties>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // --- 1. PRÊDKOŒÆ ---
            float currentMoveSpeed = 3f;
            byte activeSlot = inventory.ValueRO.ActiveSlotIndex;

            // Sprawdzamy, czy na aktualnym slocie (1 lub 2) faktycznie mamy broñ (ID > 0)
            bool hasWeaponInActiveSlot = activeSlot switch
            {
                1 => inventory.ValueRO.Slot1_WeaponId > 0,
                2 => inventory.ValueRO.Slot2_WeaponId > 0,
                4 => inventory.ValueRO.Slot4_GrenadeId > 0,
                _ => false
            };

            if (hasWeaponInActiveSlot && input.ValueRO.leftMouseButton == 1)
            {
                currentMoveSpeed = 1.5f;
            }

            // --- 2. GRAWITACJA (RAYCAST) ---
            CollisionFilter groundFilter = new CollisionFilter { BelongsTo = 1u << 0, CollidesWith = 1u << 6, GroupIndex = 0 };
            float3 rayStart = trans.ValueRO.Position + new float3(0, 0.5f, 0);
            float3 rayEnd = trans.ValueRO.Position - new float3(0, props.ValueRO.CharacterHeightOffset + 0.1f, 0);

            if (physicsWorld.CastRay(new RaycastInput { Start = rayStart, End = rayEnd, Filter = groundFilter }, out Unity.Physics.RaycastHit groundHit))
            {
                props.ValueRW.VerticalVelocity = 0;
                trans.ValueRW.Position.y = groundHit.Position.y + props.ValueRO.CharacterHeightOffset;
            }
            else
            {
                props.ValueRW.VerticalVelocity += props.ValueRO.Gravity * deltaTime;
                trans.ValueRW.Position.y += props.ValueRW.VerticalVelocity * deltaTime;
            }

            // --- 3. RUCH POZIOMY I ELIMINACJA DRGAÑ (DEPENETRACJA) ---
            float2 moveInput = new float2(input.ValueRO.Horizontal, input.ValueRO.Vertical);
            if(input.ValueRO.Horizontal!= 0 || input.ValueRO.Vertical != 0)
            {
                TriggerSound(ecb, 2, trans.ValueRO.Position, true);
            }


            if (math.lengthsq(moveInput) > 0.001f)
            {
                float3 moveDir = new float3(moveInput.x, 0, moveInput.y);
                float3 displacement = math.normalize(moveDir) * currentMoveSpeed * deltaTime;

                // 1. Wykonujemy ruch (potencjalnie wchodzimy w œcianê)
                trans.ValueRW.Position += displacement;

                // 2. Filtr œcian
                CollisionFilter wallFilter = new CollisionFilter { BelongsTo = 1u << 0, CollidesWith = (1u << 1) | (1u << 3) | (1u << 4), GroupIndex = 0 };

                // 3. Sprawdzamy, czy jesteœmy wewn¹trz œciany i wypychamy (2 iteracje dla naro¿ników)
                for (int i = 0; i < 2; i++)
                {
                    PointDistanceInput distInput = new PointDistanceInput
                    {
                        Position = trans.ValueRO.Position,
                        MaxDistance = props.ValueRO.Radius,
                        Filter = wallFilter
                    };

                    if (physicsWorld.CalculateDistance(distInput, out DistanceHit hit))
                    {
                        // Jeœli dystans do najbli¿szego punktu jest mniejszy ni¿ nasz promieñ, to znaczy, ¿e przenikamy
                        float penetrationDepth = props.ValueRO.Radius - hit.Distance;
                        if (penetrationDepth > 0)
                        {
                            // Wypychamy gracza w kierunku normalnej powierzchni o g³êbokoœæ przenikania + ma³y margines
                            trans.ValueRW.Position += hit.SurfaceNormal * (penetrationDepth + skinWidth);
                        }
                    }
                }
            }

            // --- 4. ROTACJA ---
            float3 lookTarget = input.ValueRO.MouseWorldPos;
            float3 lookDir = lookTarget - trans.ValueRO.Position;
            lookDir.y = 0;
            if (math.lengthsq(lookDir) > 0.001f)
                trans.ValueRW.Rotation = quaternion.LookRotationSafe(math.normalize(lookDir), math.up());
        }
    }




    public void TriggerSound(EntityCommandBuffer ecb, int id, float3 position, bool isLoop)
    {
        Entity soundEntity = ecb.CreateEntity();
        ecb.AddComponent(soundEntity, new PlaySoundRequest
        {
            SoundID = id,
            Position = position,
            IsLoop = isLoop
        });
    }
}