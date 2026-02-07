using System;
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
        public string GameplaySceneName = "Gameplay";

        void Start()
        {
            // 1. Sprawdzamy, czy to na pewno serwer (Headless)
            if (Application.isBatchMode || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                Debug.Log("[SERVER] Wykryto tryb Dedicated Server. Inicjalizacja...");

                // USTAWIENIA GLOBALNE DLA PROCESU SERWERA
                // Zapobiega usypianiu procesora (bardzo wa¿ne na Linuxie!)
                Screen.sleepTimeout = SleepTimeout.NeverSleep;

                // Ustawiamy sta³y klatka¿, aby zadowoliæ Netcode Sleep Mode
                // Powinien byæ zgodny z Twoim TickRate (zazwyczaj 60)
                Application.targetFrameRate = 60;

                // Serwer musi dzia³aæ zawsze, nawet jeœli "straci focus" (choæ w BatchMode i tak nie ma okna)
                Application.runInBackground = true;

                StartDedicatedServer();
            }
            else
            {
                Debug.Log("[SERVER] To nie jest serwer dedykowany. Pomijanie autostartu.");
            }
        }

        void StartDedicatedServer()
        {
            // 2. Tworzymy œwiat serwera
            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = server;

            // 3. Otwieramy port
            var ep = NetworkEndpoint.AnyIpv4.WithPort(Port);
            using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());

            if (drvQuery.HasSingleton<NetworkStreamDriver>())
            {
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