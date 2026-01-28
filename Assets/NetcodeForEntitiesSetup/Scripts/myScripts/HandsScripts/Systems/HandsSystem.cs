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
    // 1. Pole struktury dla Lookup
    private ComponentLookup<LocalTransform> _transformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 2. Inicjalizacja (false, bo piszemy do transformu)
        _transformLookup = state.GetComponentLookup<LocalTransform>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime)) return;

        if (!networkTime.IsFinalPredictionTick) return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 3. Aktualizacja Lookup na pocz¹tku klatki
        _transformLookup.Update(ref state);

        float dt = SystemAPI.Time.DeltaTime;
        float attackSpeed = 4f;
        float punchDistance = 0.25f;

        // 1. LOGIKA ATAKU
        // Dodaj PlayerInventory do zapytania
        foreach (var (input, anim, inventory) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRW<HandAttackData>, RefRO<PlayerInventory>>()
                 .WithAll<Simulate>())
        {
            // 1. Sprawdzamy, czy na wybranym slocie jest jakakolwiek broñ
            byte activeSlot = inventory.ValueRO.ActiveSlotIndex;
            bool hasWeaponInSlot = activeSlot switch
            {
                1 => inventory.ValueRO.Slot1_WeaponId > 0,
                2 => inventory.ValueRO.Slot2_WeaponId > 0,
                3 => inventory.ValueRO.Slot3_HandsId > 0, // Zazwyczaj rêce maj¹ ID 0 lub 3, sprawdŸ swoje ID
                4 => inventory.ValueRO.Slot4_GrenadeId > 0,
                _ => false
            };

            // 2. Warunek ataku: 
            // Lewy przycisk myszy ORAZ (Slot z rêkami LUB pusty slot broni)
            bool canPunch = input.ValueRO.leftMouseButton == 1 && !hasWeaponInSlot;

            if (canPunch && !anim.ValueRO.IsAttacking)
            {
                anim.ValueRW.IsAttacking = true;
                anim.ValueRW.AttackProgress = 0f;
            }

            // Reszta logiki animacji pozostaje bez zmian
            if (anim.ValueRO.IsAttacking)
            {
                anim.ValueRW.AttackProgress += SystemAPI.Time.DeltaTime * attackSpeed;
                if (anim.ValueRO.AttackProgress >= 1f)
                {
                    anim.ValueRW.IsAttacking = false;
                    anim.ValueRW.AttackProgress = 0f;
                    anim.ValueRW.AttackIsLeft = !anim.ValueRO.AttackIsLeft;
                }
            }
        }

        // 2. WIZUALIZACJA
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

            // Przekazujemy referencje do pól struktury
            UpdateHand(ecb, activeHands.ValueRO.LeftHandEntity, socket.LeftHandSocket, leftOffset, ref activeHands.ValueRW.PrevLeftHand);
            UpdateHand(ecb, activeHands.ValueRO.RightHandEntity, socket.RightHandSocket, rightOffset, ref activeHands.ValueRW.PrevRightHand);
        }
    }

    [BurstCompile]
    private void UpdateHand(EntityCommandBuffer ecb, Entity hand, Entity socket, float3 offset, ref Entity prevHand)
    {
        // U¿ywamy zaktualizowanego _transformLookup zamiast state.EntityManager
        if (hand == Entity.Null || !_transformLookup.HasComponent(hand)) return;

        if (hand != prevHand)
        {
            // Parenting w ECB
            ecb.AddComponent(hand, new Parent { Value = socket });
            prevHand = hand;
        }

        // Bezpoœredni zapis do transformu
        var lt = _transformLookup[hand];
        lt.Position = offset;
        _transformLookup[hand] = lt;
    }
}