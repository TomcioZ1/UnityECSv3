using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct HandsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime)) return;

        // U¿ywamy IsFinalPredictionTick, aby unikn¹æ jittera z rollbacków
        if (!networkTime.IsFinalPredictionTick) return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        float dt = SystemAPI.Time.DeltaTime;
        float attackSpeed = 4f;
        float punchDistance = 0.25f;

        // UWAGA: transformLookup nie mo¿e byæ ReadOnly (true), bo piszemy bezpoœrednio
        var transformLookup = state.GetComponentLookup<LocalTransform>(false);

        // 1. LOGIKA ATAKU
        foreach (var (input, anim) in SystemAPI.Query<RefRO<Unity.Multiplayer.Center.NetcodeForEntitiesSetup.MyPlayerInput>, RefRW<HandAttackData>>().WithAll<Simulate>())
        {
            if (input.ValueRO.choosenWeapon == 3 && input.ValueRO.leftMouseButton == 1 && !anim.ValueRO.IsAttacking)
            {
                anim.ValueRW.IsAttacking = true;
                anim.ValueRW.AttackProgress = 0f;
            }

            if (anim.ValueRO.IsAttacking)
            {
                anim.ValueRW.AttackProgress += dt * attackSpeed;
                if (anim.ValueRO.AttackProgress >= 1f)
                {
                    anim.ValueRW.IsAttacking = false;
                    anim.ValueRW.AttackProgress = 0f;
                    anim.ValueRW.AttackIsLeft = !anim.ValueRO.AttackIsLeft;
                }
            }
        }

        // 2. WIZUALIZACJA (Bezpoœredni zapis do Transformu)
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

            // Przekazujemy transformLookup, aby pisaæ bezpoœrednio (p³ynniej ni¿ ECB)
            UpdateHand(ref state, ecb, ref transformLookup, activeHands.ValueRO.LeftHandEntity, socket.LeftHandSocket, leftOffset, ref activeHands.ValueRW.PrevLeftHand);
            UpdateHand(ref state, ecb, ref transformLookup, activeHands.ValueRO.RightHandEntity, socket.RightHandSocket, rightOffset, ref activeHands.ValueRW.PrevRightHand);
        }
    }

    [BurstCompile]
    private void UpdateHand(ref SystemState state, EntityCommandBuffer ecb, ref ComponentLookup<LocalTransform> transformLookup, Entity hand, Entity socket, float3 offset, ref Entity prevHand)
    {
        if (hand == Entity.Null || !state.EntityManager.Exists(hand) || !transformLookup.HasComponent(hand)) return;

        // Parenting musi zostaæ w ECB (Structural Change)
        if (hand != prevHand)
        {
            if (state.EntityManager.Exists(socket))
            {
                ecb.AddComponent(hand, new Parent { Value = socket });
                prevHand = hand;
            }
        }

        // Pisanie bezpoœrednie (Direct Write) eliminuje 1 klatkê opóŸnienia z ECB
        var lt = transformLookup[hand];
        lt.Position = offset;
        transformLookup[hand] = lt;
    }
}