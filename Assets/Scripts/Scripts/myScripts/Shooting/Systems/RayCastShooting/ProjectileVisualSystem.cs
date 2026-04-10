/*using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct ProjectileVisualSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        float deltaTime = SystemAPI.Time.DeltaTime;
        double elapsedTime = SystemAPI.Time.ElapsedTime;

        // Sprawdzamy, czy jesteœmy na serwerze
        bool isServer = state.WorldUnmanaged.IsServer();

        foreach (var (transform, proj, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<ProjectileComponent>>()
                 .WithEntityAccess())
        {
            float3 currentPos = transform.ValueRO.Position;
            float3 targetPos = proj.ValueRO.TargetPosition;
            float3 nextPos = currentPos + (proj.ValueRO.Velocity * deltaTime);

            float distToTargetSq = math.distancesq(currentPos, targetPos);
            float moveStepSq = math.lengthsq(proj.ValueRO.Velocity * deltaTime);

            if (moveStepSq >= distToTargetSq || elapsedTime >= proj.ValueRO.DeathTime)
            {
                // TYLKO SERWER mo¿e niszczyæ encje oznaczone jako Ghost
                if (isServer)
                {
                    ecb.DestroyEntity(entity);
                }
                else
                {
                    // OPCJONALNIE NA KLIENCIE: 
                    // Mo¿esz wy³¹czyæ renderowanie pocisku, ¿eby "znikn¹³" wizualnie natychmiast,
                    // zanim serwer przyœle informacjê o jego zniszczeniu.
                    // ecb.RemoveComponent<URPMaterialPropertyBaseColor>(entity); // przyk³ad
                }
            }
            else
            {
                transform.ValueRW.Position = nextPos;
            }
        }
    }
}*/