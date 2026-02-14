using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    // System pomocniczy zapewniaj¹cy poprawne rejestrowanie RPC
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(RpcSystem))]
    [BurstCompile]
    public partial struct SetRpcSystemDynamicAssemblyListSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RpcCollection>();
        }

        [BurstCompile]
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

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct GoInGameClientSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawner>();

            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>()
                .Build(ref state);
            state.RequireForUpdate(query);
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithNone<NetworkStreamInGame>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<NetworkStreamInGame>(entity);

                string playerNameStr = PlayerInfoClass.PlayerName != null ? PlayerInfoClass.PlayerName : "Player";

                var req = ecb.CreateEntity();
                ecb.AddComponent(req, new GoInGameRequest { PlayerName = playerNameStr });
                ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });

                Debug.Log($"[Client] Po³¹czono. Proœba o spawn dla: {playerNameStr}");
            }
        }


     




        [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
        public partial struct GoInGameServerSystem : ISystem
        {
            // 1. Deklaracja lookupów w strukturze
            private ComponentLookup<NetworkId> _networkIdLookup;
            private ComponentLookup<NetworkStreamInGame> _networkStreamInGameLookup;
            private ComponentLookup<LocalTransform> _localTransformLookup;

            [BurstCompile]
            public void OnCreate(ref SystemState state)
            {
                state.RequireForUpdate<PlayerSpawner>();

                // 2. Inicjalizacja lookupów
                _networkIdLookup = state.GetComponentLookup<NetworkId>(true);
                _networkStreamInGameLookup = state.GetComponentLookup<NetworkStreamInGame>(true);
                _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);

                var query = new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<GoInGameRequest>()
                    .WithAll<ReceiveRpcCommandRequest>()
                    .Build(ref state);
                state.RequireForUpdate(query);
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state)
            {
                // 3. Aktualizacja lookupów przed u¿yciem
                _networkIdLookup.Update(ref state);
                _networkStreamInGameLookup.Update(ref state);
                _localTransformLookup.Update(ref state);

                var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

                var spawnerEntity = SystemAPI.GetSingletonEntity<PlayerSpawner>();
                var spawnerData = SystemAPI.GetComponent<PlayerSpawner>(spawnerEntity);
                var spawnerTransform = SystemAPI.GetComponent<LocalTransform>(spawnerEntity);

                // Odczytujemy skalê z prefaba przez zaktualizowany lookup
                var prefabTransform = _localTransformLookup[spawnerData.Player];

                foreach (var (rpcRequest, goInGameRequest, rpcEntity) in
                         SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequest>>()
                         .WithEntityAccess())
                {
                    var connection = rpcRequest.ValueRO.SourceConnection;

                    // Sprawdzamy po³¹czenie u¿ywaj¹c lookupów zamiast EntityManager (dzia³a szybciej i z Burst)
                    if (!_networkIdLookup.HasComponent(connection) || _networkStreamInGameLookup.HasComponent(connection))
                    {
                        ecb.DestroyEntity(rpcEntity);
                        continue;
                    }

                    var networkId = _networkIdLookup[connection];
                    var playerName = goInGameRequest.ValueRO.PlayerName;
                    SendDestroyedGhostsToClient(ref state, ecb, connection);
                    Debug.Log($"[Server] Spawning '{playerName}' w {spawnerTransform.Position} ze skal¹ prefaba {prefabTransform.Scale}");


                    ecb.AddComponent<NetworkStreamInGame>(connection);

                    var player = ecb.Instantiate(spawnerData.Player);

                    ecb.SetComponent(player, LocalTransform.FromPositionRotationScale(
                        spawnerTransform.Position,
                        spawnerTransform.Rotation,
                        prefabTransform.Scale));

                    ecb.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });
                    ecb.SetComponent(player, new PlayerName { Value = playerName });

                    ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = player });

                    ecb.DestroyEntity(rpcEntity);
                }
            }


            private void SendDestroyedGhostsToClient(ref SystemState state, EntityCommandBuffer ecb, Entity targetConnection)
            {
                if (!SystemAPI.TryGetSingletonEntity<DestroyedGhostElement>(out Entity bufferEntity))
                    return;

                var destroyedBuffer = SystemAPI.GetBuffer<DestroyedGhostElement>(bufferEntity);

                var rpcData = new SyncDestroyedGhostsRPC();
                rpcData.GhostIds = new FixedList128Bytes<int>();

                // Przepisujemy dane z bufora do listy RPC (max 32 inty dla FixedList128)
                // Jeœli potrzebujesz wiêcej, u¿yj FixedList512Bytes (max 128 intów)
                for (int i = 0; i < destroyedBuffer.Length && i < 32; i++)
                {
                    rpcData.GhostIds.Add(destroyedBuffer[i].GhostId);
                }

                var rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, rpcData);
                ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = targetConnection });

                Debug.Log($"[Server] Wys³ano RPC z {rpcData.GhostIds.Length} zniszczonymi duchami do klienta.");
            }






        }



    }
}




