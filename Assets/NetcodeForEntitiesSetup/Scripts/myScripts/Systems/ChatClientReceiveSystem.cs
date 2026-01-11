using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ChatClientReceiveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (rpc, entity) in SystemAPI.Query<RefRO<ChatMessageRpc>>()
                 .WithAll<ReceiveRpcCommandRequest>()
                 .WithEntityAccess())
        {
            // Tworzymy encjê ChatMessageEvent dla UI
            var e = ecb.CreateEntity();
            ecb.AddComponent(e, new ChatMessageEvent
            {
                Message = rpc.ValueRO.Message
            });

            // Usuwamy RPC
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}
