using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientProjectileVisualizerSystem : ISystem
{
    // Przechowujemy informacjê o ostatnim obs³u¿onym strzale dla ka¿dej encji
    private ComponentLookup<LastProcessedShot> _lastShotLookup;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ProjectilePrefabNoScary>();
        _lastShotLookup = state.GetComponentLookup<LastProcessedShot>(true);
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var prefab = SystemAPI.GetSingleton<ProjectilePrefabNoScary>().Value;

        // Iterujemy po wszystkich "duchach" graczy w zasiêgu wzroku
        foreach (var (shotEvent, transform, entity) in
                 SystemAPI.Query<RefRO<ShotEvent>, RefRO<LocalTransform>>()
                 .WithEntityAccess())
        {
            // Sprawdzamy, czy serwer przys³a³ info o nowym strzale
            if (SystemAPI.HasComponent<LastProcessedShot>(entity))
            {
                var lastShot = SystemAPI.GetComponent<LastProcessedShot>(entity);
                if (lastShot.Count == shotEvent.ValueRO.ShotCount) continue; // Ju¿ to obs³u¿yliœmy

                // NOWY STRZA£ WYKRYTY!
                // Obliczamy pozycjê lufy na podstawie AKTUALNEJ (interpolowanej) pozycji gracza
                float3 currentMuzzlePos = transform.ValueRO.Position + math.mul(transform.ValueRO.Rotation, new float3( 0.07f, 0f ,1f ));
                 
                // Spawnujemy lokalny pocisk (tylko grafika, nie jest Ghostem!)
                Entity vProj = ecb.Instantiate(prefab);
                ecb.SetComponent(vProj, LocalTransform.FromPositionRotation(currentMuzzlePos, quaternion.LookRotationSafe(shotEvent.ValueRO.Direction, math.up())));
                ecb.AddComponent(vProj, new VisualProjectile
                {
                    Velocity = shotEvent.ValueRO.Direction * 20f,
                    TargetPos = shotEvent.ValueRO.TargetPos,
                });

                // Aktualizujemy licznik
                ecb.SetComponent(entity, new LastProcessedShot { Count = shotEvent.ValueRO.ShotCount });
            }
            else
            {
                ecb.AddComponent(entity, new LastProcessedShot { Count = shotEvent.ValueRO.ShotCount });
            }
        }
    }
}

