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
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        float dt = SystemAPI.Time.DeltaTime;
        float attackSpeed = 3f; // Trochê szybciej dla lepszego feelingu ciosów na zmianê
        float punchDistance = 0.2f;

        var transformLookup = state.GetComponentLookup<LocalTransform>(true);

        // --- 1. LOGIKA ATAKU (Tylko dla tych z Inputem) ---
        foreach (var (input, anim) in SystemAPI.Query<RefRO<Unity.Multiplayer.Center.NetcodeForEntitiesSetup.MyPlayerInput>, RefRW<HandAttackData>>().WithAll<Simulate>())
        {
            // Atakuj tylko jeœli wybrano rêce (3)
            if (input.ValueRO.choosenWeapon==3 && input.ValueRO.leftMouseButton == 1 && !anim.ValueRO.IsAttacking)
            {
                anim.ValueRW.IsAttacking = true;
                anim.ValueRW.AttackProgress = 0f;
                // Rêka zostanie prze³¹czona na koñcu ciosu (lub tutaj, jeœli wolisz)
            }

            if (anim.ValueRO.IsAttacking)
            {
                anim.ValueRW.AttackProgress += dt * attackSpeed;

                if (anim.ValueRO.AttackProgress >= 1f)
                {
                    anim.ValueRW.IsAttacking = false;
                    anim.ValueRW.AttackProgress = 0f;
                    // PRZE£¥CZAMY RÊKÊ na przeciwn¹ po zakoñczeniu ciosu
                    anim.ValueRW.AttackIsLeft = !anim.ValueRO.AttackIsLeft;
                }
            }
        }

        // --- 2. WIZUALIZACJA (Dla wszystkich Ghostów) ---
        // --- 2. WIZUALIZACJA (Dla wszystkich Ghostów) ---
        foreach (var (anim, socket, activeHands) in
                 SystemAPI.Query<RefRO<HandAttackData>, HandsSocket, RefRW<ActiveHands>>())
        {
            float3 leftOffset = float3.zero;
            float3 rightOffset = float3.zero;

            if (anim.ValueRO.IsAttacking)
            {
                float punchEffect = math.sin(math.PI * math.saturate(anim.ValueRO.AttackProgress));

                // G³ówne uderzenie do przodu (Oœ Z)
                float forwardMove = punchEffect * punchDistance;

                // Ruch do œrodka (Oœ X)
                // punchDistance * 0.3f sprawi, ¿e rêka zbli¿y siê do œrodka o 30% zasiêgu ciosu
                float inwardMove = punchEffect * (punchDistance * 0.4f);

                if (anim.ValueRO.AttackIsLeft)
                {
                    // Lewa rêka idzie DO PRZODU i W PRAWO (dodatnie X)
                    leftOffset = new float3(inwardMove, 0, forwardMove);
                }
                else
                {
                    // Prawa rêka idzie DO PRZODU i W LEWO (ujemne X)
                    rightOffset = new float3(-inwardMove, 0, forwardMove);
                }
            }

            UpdateHand(ref state, ecb, transformLookup, activeHands.ValueRO.LeftHandEntity, socket.LeftHandSocket, leftOffset, ref activeHands.ValueRW.PrevLeftHand);
            UpdateHand(ref state, ecb, transformLookup, activeHands.ValueRO.RightHandEntity, socket.RightHandSocket, rightOffset, ref activeHands.ValueRW.PrevRightHand);
        }
    }

    [BurstCompile]
    private void UpdateHand(ref SystemState state, EntityCommandBuffer ecb, ComponentLookup<LocalTransform> transformLookup, Entity hand, Entity socket, float3 offset, ref Entity prevHand)
    {
        // 1. Sprawdzamy, czy encja rêki W OGÓLE istnieje w managerze
        if (hand == Entity.Null || !state.EntityManager.Exists(hand))
        {
            return;
        }

        // 2. Sprawdzamy, czy transformacja d³oni jest dostêpna.
        // Jeœli gracz zgin¹³, komponenty mog¹ byæ ju¿ usuwane.
        if (!transformLookup.HasComponent(hand))
        {
            return;
        }

        // 3. Sprawdzamy, czy RODZIC (socket/gracz) nadal istnieje.
        // Jeœli gracz zosta³ zniszczony, nie chcemy podpinaæ r¹k do "ducha".
        if (!state.EntityManager.Exists(socket))
        {
            return;
        }

        // Obs³uga parentingu przy zmianie broni/r¹k
        if (hand != prevHand)
        {
            ecb.AddComponent(hand, new Parent { Value = socket });
            prevHand = hand;
        }

        // Pobieramy aktualn¹ transformacjê (skalê/rotacjê)
        var currentTransform = transformLookup[hand];
        currentTransform.Position = offset;

        // U¿ywamy AddComponent zamiast SetComponent - to jest "bezpieczny zapis"
        ecb.AddComponent(hand, currentTransform);
    }
}

