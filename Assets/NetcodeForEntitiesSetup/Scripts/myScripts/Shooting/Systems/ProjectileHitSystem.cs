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

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // ROZWI¥ZANIE B£ÊDU BC1028: 
        // U¿ywamy EntityQueryBuilder z Allocator.Temp, co jest wspierane przez Burst.
        _targetQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<HealthComponent, LocalTransform>()
            .Build(ref state);

        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        // Pobieramy dane do NativeArray (Allocator.TempJob dla bezpieczeñstwa Jobów)
        var targetEntities = _targetQuery.ToEntityArray(Allocator.TempJob);
        var targetTransforms = _targetQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        var healthLookup = state.GetComponentLookup<HealthComponent>(false);

        var hitJob = new ProjectileHitJob
        {
            TargetEntities = targetEntities,
            TargetTransforms = targetTransforms,
            HealthLookup = healthLookup,
        };

        // Uruchomienie równoleg³e na wielu rdzeniach
        state.Dependency = hitJob.ScheduleParallel(state.Dependency);

        // Zwolnienie tablic dopiero po zakoñczeniu Joba
        targetEntities.Dispose(state.Dependency);
        targetTransforms.Dispose(state.Dependency);
    }
}

[BurstCompile]
public partial struct ProjectileHitJob : IJobEntity
{
    [ReadOnly] public NativeArray<Entity> TargetEntities;
    [ReadOnly] public NativeArray<LocalTransform> TargetTransforms;

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