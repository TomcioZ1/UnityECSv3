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
    private ComponentLookup<HealthComponent> _healthLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Optymalizacja: Cache zapytania o cele
        _targetQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<HealthComponent, LocalTransform>()
            .Build(ref state);

        _healthLookup = state.GetComponentLookup<HealthComponent>(false);
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        // W Netcode obra¿enia rozliczamy tylko w pierwszym ticku predykcji
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        _healthLookup.Update(ref state);

        // U¿ywamy TempJob, bo dane id¹ do ScheduleParallel
        var targetEntities = _targetQuery.ToEntityArray(Allocator.TempJob);
        var targetTransforms = _targetQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        var hitJob = new ProjectileHitJob
        {
            TargetEntities = targetEntities,
            TargetTransforms = targetTransforms,
            HealthLookup = _healthLookup,
            CurrentTime = SystemAPI.Time.ElapsedTime // Przekazujemy aktualny czas
        };

        state.Dependency = hitJob.ScheduleParallel(state.Dependency);

        // Dispose z uwzglêdnieniem zale¿noœci (Dependency)
        targetEntities.Dispose(state.Dependency);
        targetTransforms.Dispose(state.Dependency);
    }
}

[BurstCompile]
public partial struct ProjectileHitJob : IJobEntity
{
    [ReadOnly] public NativeArray<Entity> TargetEntities;
    [ReadOnly] public NativeArray<LocalTransform> TargetTransforms;
    [NativeDisableParallelForRestriction] public ComponentLookup<HealthComponent> HealthLookup;
    public double CurrentTime;

    // U¿ywamy RefRW dla ProjectileComponent, aby oznaczyæ œmieræ pocisku
    public void Execute(Entity entity, RefRW<ProjectileComponent> proj, in LocalTransform trans)
    {
        // Jeœli pocisk ju¿ "nie ¿yje", pomijamy go
        if (proj.ValueRO.DeathTime <= CurrentTime) return;

        float3 projPos = trans.Position;
        Entity owner = proj.ValueRO.Owner;

        for (int i = 0; i < TargetEntities.Length; i++)
        {
            Entity targetEntity = TargetEntities[i];

            // Nie trafiamy samego siebie
            if (targetEntity == owner) continue;

            // Sprawdzanie dystansu (0.2f to bardzo ma³y promieñ, upewnij siê, ¿e pasuje do Twoich modeli)
            if (math.distancesq(projPos, TargetTransforms[i].Position) <= 0.04f) // 0.2 * 0.2 = 0.04
            {
                if (HealthLookup.HasComponent(targetEntity))
                {
                    var health = HealthLookup[targetEntity];
                    health.HealthPoints -= proj.ValueRO.Damage;
                    health.LastHitBy = owner;
                    HealthLookup[targetEntity] = health;
                }

                // Oznaczamy pocisk jako "do zniszczenia" poprzez ustawienie czasu œmierci na teraz
                proj.ValueRW.DeathTime = CurrentTime;

                // Przerywamy pêtlê - pocisk trafia tylko w jeden cel
                break;
            }
        }
    }
}