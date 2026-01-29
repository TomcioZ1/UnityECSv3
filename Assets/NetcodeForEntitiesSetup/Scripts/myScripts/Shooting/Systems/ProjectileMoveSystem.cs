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

        // Zmieniliœmy RefRW na RefRO dla ProjectileComponent, 
        // poniewa¿ ju¿ go nie modyfikujemy w tej pêtli (tylko odczytujemy Velocity).
        foreach (var (projectile, transform) in
                 SystemAPI.Query<RefRO<ProjectileComponent>, RefRW<LocalTransform>>()
                 .WithAll<Simulate>())
        {
            // 1. AKTUALIZACJA POZYCJI
            // Przesuwamy pocisk o wektor prêdkoœci. 
            // Poniewa¿ Velocity jest sta³e, a zmiana pozycji LocalTransform 
            // jest przewidywana przez Netcode, ruch bêdzie p³ynny.
            transform.ValueRW.Position += projectile.ValueRO.Velocity * dt;

            // 2. USUNIÊTO AKTUALIZACJÊ LIFETIME
            // Czas ¿ycia (DeathTime) jest sprawdzany w ProjectileDestroySystem,
            // który porównuje go z aktualnym czasem ElapsedTime.
        }
    }
}