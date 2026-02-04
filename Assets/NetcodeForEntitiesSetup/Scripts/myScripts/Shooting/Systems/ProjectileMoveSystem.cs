using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))] // ZMIANA: Z PredictedSimulation na FixedStep
[BurstCompile]
public partial struct ProjectileMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // W FixedStep u¿ywamy FixedDeltaTime dla maksymalnej precyzji sieciowej
        var dt = SystemAPI.Time.DeltaTime;

        foreach (var (projectile, transform) in
                 SystemAPI.Query<RefRO<ProjectileComponent>, RefRW<LocalTransform>>()
                 .WithAll<Simulate>())
        {
            // Tutaj projectile.ValueRO.Velocity zawiera ju¿ (kierunek * speed) + playerVel
            transform.ValueRW.Position += projectile.ValueRO.Velocity * dt;
        }
    }
}