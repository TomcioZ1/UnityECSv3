using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct HandsSystem : ISystem
{
    private ComponentLookup<LocalTransform> _transformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _transformLookup = state.GetComponentLookup<LocalTransform>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime)) return;
        if (!networkTime.IsFinalPredictionTick) return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        _transformLookup.Update(ref state);

        float dt = SystemAPI.Time.DeltaTime;
        float attackSpeed = 4f;
        float punchDistance = 0.25f;

        // --- 1. LOGIKA ATAKU I RESETU ---
        foreach (var (input, anim, inventory) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRW<HandAttackData>, RefRO<PlayerInventory>>()
                 .WithAll<Simulate>())
        {
            byte activeSlot = inventory.ValueRO.ActiveSlotIndex;
            bool hasWeaponInSlot = activeSlot switch
            {
                1 => inventory.ValueRO.Slot1_WeaponId > 0,
                2 => inventory.ValueRO.Slot2_WeaponId > 0,
                3 => inventory.ValueRO.Slot3_HandsId > 0,
                4 => inventory.ValueRO.Slot4_GrenadeId > 0,
                _ => false
            };

            bool canPunch = input.ValueRO.leftMouseButton == 1 && !hasWeaponInSlot;

            // Start nowego ataku
            if (canPunch && !anim.ValueRO.IsAttacking)
            {
                anim.ValueRW.IsAttacking = true;
                anim.ValueRW.AttackProgress = 0f;
                anim.ValueRW.HasAppliedDamage = false; // RESET FLAGI OBRA¯EÑ
            }

            if (anim.ValueRO.IsAttacking)
            {
                anim.ValueRW.AttackProgress += dt * attackSpeed;

                // Koniec ataku
                if (anim.ValueRO.AttackProgress >= 1f)
                {
                    anim.ValueRW.IsAttacking = false;
                    anim.ValueRW.AttackProgress = 0f;
                    anim.ValueRW.AttackIsLeft = !anim.ValueRO.AttackIsLeft;
                    anim.ValueRW.HasAppliedDamage = false; // RESET FLAGI DLA PEWNOŒCI
                }
            }
        }

        // --- 2. WIZUALIZACJA ---
        foreach (var (anim, socket, activeHands) in
                 SystemAPI.Query<RefRO<HandAttackData>, HandsSocket, RefRW<ActiveHands>>())
        {
            float3 leftOffset = float3.zero;
            float3 rightOffset = float3.zero;

            if (anim.ValueRO.IsAttacking)
            {
                float punchEffect = math.sin(math.PI * math.saturate(anim.ValueRO.AttackProgress));
                float fwd = punchEffect * punchDistance;
                float side = punchEffect * (punchDistance * 0.4f);

                if (anim.ValueRO.AttackIsLeft) leftOffset = new float3(side, 0, fwd);
                else rightOffset = new float3(-side, 0, fwd);
            }

            UpdateHand(ecb, activeHands.ValueRO.LeftHandEntity, socket.LeftHandSocket, leftOffset, ref activeHands.ValueRW.PrevLeftHand);
            UpdateHand(ecb, activeHands.ValueRO.RightHandEntity, socket.RightHandSocket, rightOffset, ref activeHands.ValueRW.PrevRightHand);
        }
    }

    [BurstCompile]
    private void UpdateHand(EntityCommandBuffer ecb, Entity hand, Entity socket, float3 offset, ref Entity prevHand)
    {
        if (hand == Entity.Null || !_transformLookup.HasComponent(hand)) return;

        if (hand != prevHand)
        {
            ecb.AddComponent(hand, new Parent { Value = socket });
            prevHand = hand;
        }

        var lt = _transformLookup[hand];
        lt.Position = offset;
        _transformLookup[hand] = lt;
    }
}