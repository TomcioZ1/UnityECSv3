using System;
using System.Diagnostics;
using Unity.Collections;
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
            if (Application.isBatchMode || true)
            {
                UnityEngine.Debug.Log("[SERVER] Wykryto tryb Dedicated Server. Inicjalizacja...");

                // USTAWIENIA GLOBALNE DLA PROCESU SERWERA
                // Zapobiega usypianiu procesora (bardzo wa¿ne na Linuxie!)
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Application.runInBackground = true;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 0;

                //NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;


                StartDedicatedServer();
            }
            else
            {
                UnityEngine.Debug.Log("[SERVER] To nie jest serwer dedykowany. Pomijanie autostartu.");
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
                    UnityEngine.Debug.Log($"[SERVER] SUCCESS: Port {Port} jest otwarty.");
                else
                    UnityEngine.Debug.LogError($"[SERVER] ERROR: Nie uda³o siê otworzyæ portu {Port}!");
            }

            // 4. £adujemy scenê gry
            SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Single);
        }
    }
}