using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering; // Wymagane dla MaterialProperty
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

        // System automatycznie znajdzie encje, które maj¹ DestroyedDrop, LocalTransform ORAZ DissolveProperty
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
                     ref DestroyedDrop drop,
                     ref LocalTransform transform,
                     ref DissolveProperty dissolve) // <--- Dodane do Execute
        {
            // 1. Grawitacja i ruch
            if (transform.Position.y > 0 || drop.Velocity.y > 0)
            {
                drop.Velocity += Gravity * DeltaTime;
                transform.Position += drop.Velocity * DeltaTime;
            }

            // 2. Blokada na poziomie ziemi
            if (transform.Position.y < 0)
            {
                transform.Position.y = 0;
                drop.Velocity = float3.zero;
            }

            // 3. Obliczanie postêpu ¿ycia
            float lifeRatio = math.saturate(drop.RemainingLife / drop.MaxLife);

            // Logika Dissolve:
            // Jeœli w shaderze 0 = widoczny, a 1 = rozpuszczony, u¿ywamy (1 - lifeRatio)
            dissolve.Value = 1.0f - lifeRatio;

            // Opcjonalnie: mo¿esz zachowaæ skalowanie lub polegaæ tylko na dissolve
            //transform.Scale = drop.BaseScale * lifeRatio;

            // 4. Odliczanie czasu i niszczenie
            drop.RemainingLife -= DeltaTime;

            if (drop.RemainingLife <= 0)
            {
                ECB.DestroyEntity(chunkIndex, entity);
            }
        }
    }
}