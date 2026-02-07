using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct DestroyedDropMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        new DestroyedDropJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Gravity = new float3(0, -9.81f, 0),
            ECB = ecb
        }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct DestroyedDropJob : IJobEntity
    {
        public float DeltaTime;
        public float3 Gravity;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
                     ref DestroyedDrop drop, ref LocalTransform transform)
        {
            // 1. Grawitacja i ruch (tylko jeœli obiekt jest nad ziemi¹ lub ma prêdkoœæ pionow¹)
            if (transform.Position.y > 0 || drop.Velocity.y > 0)
            {
                drop.Velocity += Gravity * DeltaTime;
                transform.Position += drop.Velocity * DeltaTime;
            }

            // 2. Blokada na poziomie ziemi
            if (transform.Position.y < 0)
            {
                transform.Position.y = 0;
                drop.Velocity = float3.zero; // Zatrzymuje siê w miejscu po upadku
            }

            // 3. Skalowanie na podstawie czasu ¿ycia
            // saturate pilnuje, ¿eby wynik by³ w przedziale 0.0 - 1.0
            float lifeRatio = math.saturate(drop.RemainingLife / drop.MaxLife);
            transform.Scale = drop.BaseScale * lifeRatio;

            // 4. Odliczanie czasu i niszczenie
            drop.RemainingLife -= DeltaTime;

            if (drop.RemainingLife <= 0)
            {
                ECB.DestroyEntity(chunkIndex, entity);
            }
        }
    }
}