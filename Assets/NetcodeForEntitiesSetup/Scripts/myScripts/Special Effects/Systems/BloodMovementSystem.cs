using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct BloodMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        float dt = SystemAPI.Time.DeltaTime;
        float3 gravity = new float3(0, -9.81f, 0);

        new BloodJob { DeltaTime = dt, Gravity = gravity, ECB = ecb }.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct BloodJob : IJobEntity
    {
        public float DeltaTime;
        public float3 Gravity;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref BloodDrop drop, ref LocalTransform transform)
        {
            drop.Velocity += Gravity * DeltaTime;
            transform.Position += drop.Velocity * DeltaTime;

            // Krew mocniej trzyma siê ziemi (mniej siê odbija ni¿ woda)
            if (transform.Position.y < 0)
            {
                transform.Position.y = 0;
                drop.Velocity = float3.zero; // Krew "rozmazuje siê" na ziemi i przestaje ruszaæ
            }

            drop.RemainingLife -= DeltaTime;
            if (drop.RemainingLife <= 0) ECB.DestroyEntity(chunkIndex, entity);
        }
    }
}