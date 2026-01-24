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
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Pobieramy lookup komponentów (z mo¿liwoœci¹ zapisu dla HealthComponent)
        var healthLookup = state.GetComponentLookup<HealthComponent>(false);

        // 2. Budujemy zapytanie o cele
        var targetsQuery = SystemAPI.QueryBuilder()
            .WithAll<HealthComponent, LocalToWorld>()
            .Build();

        // 3. Pobieramy dane celów
        var allTargets = targetsQuery.ToEntityArray(Allocator.Temp);
        var allTargetTransforms = targetsQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);

        if (allTargets.Length <= 1) return;

        // 4. G³ówna pêtla atakuj¹cych
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
                            // --- LOGIKA TRAFIENIA Z LAST HIT BY ---
                            var hp = healthLookup[targetEntity];
                            hp.HealthPoints -= anim.ValueRO.AttackDamage;

                            // PRZYPISANIE ZABÓJCY:
                            hp.LastHitBy = playerEntity;

                            healthLookup[targetEntity] = hp;

                            anim.ValueRW.HasAppliedDamage = true;
                            hitFound = true;

                            Debug.Log($"<color=red>PUNCH HIT!</color> {playerEntity.Index} hit {targetEntity.Index}. HP left: {hp.HealthPoints}");
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