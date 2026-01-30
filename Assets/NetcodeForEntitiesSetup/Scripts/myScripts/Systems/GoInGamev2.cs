using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    // System pomocniczy do RPC (pozostaje bez zmian, ale upewnij siê, ¿e jest potrzebny w Twoim setupie)
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

            // Pobieramy dane o prefabie i pozycji spawnera
            var spawnerEntity = SystemAPI.GetSingletonEntity<CubeSpawner>();
            var spawnerData = SystemAPI.GetComponent<CubeSpawner>(spawnerEntity);
            var spawnerTransform = SystemAPI.GetComponent<LocalTransform>(spawnerEntity);

            var networkIdLookup = state.GetComponentLookup<NetworkId>(true);

            foreach (var (rpcRequest, goInGameRequest, rpcEntity) in
                     SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequest>>()
                     .WithEntityAccess())
            {
                var connection = rpcRequest.ValueRO.SourceConnection;

                if (!networkIdLookup.HasComponent(connection) || state.EntityManager.HasComponent<NetworkStreamInGame>(connection))
                {
                    ecb.DestroyEntity(rpcEntity);
                    continue;
                }

                var networkId = networkIdLookup[connection];
                var playerName = goInGameRequest.ValueRO.PlayerName;

                Debug.Log($"[Server] Spawnowanie gracza '{playerName}' w pozycji {spawnerTransform.Position}");

                ecb.AddComponent<NetworkStreamInGame>(connection);

                // 1. Spawn gracza z prefabu
                var player = ecb.Instantiate(spawnerData.Cube);

                // 2. USTAWIENIE POZYCJI: Kopiujemy pozycjê i rotacjê ze spawnera
                // Ustawiamy Scale na 1, aby unikn¹æ problemów z fizyk¹
                ecb.SetComponent(player, LocalTransform.FromPositionRotationScale(
                    spawnerTransform.Position,
                    spawnerTransform.Rotation,
                    1.0f));

                // 3. Przypisanie w³aœciciela i imienia
                ecb.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });
                ecb.SetComponent(player, new PlayerName { Value = playerName });

                ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = player });
                ecb.DestroyEntity(rpcEntity);
            }
        }
    }




}