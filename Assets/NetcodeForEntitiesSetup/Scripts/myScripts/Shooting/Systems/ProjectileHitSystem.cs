using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.NetCode;
using Unity;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ProjectileHitSystem : ISystem
{
    private ComponentLookup<HealthComponent> _healthLookup;
    private ComponentLookup<ProjectileComponent> _projectileLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _healthLookup = state.GetComponentLookup<HealthComponent>(false);
        _projectileLookup = state.GetComponentLookup<ProjectileComponent>(false);

        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _healthLookup.Update(ref state);
        _projectileLookup.Update(ref state);

        var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        // Uruchamiamy Job obs³uguj¹cy triggery fizyki
        state.Dependency = new ProjectileTriggerJob
        {
            HealthLookup = _healthLookup,
            ProjectileLookup = _projectileLookup,
            CurrentTime = SystemAPI.Time.ElapsedTime
        }.Schedule(simulation, state.Dependency);
    }
}

[BurstCompile]
struct ProjectileTriggerJob : ITriggerEventsJob
{
    public ComponentLookup<HealthComponent> HealthLookup;
    public ComponentLookup<ProjectileComponent> ProjectileLookup;
    public double CurrentTime;

    public void Execute(TriggerEvent triggerEvent)
    {
        ProcessCollision(triggerEvent.EntityA, triggerEvent.EntityB);
        ProcessCollision(triggerEvent.EntityB, triggerEvent.EntityA);
    }

    private void ProcessCollision(Entity projectileEntity, Entity targetEntity)
    {
        // 1. Sprawdzamy czy pierwszy obiekt to pocisk, a drugi ma ¿ycie
        if (ProjectileLookup.HasComponent(projectileEntity) && HealthLookup.HasComponent(targetEntity))
        {
            var proj = ProjectileLookup[projectileEntity];

            // 2. Jeœli pocisk ju¿ zosta³ oznaczony jako martwy (np. trafi³ coœ w tej samej klatce), pomijamy
            if (proj.DeathTime <= CurrentTime) return;

            // 3. Nie trafiamy w³aœciciela pocisku
            if (targetEntity == proj.Owner) return;

            // 4. Zadajemy obra¿enia
            var health = HealthLookup[targetEntity];
            health.HealthPoints -= proj.Damage;
            health.LastHitBy = proj.Owner;
            HealthLookup[targetEntity] = health;

            // 5. "Niszczymy" pocisk (ustawiamy DeathTime na teraz)
            // System niszcz¹cy pociski (np. ProjectileDeathSystem) usunie encjê na podstawie tego czasu
            proj.DeathTime = CurrentTime;
            ProjectileLookup[projectileEntity] = proj;

            // Log serwerowy dla testów
            //Unity.Debug.Log($"[PROJECTILE] Serwer: Pocisk {projectileEntity.Index} trafil {targetEntity.Index}. HP: {health.HealthPoints}");
        }
    }
}