using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
            private ComponentLookup<NetworkId> _networkIdLookup;
            private ComponentLookup<NetworkStreamInGame> _networkStreamInGameLookup;
            private ComponentLookup<LocalTransform> _localTransformLookup;

            [BurstCompile]
            public void OnCreate(ref SystemState state)
            {
                state.RequireForUpdate<PlayerSpawner>();

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
                _networkIdLookup.Update(ref state);
                _networkStreamInGameLookup.Update(ref state);
                _localTransformLookup.Update(ref state);

                var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

                // 1. Pobieramy spawner i jego bufor punktów
                var spawnerEntity = SystemAPI.GetSingletonEntity<PlayerSpawner>();
                var spawnerData = SystemAPI.GetComponent<PlayerSpawner>(spawnerEntity);
                var spawnPoints = SystemAPI.GetBuffer<SpawnPointElement>(spawnerEntity);

                // Odczytujemy skalê z prefaba
                var prefabTransform = _localTransformLookup[spawnerData.Player];

                if (!SystemAPI.TryGetSingleton<TimeToStopTheGame>(out var gameStopData)) return;

                // Inicjalizacja losowoœci (opcjonalne, jeœli chcesz losowaæ punkty)
                var random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + 1);

                foreach (var (rpcRequest, goInGameRequest, rpcEntity) in
                         SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequest>>()
                         .WithEntityAccess())
                {
                    var connection = rpcRequest.ValueRO.SourceConnection;

                    if (!_networkIdLookup.HasComponent(connection) || _networkStreamInGameLookup.HasComponent(connection))
                    {
                        ecb.DestroyEntity(rpcEntity);
                        continue;
                    }

                    var networkId = _networkIdLookup[connection];
                    var playerName = goInGameRequest.ValueRO.PlayerName;

                    // 2. Wybieramy punkt spawnu
                    float3 spawnPosition = float3.zero; // Domyœlnie
                    if (spawnPoints.Length > 0)
                    {
                        // OPCJA A: Losowo
                        int randomIndex = random.NextInt(0, spawnPoints.Length);
                        spawnPosition = spawnPoints[randomIndex].Position;

                        /* OPCJA B: Po kolei (Round Robin) - wymaga zapisu do spawnerData
                        int index = spawnerData.NextSpawnIndex % spawnPoints.Length;
                        spawnPosition = spawnPoints[index].Position;
                        spawnerData.NextSpawnIndex++;
                        SystemAPI.SetComponent(spawnerEntity, spawnerData); 
                        */
                    }

                    // Reszta logiki RPC
                    /*var responseMsg = ecb.CreateEntity();
                    ecb.AddComponent(responseMsg, new GameStartTimeResponse { ExactTimeOfGameStop = gameStopData.ExactTimeOfGameStop });
                    ecb.AddComponent(responseMsg, new SendRpcCommandRequest { TargetConnection = connection });*/

                    ecb.AddComponent<NetworkStreamInGame>(connection);

                    var player = ecb.Instantiate(spawnerData.Player);

                    // 3. Ustawiamy pozycjê na wybran¹ z bufora
                    ecb.SetComponent(player, LocalTransform.FromPositionRotationScale(
                        spawnPosition, // Pozycja z bufora
                        quaternion.identity,
                        prefabTransform.Scale));

                    ecb.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });
                    ecb.SetComponent(player, new PlayerName { Value = playerName });

                    ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = player });

                    ecb.DestroyEntity(rpcEntity);
                }
            }
        }





    }



}




