using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileHitSystem : ISystem
{
    private EntityQuery _targetQuery;
    // 1. Deklarujemy Lookup jako pole struktury
    private ComponentLookup<HealthComponent> _healthLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _targetQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<HealthComponent, LocalTransform>()
            .Build(ref state);

        // 2. Inicjalizujemy Lookup w OnCreate
        _healthLookup = state.GetComponentLookup<HealthComponent>(false);

        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        // 3. KLUCZOWE: Aktualizujemy stan Lookup na pocz¹tku OnUpdate
        _healthLookup.Update(ref state);

        var targetEntities = _targetQuery.ToEntityArray(Allocator.TempJob);
        var targetTransforms = _targetQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        var hitJob = new ProjectileHitJob
        {
            TargetEntities = targetEntities,
            TargetTransforms = targetTransforms,
            // Przekazujemy zaktualizowany lookup do Joba
            HealthLookup = _healthLookup,
        };

        state.Dependency = hitJob.ScheduleParallel(state.Dependency);

        targetEntities.Dispose(state.Dependency);
        targetTransforms.Dispose(state.Dependency);
    }
}

[BurstCompile]
public partial struct ProjectileHitJob : IJobEntity
{
    [ReadOnly] public NativeArray<Entity> TargetEntities;
    [ReadOnly] public NativeArray<LocalTransform> TargetTransforms;

    // To pozostaje bez zmian - pozwala na zapis w Jobie równoleg³ym
    [NativeDisableParallelForRestriction]
    public ComponentLookup<HealthComponent> HealthLookup;

    public void Execute(RefRW<ProjectileComponent> proj, in LocalTransform trans)
    {
        if (proj.ValueRO.Lifetime <= 0) return;

        float3 projPos = trans.Position;
        Entity owner = proj.ValueRO.Owner;

        for (int i = 0; i < TargetEntities.Length; i++)
        {
            Entity targetEntity = TargetEntities[i];
            if (targetEntity == owner) continue;

            if (math.distancesq(projPos, TargetTransforms[i].Position) <= 0.2f)
            {
                if (HealthLookup.HasComponent(targetEntity))
                {
                    var health = HealthLookup[targetEntity];
                    health.HealthPoints -= proj.ValueRO.Damage;
                    health.LastHitBy = owner;
                    HealthLookup[targetEntity] = health;
                }

                proj.ValueRW.Lifetime = 0;
                break;
            }
        }
    }
}