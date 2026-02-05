using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct WaterDropSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        float dt = SystemAPI.Time.DeltaTime;
        float3 gravity = new float3(0, -9.81f, 0);

        new WaterDropJob
        {
            DeltaTime = dt,
            Gravity = gravity,
            ECB = ecb
        }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct WaterDropJob : IJobEntity
    {
        public float DeltaTime;
        public float3 Gravity;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref WaterDrop drop, ref LocalTransform transform)
        {
            // Fizyka
            drop.Velocity += Gravity * DeltaTime;
            transform.Position += drop.Velocity * DeltaTime;

            // Odbicie od ziemi (Y=0)
            if (transform.Position.y < 0)
            {
                transform.Position.y = 0;
                drop.Velocity.y *= -0.3f; // T³umienie
            }

            // Skalowanie (znikanie)
            float lifePercent = math.saturate(drop.RemainingLife);
            transform.Scale = 0.15f * lifePercent;

            // Czas ¿ycia
            drop.RemainingLife -= DeltaTime;
            if (drop.RemainingLife <= 0)
            {
                ECB.DestroyEntity(chunkIndex, entity);
            }
        }
    }
}