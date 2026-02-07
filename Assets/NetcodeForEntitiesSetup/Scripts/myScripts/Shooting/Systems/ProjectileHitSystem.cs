using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ProjectileHitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete(); // naprawia blad rece i pociski chca zmienic cos w tym samym czasie

        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var healthLookup = SystemAPI.GetComponentLookup<HealthComponent>(false);

        var currentTime = SystemAPI.Time.ElapsedTime;
        var dt = SystemAPI.Time.DeltaTime;

        foreach (var (proj, transform, entity) in
                 SystemAPI.Query<RefRW<ProjectileComponent>, RefRW<LocalTransform>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            if (proj.ValueRO.DeathTime <= currentTime) continue;

            float3 start = transform.ValueRO.Position;
            float3 end = start + (proj.ValueRO.Velocity * dt);

            // KONFIGURACJA FILTRA DLA TWOICH KATEGORII:
            // BelongsTo: Pocisk (4)
            // CollidesWith: Player (0) ORAZ Rzecz (3)
            var rayInput = new RaycastInput
            {
                Start = start,
                End = end,
                Filter = new CollisionFilter
                {
                    BelongsTo = 1u << 4,
                    CollidesWith = (1u << 0) | (1u << 3),
                    GroupIndex = 0
                }
            };

            if (collisionWorld.CastRay(rayInput, out var hit))
            {
                // Ignoruj, jeœli promieñ trafi³ w gracza, który go wystrzeli³
                if (hit.Entity == proj.ValueRO.Owner)
                {
                    transform.ValueRW.Position = end;
                    continue;
                }

                // Logika obra¿eñ
                if (healthLookup.HasComponent(hit.Entity))
                {
                    var health = healthLookup[hit.Entity];
                    health.HealthPoints -= proj.ValueRO.Damage;
                    health.LastHitBy = proj.ValueRO.Owner;
                    healthLookup[hit.Entity] = health;
                }

                // Zatrzymanie pocisku na przeszkodzie
                proj.ValueRW.DeathTime = currentTime;
                transform.ValueRW.Position = hit.Position;
            }
            else
            {
                // Brak przeszkód - swobodny lot
                transform.ValueRW.Position = end;
            }
        }
    }
}