using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(HandsSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct PunchHitSystem : ISystem
{
    // 1. Deklarujemy pola Lookup i Query jako pola struktury
    private ComponentLookup<HealthComponent> _healthLookup;
    private EntityQuery _targetsQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 2. Inicjalizujemy Lookup (false = chcemy zapisywaæ punkty ¿ycia)
        _healthLookup = state.GetComponentLookup<HealthComponent>(false);

        // 3. Budujemy zapytanie raz w OnCreate
        _targetsQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<HealthComponent, LocalToWorld>()
            .Build(ref state);

        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 4. KLUCZOWE: Aktualizujemy stan Lookup na pocz¹tku klatki
        _healthLookup.Update(ref state);

        // U¿ywamy Allocator.Temp dla tablic, które ¿yj¹ tylko w jednej klatce
        var allTargets = _targetsQuery.ToEntityArray(Allocator.Temp);
        var allTargetTransforms = _targetsQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);

        if (allTargets.Length <= 1) return;

        foreach (var (anim, ltw, playerEntity) in
                 SystemAPI.Query<RefRW<HandAttackData>, RefRO<LocalToWorld>>()
                 .WithEntityAccess())
        {
            if (anim.ValueRO.IsAttacking && anim.ValueRO.AttackProgress >= 0.6f && !anim.ValueRO.HasAppliedDamage)
            {
                float3 playerPos = ltw.ValueRO.Position;
                float3 forward = ltw.ValueRO.Forward;

                float maxRange = 0.8f;
                float hitAngleThreshold = 0.4f;
                bool hitFound = false;

                for (int i = 0; i < allTargets.Length; i++)
                {
                    Entity targetEntity = allTargets[i];

                    if (targetEntity == playerEntity) continue;

                    float3 targetPos = allTargetTransforms[i].Position;
                    float distSq = math.distancesq(playerPos.xz, targetPos.xz);

                    if (distSq <= (maxRange * maxRange))
                    {
                        float3 toTarget = math.normalize(new float3(targetPos.x - playerPos.x, 0, targetPos.z - playerPos.z));
                        float dot = math.dot(forward.xz, toTarget.xz);

                        if (dot > hitAngleThreshold)
                        {
                            // U¿ywamy zaktualizowanego pola _healthLookup
                            var hp = _healthLookup[targetEntity];
                            hp.HealthPoints -= anim.ValueRO.AttackDamage;
                            hp.LastHitBy = playerEntity;

                            _healthLookup[targetEntity] = hp;

                            anim.ValueRW.HasAppliedDamage = true;
                            hitFound = true;

                            //Debug.Log($"<color=red>PUNCH HIT!</color> {playerEntity.Index} hit {targetEntity.Index}. HP left: {hp.HealthPoints}");
                            break;
                        }
                    }
                }

                if (!hitFound)
                {
                    anim.ValueRW.HasAppliedDamage = true;
                }
            }
            else if (!anim.ValueRO.IsAttacking)
            {
                anim.ValueRW.HasAppliedDamage = false;
            }
        }
    }
}