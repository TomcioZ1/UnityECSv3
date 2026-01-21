using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(ProjectileHitSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
[BurstCompile]
public partial struct ProjectileDestroySystem : ISystem
{
    private EntityQuery _singletonQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // KLUCZ: Deklarujemy jawnie zapytanie o singletona ECB. 
        // To eliminuje b³¹d "required component type was not declared" w Burst.
        _singletonQuery = state.GetEntityQuery(ComponentType.ReadOnly<EndSimulationEntityCommandBufferSystem.Singleton>());

        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate(_singletonQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();

        // W Netcode niszczymy/wy³¹czamy tylko w tym specyficznym ticku
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        // BEZPIECZNE POBIERANIE ECB:
        // Pobieramy singletona przez zapytanie zadeklarowane w OnCreate.
        // U¿ywamy GetSingletonEntity, aby unikn¹æ b³êdu "matches 2", jeœli Netcode zduplikowa³ systemy.
        var ecbEntity = _singletonQuery.GetSingletonEntity();
        var ecbSingleton = state.EntityManager.GetComponentData<EndSimulationEntityCommandBufferSystem.Singleton>(ecbEntity);
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var isServer = state.WorldUnmanaged.IsServer();

        // PÊTLA ZABEZPIECZONA:
        // WithNone<Disabled> zapobiega "podwójnemu niszczeniu", które powodowa³o ArgumentException
        foreach (var (proj, entity) in SystemAPI.Query<RefRO<ProjectileComponent>>()
                     .WithAll<Simulate>()
                     .WithNone<Disabled>()
                     .WithEntityAccess())
        {
            if (proj.ValueRO.Lifetime <= 0)
            {
                if (isServer)
                {
                    // Na serwerze niszczymy ca³kowicie
                    ecb.DestroyEntity(entity);
                }
                else
                {
                    // Na kliencie TYLKO wy³¹czamy. Netcode sam usunie encjê, 
                    // gdy dostanie info z serwera. To zapobiega b³êdom desynchronizacji.
                    ecb.AddComponent<Disabled>(entity);
                }
            }
        }
    }
}