using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct ChatServerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (rpc, entity) in SystemAPI.Query<RefRO<ChatMessageRpc>>()
                 .WithAll<ReceiveRpcCommandRequest>()
                 .WithEntityAccess())
        {
            //Debug.Log($"SERVER RECEIVED from {rpc.ValueRO.Sender}: {rpc.ValueRO.Message}");

            // broadcast do wszystkich klientów
            var broadcast = ecb.CreateEntity();
            ecb.AddComponent(broadcast, new ChatMessageRpc
            {
                Sender = rpc.ValueRO.Sender,
                Message = rpc.ValueRO.Message
            });
            ecb.AddComponent<SendRpcCommandRequest>(broadcast);
            
            // usuñ oryginalny RPC od klienta
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
