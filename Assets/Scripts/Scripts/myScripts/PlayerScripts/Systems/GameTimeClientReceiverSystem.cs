using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct GameTimeClientReceiverSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        // Szukamy przychodz¹cych odpowiedzi z czasem
        foreach (var (response, rpcEntity) in SystemAPI.Query<RefRO<GameStartTimeResponse>>().WithEntityAccess())
        {
            // Opcja A: Zapisujemy czas do singletona na kliencie
            if (SystemAPI.TryGetSingletonEntity<TimeToStopTheGame>(out var timeEntity))
            {
                ecb.SetComponent(timeEntity, new TimeToStopTheGame { ExactTimeOfGameStop = response.ValueRO.ExactTimeOfGameStop });
            }
            else
            {
                // Jeœli singleton jeszcze nie istnieje na kliencie, tworzymy go
                var newTimeEntity = ecb.CreateEntity();
                ecb.AddComponent(newTimeEntity, new TimeToStopTheGame { ExactTimeOfGameStop = response.ValueRO.ExactTimeOfGameStop });
            }

            Debug.Log($"[Client] Otrzymano czas zakoñczenia gry od serwera: {response.ValueRO.ExactTimeOfGameStop}");

            // Niszczymy encjê RPC po odebraniu
            ecb.DestroyEntity(rpcEntity);
        }
    }
}