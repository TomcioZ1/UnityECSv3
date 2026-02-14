using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct MoveVisualProjectileSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        double elapsedTime = SystemAPI.Time.ElapsedTime;
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (transform, proj, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<VisualProjectile>>()
                 .WithEntityAccess())
        {
            float3 currentPos = transform.ValueRO.Position;
            float3 velocity = proj.ValueRO.Velocity;

            // 1. Obliczamy dystans, jaki pocisk pokona w TEJ klatce
            float3 frameMovement = velocity * dt;
            float frameDistance = math.length(frameMovement);
            float3 nextPos = currentPos + frameMovement;

            // 2. Definiujemy parametry precyzji:
            // Promieñ pocisku (po³owa skali X/Y)
            const float projectileRadius = 0.05f;
            // Margines b³êdu oparty na prêdkoœci (zapobiega "przeskakiwaniu" celu)
            float hitThreshold = math.max(projectileRadius, frameDistance);
            float hitThresholdSq = hitThreshold * hitThreshold;

            // 3. Obliczamy dystans do celu od aktualnej pozycji
            float distToTargetSq = math.distancesq(currentPos, proj.ValueRO.TargetPos);

            // 4. Logika trafienia:
            // Sprawdzamy czy dystans do celu jest mniejszy ni¿ to, co przelecimy w tej klatce
            // Dodajemy projectileRadius, aby pocisk znika³ gdy "nos" dotknie celu, a nie œrodek.
            bool isHittingTarget = distToTargetSq <= (hitThresholdSq);
            bool isExpired = elapsedTime > proj.ValueRO.DeathTime;

            if (isHittingTarget)
            {
                // Dla idealnej precyzji wizualnej ustawiamy pocisk dok³adnie w TargetPos przed zniszczeniem
                transform.ValueRW.Position = proj.ValueRO.TargetPos;
                ecb.DestroyEntity(entity);
            }
            else
            {
                // Jeœli nie trafi³, aktualizujemy pozycjê i rotacjê (na wypadek zmian kierunku)
                transform.ValueRW.Position = nextPos;

                // Opcjonalnie: upewniamy siê, ¿e przód (oœ Z pocisku 0.7) patrzy w stronê celu
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(math.normalize(velocity), math.up());
            }
        }
    }
}