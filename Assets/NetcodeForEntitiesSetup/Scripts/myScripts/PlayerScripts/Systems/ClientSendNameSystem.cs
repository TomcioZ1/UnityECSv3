using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientSendNameSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        // Szukamy encji po³¹czenia, która jeszcze nie wys³a³a nicku
        foreach (var (netId, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                     .WithNone<NameSentTag>()
                     .WithEntityAccess())
        {
            FixedString64Bytes nameToSend = "Player";
            if (PlayerInfoClass.PlayerName != null)
                nameToSend = PlayerInfoClass.PlayerName;

            var rpc = ecb.CreateEntity();
            ecb.AddComponent(rpc, new SetPlayerNameRpc { Name = nameToSend });
            ecb.AddComponent(rpc, new SendRpcCommandRequest { TargetConnection = entity });

            ecb.AddComponent<NameSentTag>(entity);
            Debug.Log($"[Client] Wys³ano RPC z nickiem: {nameToSend}");
        }
        ecb.Playback(state.EntityManager);
    }
}
public struct NameSentTag : IComponentData { }