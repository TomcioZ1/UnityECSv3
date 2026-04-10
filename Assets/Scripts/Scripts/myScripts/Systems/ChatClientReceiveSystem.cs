using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ChatClientReceiveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (rpc, entity) in
            SystemAPI.Query<RefRO<ChatMessageRpc>>()
            .WithAll<ReceiveRpcCommandRequest>()
            .WithEntityAccess())
        {
            var evt = ecb.CreateEntity();
            ecb.AddComponent(evt, new ChatMessageEvent
            {
                Sender = rpc.ValueRO.Sender,
                Message = rpc.ValueRO.Message
            });

            ecb.DestroyEntity(entity); //  RPC ¿yje 1 tick
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
