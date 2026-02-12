/*using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // W Netcode for Entities, dla systemów predykcji, 
        // DeltaTime automatycznie zwraca czas trwania jednego Ticku sieciowego.
        var dt = SystemAPI.Time.DeltaTime;

        // Kluczowe: Pobieramy NetworkTime, aby wiedzieæ, czy symulujemy, czy tylko interpolujemy
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime)) return;

        // Query z Simulate zapewnia, ¿e:
        // 1. Serwer porusza wszystkimi pociskami.
        // 2. Klient porusza tylko swoimi (Predicted) lub tymi, które serwer kaza³ mu symulowaæ.
        foreach (var (projectile, transform) in
                 SystemAPI.Query<RefRO<ProjectileComponent>, RefRW<LocalTransform>>()
                 .WithAll<Simulate>())
        {
            // PROSTA LOGIKA RUCHU
            transform.ValueRW.Position += projectile.ValueRO.Velocity * dt;

            // OPCJONALNIE: Mo¿esz tu dodaæ automatyczne niszczenie po czasie, 
            // u¿ywaj¹c projectile.ValueRO.DeathTime i SystemAPI.Time.ElapsedTime
        }
    }
}*/