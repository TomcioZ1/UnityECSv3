using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    // System pomocniczy zapewniaj¹cy poprawne rejestrowanie RPC w dynamicznych zestawach
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

    // Definicja proœby RPC
    public struct GoInGameRequest : IRpcCommand
    {
        public FixedString64Bytes PlayerName;
    }

    // STRONA KLIENTA: Wysy³a proœbê o wejœcie do gry
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct GoInGameClientSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CubeSpawner>();

            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>()
                .Build(ref state);
            state.RequireForUpdate(query);
        }

        // Brak Burst, poniewa¿ odwo³ujemy siê do MonoBehaviour (PlayerNameInput)
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithNone<NetworkStreamInGame>()
                         .WithEntityAccess())
            {
                // Oznaczamy po³¹czenie, aby nie wysy³aæ proœby wielokrotnie
                ecb.AddComponent<NetworkStreamInGame>(entity);

                // Pobieramy imiê z UI (MonoBehaviour)
                string playerNameStr = PlayerNameInput.Instance != null ? PlayerNameInput.Instance.Name : "Player";

                // Tworzymy encjê RPC
                var req = ecb.CreateEntity();
                ecb.AddComponent(req, new GoInGameRequest { PlayerName = playerNameStr });
                ecb.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });

                Debug.Log($"[Client] Po³¹czono. Proœba o spawn dla: {playerNameStr}");
            }
        }
    }

    // STRONA SERWERA: Obs³uguje proœbê, spawnuje gracza i ustawia skalê z prefaba
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

            // Dane spawnera (miejsce narodzin gracza)
            var spawnerEntity = SystemAPI.GetSingletonEntity<CubeSpawner>();
            var spawnerData = SystemAPI.GetComponent<CubeSpawner>(spawnerEntity);
            var spawnerTransform = SystemAPI.GetComponent<LocalTransform>(spawnerEntity);

            // POBIERANIE SKALI Z PREFABA:
            // Odczytujemy LocalTransform bezpoœrednio z encji prefaba zdefiniowanej w CubeSpawner
            var prefabTransform = state.EntityManager.GetComponentData<LocalTransform>(spawnerData.Cube);

            var networkIdLookup = state.GetComponentLookup<NetworkId>(true);

            foreach (var (rpcRequest, goInGameRequest, rpcEntity) in
                     SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequest>>()
                     .WithEntityAccess())
            {
                var connection = rpcRequest.ValueRO.SourceConnection;

                // Sprawdzamy czy po³¹czenie jest poprawne i czy gracz ju¿ nie jest w grze
                if (!networkIdLookup.HasComponent(connection) || state.EntityManager.HasComponent<NetworkStreamInGame>(connection))
                {
                    ecb.DestroyEntity(rpcEntity);
                    continue;
                }

                var networkId = networkIdLookup[connection];
                var playerName = goInGameRequest.ValueRO.PlayerName;

                Debug.Log($"[Server] Spawning '{playerName}' w {spawnerTransform.Position} ze skal¹ prefaba {prefabTransform.Scale}");

                // Dodajemy komponent InGame do po³¹czenia
                ecb.AddComponent<NetworkStreamInGame>(connection);

                // 1. Instancjonowanie gracza
                var player = ecb.Instantiate(spawnerData.Cube);

                // 2. USTAWIENIE TRANSFORMACJI:
                // Pozycja i Rotacja ze spawnera na scenie, Skala z Twojego Prefaba
                ecb.SetComponent(player, LocalTransform.FromPositionRotationScale(
                    spawnerTransform.Position,
                    spawnerTransform.Rotation,
                    prefabTransform.Scale));

                // 3. Konfiguracja Netcode
                ecb.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });

                // Zak³adamy, ¿e masz komponent PlayerName (IComponentData) do przechowywania imienia
                ecb.SetComponent(player, new PlayerName { Value = playerName });

                // Powi¹zanie encji gracza z po³¹czeniem (czyszczenie przy roz³¹czeniu)
                ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = player });

                // Usuwamy proœbê RPC po przetworzeniu
                ecb.DestroyEntity(rpcEntity);
            }
        }
    }
}