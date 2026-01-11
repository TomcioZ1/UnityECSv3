using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Burst;

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    // System pomocniczy do RPC (pozostaje bez zmian, ale upewnij siê, ¿e jest potrzebny w Twoim setupie)
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(RpcSystem))]
    public partial struct SetRpcSystemDynamicAssemblyListSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RpcCollection>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var rpcCollection = SystemAPI.GetSingletonRW<RpcCollection>();
            rpcCollection.ValueRW.DynamicAssemblyList = true;
            state.Enabled = false;
        }
    }

    public struct GoInGameRequest : IRpcCommand
    {
        public FixedString64Bytes PlayerName;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct GoInGameClientSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CubeSpawner>();
            // Czekamy, a¿ bêdziemy mieæ po³¹czenie, które nie jest jeszcze "InGame"
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>()
                .Build(ref state);
            state.RequireForUpdate(query);
        }

        // Nie u¿ywamy Burst, bo odwo³ujemy siê do PlayerNameInput.Instance (MonoBehaviour)
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithNone<NetworkStreamInGame>()
                         .WithEntityAccess())
            {
                // 1. Oznaczamy po³¹czenie jako bêd¹ce w grze
                ecb.AddComponent<NetworkStreamInGame>(entity);

                // 2. Pobieramy imiê
                string playerNameStr = PlayerNameInput.Instance != null ? PlayerNameInput.Instance.Name : "Player";

                // 3. Wysy³amy proœbê do serwera
                var req = ecb.CreateEntity();
                ecb.AddComponent(req, new GoInGameRequest { PlayerName = playerNameStr });
                ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });

                Debug.Log($"[Client] Po³¹czono. Wysy³anie proœby o wejœcie do gry dla: {playerNameStr}");
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct GoInGameServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CubeSpawner>();
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GoInGameRequest>()
                .WithAll<ReceiveRpcCommandRequest>()
                .Build(ref state);
            state.RequireForUpdate(query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var prefab = SystemAPI.GetSingleton<CubeSpawner>().Cube;
            var networkIdLookup = state.GetComponentLookup<NetworkId>(true);

            foreach (var (rpcRequest, goInGameRequest, rpcEntity) in
                     SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequest>>()
                     .WithEntityAccess())
            {
                var connection = rpcRequest.ValueRO.SourceConnection;

                // Sprawdzamy czy po³¹czenie jeszcze istnieje i czy nie jest ju¿ w grze
                if (!networkIdLookup.HasComponent(connection) || state.EntityManager.HasComponent<NetworkStreamInGame>(connection))
                {
                    ecb.DestroyEntity(rpcEntity);
                    continue;
                }

                var networkId = networkIdLookup[connection];
                var playerName = goInGameRequest.ValueRO.PlayerName;

                Debug.Log($"[Server] Spawnowanie gracza '{playerName}' dla NetworkId: {networkId.Value}");

                // 1. Kluczowe: Serwer musi dodaæ ten komponent do po³¹czenia, aby zacz¹æ replikacjê Ghostów!
                ecb.AddComponent<NetworkStreamInGame>(connection);

                // 2. Spawn gracza
                var player = ecb.Instantiate(prefab);
                ecb.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });

                // Upewnij siê, ¿e komponent PlayerName istnieje w ECS i jest zsynchronizowany (GhostField)
                ecb.SetComponent(player, new PlayerName { Value = playerName });

                // 3. Dodanie do LinkedEntityGroup, aby gracz zosta³ usuniêty przy roz³¹czeniu
                ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = player });

                // 4. Usuwamy encjê RPC
                ecb.DestroyEntity(rpcEntity);
            }
        }
    }
}