using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerReceiveNameSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        // 1. Przetwarzamy przychodz¹ce RPC
        foreach (var (rpc, request, rpcEntity) in SystemAPI.Query<RefRO<SetPlayerNameRpc>, RefRO<ReceiveRpcCommandRequest>>()
                     .WithEntityAccess())
        {
            Entity connectionSource = request.ValueRO.SourceConnection;
            FixedString64Bytes newName = rpc.ValueRO.Name;

            // 2. Szukamy encji gracza, która nale¿y do tego po³¹czenia
            // Szukamy encji z PlayerName, której GhostOwner matches connection ID
            if (state.EntityManager.HasComponent<NetworkId>(connectionSource))
            {
                var networkId = state.EntityManager.GetComponentData<NetworkId>(connectionSource).Value;

                foreach (var (playerName, ghostOwner, playerEntity) in SystemAPI.Query<RefRW<PlayerName>, RefRO<GhostOwner>>()
                             .WithEntityAccess())
                {
                    if (ghostOwner.ValueRO.NetworkId == networkId)
                    {
                        playerName.ValueRW.Value = newName;
                        Debug.Log($"[Server] Ustawiono nick {newName} dla encji {playerEntity.Index}");



                        // Opcjonalnie: Tutaj mo¿esz te¿ dodaæ gracza do Leaderboardu
                        // AddToLeaderboard(newName);
                    }
                }
            }

            ecb.DestroyEntity(rpcEntity);
        }
        ecb.Playback(state.EntityManager);
    }
}