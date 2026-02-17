using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct HandsSystem : ISystem
{
    private ComponentLookup<LocalTransform> _transformLookup;
    // Dodajemy lookup dla Parent, aby bezpiecznie sprawdzaæ jego istnienie
    private ComponentLookup<Parent> _parentLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _transformLookup = state.GetComponentLookup<LocalTransform>(false);
        _parentLookup = state.GetComponentLookup<Parent>(true); // ReadOnly
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime)) return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Odœwie¿amy lookupy
        _transformLookup.Update(ref state);
        _parentLookup.Update(ref state);

        const float attackSpeed = 3f;
        const float punchDistance = 0.25f;
        bool isClient = state.WorldUnmanaged.IsClient();

        // --- 1. LOGIKA SYMULACJI (Tick-based) ---
        if (networkTime.IsFirstTimeFullyPredictingTick)
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (input, anim, inventory, transform) in
                     SystemAPI.Query<RefRO<MyPlayerInput>, RefRW<HandAttackData>, RefRO<PlayerInventory>, RefRO<LocalTransform>>()
                     .WithAll<Simulate>())
            {
                bool hasWeapon = CheckIfHasWeapon(inventory.ValueRO);
                bool canPunch = input.ValueRO.leftMouseButton == 1 && !hasWeapon;

                if (canPunch && !anim.ValueRO.IsAttacking)
                {
                    anim.ValueRW.IsAttacking = true;
                    anim.ValueRW.AttackProgress = 0f;
                    anim.ValueRW.HasAppliedDamage = false;
                    if (isClient)
                    {
                        TriggerSound(ecb, 4, transform.ValueRO.Position, false);
                    }
                }

                if (anim.ValueRO.IsAttacking)
                {
                    anim.ValueRW.AttackProgress += dt * attackSpeed;

                    if (anim.ValueRO.AttackProgress >= 1f)
                    {
                        anim.ValueRW.IsAttacking = false;
                        anim.ValueRW.AttackProgress = 0f;
                        anim.ValueRW.AttackIsLeft = !anim.ValueRO.AttackIsLeft;
                        anim.ValueRW.HasAppliedDamage = false;
                    }
                }
            }
        }

        // --- 2. WIZUALIZACJA (P³ynna) ---
        foreach (var (anim, socket, activeHands) in
                 SystemAPI.Query<RefRO<HandAttackData>, HandsSocket, RefRW<ActiveHands>>())
        {
            float3 leftOffset = float3.zero;
            float3 rightOffset = float3.zero;

            if (anim.ValueRO.IsAttacking)
            {
                float progress = math.saturate(anim.ValueRO.AttackProgress);
                float punchEffect = math.sin(math.PI * progress);
                float fwd = punchEffect * punchDistance;
                float side = punchEffect * (punchDistance * 0.3f);

                if (anim.ValueRO.AttackIsLeft)
                    leftOffset = new float3(side, 0, fwd);
                else
                    rightOffset = new float3(-side, 0, fwd);
            }

            UpdateHand(ecb, activeHands.ValueRO.LeftHandEntity, socket.LeftHandSocket, leftOffset, ref activeHands.ValueRW.PrevLeftHand);
            UpdateHand(ecb, activeHands.ValueRO.RightHandEntity, socket.RightHandSocket, rightOffset, ref activeHands.ValueRW.PrevRightHand);
        }
    }

    [BurstCompile]
    private void UpdateHand(EntityCommandBuffer ecb, Entity hand, Entity socket, float3 offset, ref Entity prevHand)
    {
        // Sprawdzamy czy encja istnieje i ma transform za pomoc¹ lookupa
        if (hand == Entity.Null || !_transformLookup.HasComponent(hand)) return;

        if (hand != prevHand)
        {
            // U¿ywamy lookupa do sprawdzenia czy ma komponent Parent
            if (_parentLookup.HasComponent(hand))
                ecb.SetComponent(hand, new Parent { Value = socket });
            else
                ecb.AddComponent(hand, new Parent { Value = socket });

            prevHand = hand;
        }

        var lt = _transformLookup[hand];
        lt.Position = offset;
        _transformLookup[hand] = lt;
    }

    private static bool CheckIfHasWeapon(PlayerInventory inv)
    {
        return inv.ActiveSlotIndex switch
        {
            1 => inv.Slot1_WeaponId > 0,
            2 => inv.Slot2_WeaponId > 0,
            3 => false,
            4 => inv.Slot4_GrenadeId > 0,
            _ => false
        };
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