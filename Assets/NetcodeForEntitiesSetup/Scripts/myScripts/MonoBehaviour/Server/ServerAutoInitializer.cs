using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    public class ServerAutoInitializer : MonoBehaviour
    {
        public ushort Port = 7979;
        public string GameplaySceneName = "Gameplay"; // Nazwa sceny z Twoj¹ gr¹

        void Start()
        {
            // 1. Sprawdzamy, czy to na pewno serwer (Headless)
            // Dziêki temu ten sam kod nie popsuje Ci klienta na Windowsie
            if (Application.isBatchMode || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                Debug.Log("[SERVER] Wykryto tryb Dedicated Server. Inicjalizacja...");
                StartDedicatedServer();
            }
            else
            {
                Debug.Log("[SERVER] To nie jest serwer dedykowany. Pomijanie autostartu.");
            }
            Debug.Log("[SERVER] Wykryto tryb Dedicated Server. Inicjalizacja...");
        }

        void StartDedicatedServer()
        {
            Application.runInBackground = true;

            // 2. Tworzymy œwiat serwera
            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = server;

            // 3. Otwieramy port
            var ep = NetworkEndpoint.AnyIpv4.WithPort(Port);
            using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());

            if (drvQuery.HasSingleton<NetworkStreamDriver>())
            {
                // W Unity 6 Listen zwraca bool
                bool success = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(ep);
                if (success)
                    Debug.Log($"[SERVER] SUCCESS: Port {Port} jest otwarty.");
                else
                    Debug.LogError($"[SERVER] ERROR: Nie uda³o siê otworzyæ portu {Port}!");
            }

            // 4. £adujemy scenê gry
            SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Single);
        }
    }
}