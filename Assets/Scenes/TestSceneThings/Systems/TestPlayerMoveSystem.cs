using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct PlayerMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (velocity, input) in
                 SystemAPI.Query<RefRW<PhysicsVelocity>, PlayerMoveInput>().WithAll<TestPlayerTag>())
        {
            Debug.Log("ello");
            // Przenosimy input na wektor 3D (X i Z dla ruchu po ziemi)
            float3 moveDirection = new float3(input.Value.x, 0, input.Value.y);

            // Normalizujemy wektor, ¿eby ruch po skosie nie by³ szybszy
            if (math.lengthsq(moveDirection) > 0)
                moveDirection = math.normalize(moveDirection);

            // Ustawiamy prêdkoœæ liniow¹
            velocity.ValueRW.Linear = moveDirection * input.Speed;

            // Opcjonalnie: blokujemy rotacjê, ¿eby gracz siê nie przewraca³
            velocity.ValueRW.Angular = float3.zero;
        }
    }
}