/*using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct SetPlayerNameServerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (rpc, reqEntity) in
                 SystemAPI.Query<RefRO<SetPlayerNameRpc>>()
                 .WithAll<ReceiveRpcCommandRequest>()
                 .WithEntityAccess())
        {
            var connection = SystemAPI
                .GetComponent<ReceiveRpcCommandRequest>(reqEntity)
                .SourceConnection;

            // znajdz ghosta gracza przypisanego do tego connection
            foreach (var (owner, playerEntity) in
                     SystemAPI.Query<RefRO<GhostOwner>>().WithEntityAccess())
            {
                if (owner.ValueRO.NetworkId ==
                    SystemAPI.GetComponent<NetworkId>(connection).Value)
                {
                    ecb.SetComponent(playerEntity, new PlayerName
                    {
                        Value = rpc.ValueRO.Name
                    });
                    Debug.Log("Set player name to: " +
                              rpc.ValueRO.Name.ToString());
                }
            }

            ecb.DestroyEntity(reqEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
*/