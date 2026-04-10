using Unity.Burst;
using Unity.Entities;
using Unity.Transforms; // Wymagane dla LocalTransform

// Zak³adam, ¿e Twój HealthComponent wygl¹da mniej wiêcej tak:
// public struct HealthComponent : IComponentData { public float Value; }

[BurstCompile]
partial struct PlayerVoidDeathSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // U¿ywamy EntityQuery, aby znaleŸæ graczy z komponentami Health i Transform
        // Wykonuje siê to w bezpieczny sposób dziêki Burst
        foreach (var (transform, health) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthComponent>>())
        {
            // Sprawdzamy zakres Y
            if (transform.ValueRO.Position.y <= 3f && transform.ValueRO.Position.y >= 0f)
            {
                // Ustawiamy zdrowie na 0
                health.ValueRW.HealthPoints = 0;
            }
        }
    }

    
}