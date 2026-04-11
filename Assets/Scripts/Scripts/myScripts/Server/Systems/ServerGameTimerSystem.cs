using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerTimerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (timer, entity) in SystemAPI.Query<RefRW<GameTimer>>().WithEntityAccess())
        {
            if (timer.ValueRO.TimeRemaining > 0)
            {
                timer.ValueRW.TimeRemaining -= deltaTime;
            }
            else
            {
                timer.ValueRW.TimeRemaining = 0;

                // 1. Roz³¹cz graczy
                var connQuery = state.EntityManager.CreateEntityQuery(typeof(NetworkId));
                var connections = connQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var conn in connections)
                {
                    ecb.AddComponent<NetworkStreamRequestDisconnect>(conn);
                }

                // 2. Dodaj sygna³ do zmiany sceny (Singleton)
                var signalEntity = ecb.CreateEntity();
                ecb.AddComponent<QuitToServerSceneTag>(signalEntity);

                // 3. Zniszcz timer, ¿eby nie odpalaæ tego co klatkê
                ecb.DestroyEntity(entity);
            }
        }
    }
}

public struct QuitToServerSceneTag : IComponentData { }