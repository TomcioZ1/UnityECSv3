using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;

        // Query szuka wszystkich encji, które maj¹ ProjectileComponent i LocalTransform
        // WithAll<Simulate> jest kluczowe dla poprawnego dzia³ania w Netcode
        foreach (var (projectile, transform) in
                 SystemAPI.Query<RefRW<ProjectileComponent>, RefRW<LocalTransform>>()
                 .WithAll<Simulate>())
        {
            // 1. AKTUALIZACJA POZYCJI
            // Przesuwamy pocisk o wektor prêdkoœci pomno¿ony przez czas
            transform.ValueRW.Position += projectile.ValueRO.Velocity * dt;

            // 2. AKTUALIZACJA CZASU ¯YCIA
            // Zmniejszamy Lifetime
            projectile.ValueRW.Lifetime -= dt;
        }
    }
}