/*using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Physics;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ProjectileSpawnSystem))]
[BurstCompile]
public partial struct ProjectileSimulationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var healthLookup = SystemAPI.GetComponentLookup<HealthComponent>(false);
        var dt = SystemAPI.Time.DeltaTime;
        var currentTime = SystemAPI.Time.ElapsedTime;

        foreach (var (proj, transform) in
                 SystemAPI.Query<RefRW<ProjectileComponent>, RefRW<LocalTransform>>()
                 .WithAll<Simulate>())
        {

            // Pomiñ pociski, które ju¿ powinny znikn¹æ
            if (proj.ValueRO.DeathTime <= currentTime) continue;

            float3 start = transform.ValueRO.Position;
            float3 end = start + (proj.ValueRO.Velocity * dt);


            var rayInput = new RaycastInput
            {
                Start = start,
                End = end,
                Filter = new CollisionFilter
                {
                    BelongsTo = 1u << 4, // Pocisk
                    CollidesWith = (1u << 0) | (1u << 3), // Gracz + Rzecz
                    GroupIndex = 0
                }
            };

            //if (currentTime - proj.ValueRO.SpawnTime < proj.ValueRO.TimeOffset) continue;

            if (collisionWorld.CastRay(rayInput, out var hit))
            {
                if (hit.Entity == proj.ValueRO.Owner)
                {
                    transform.ValueRW.Position = end;
                    continue;
                }


                // TRAFIENIE
                transform.ValueRW.Position = hit.Position;
                proj.ValueRW.DeathTime = (float)currentTime; // Sygna³ dla DestroySystem

                // Obra¿enia tylko na Serwerze
                if (state.WorldUnmanaged.IsServer() && healthLookup.HasComponent(hit.Entity))
                {
                    var health = healthLookup[hit.Entity];
                    health.HealthPoints -= proj.ValueRO.Damage;
                    health.LastHitBy = proj.ValueRO.Owner;
                    healthLookup[hit.Entity] = health;
                }
            }
            else
            {
                // Zwyk³y ruch, gdy brak kolizji
                transform.ValueRW.Position = end;
            }
        }
    }
}*/