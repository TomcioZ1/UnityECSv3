using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // Wymagane dla pól tekstowych TextMeshPro

#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem.UI;
#endif

namespace Unity.Multiplayer.Center.NetcodeForEntitiesSetup
{
    public class ConnectionUI : MonoBehaviour
    {
        [Header("UI References (TextMeshPro)")]
        public TMP_InputField AddressInputField; // Pole na IP/Domenê (np. toys-firm.gl.at.ply.gg)
        public TMP_InputField PortInputField;    // Pole na Port (np. 9805)

        [Header("Settings")]
        public string SceneToLoad;
        public Text ConnectionLabel;
        public Button StartHostButton;
        public Button StartClientButton;

        internal static string OldFrontendWorldName = string.Empty;

        public string ConnectionStatus
        {
            get => ConnectionLabel != null ? ConnectionLabel.text : "";
            set
            {
                if (ConnectionLabel != null)
                    ConnectionLabel.text = value;
            }
        }

        public void OnBeforeConnect()
        {
            Application.runInBackground = true;
        }

        public void OnConnected()
        {
            DestroyLocalSimulationWorld();

            var scene = SceneManager.GetSceneByName(SceneToLoad);
            if (scene.IsValid())
                return;

            SceneManager.LoadSceneAsync(SceneToLoad, LoadSceneMode.Single);
        }

        void Awake()
        {
            if (StartHostButton != null) StartHostButton.onClick.AddListener(StartClientServer);
            if (StartClientButton != null) StartClientButton.onClick.AddListener(StartClient);

            // Domyœlne wartoœci startowe w polach tekstowych
            if (AddressInputField != null && string.IsNullOrEmpty(AddressInputField.text))
                AddressInputField.text = "127.0.0.1";

            if (PortInputField != null && string.IsNullOrEmpty(PortInputField.text))
                PortInputField.text = "7979";

            if (!FindAnyObjectByType<EventSystem>())
            {
                var inputType = typeof(StandaloneInputModule);
#if ENABLE_INPUT_SYSTEM
                inputType = typeof(InputSystemUIInputModule);
#endif
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), inputType);
                eventSystem.transform.SetParent(transform);
            }
        }

        void AddConnectionUISystemToUpdateList()
        {
            foreach (var world in World.All)
            {
                if (world.IsClient() && !world.IsThinClient())
                {
                    var sys = world.GetOrCreateSystemManaged<ConnectionUISystem>();
                    sys.UIBehaviour = this;
                    var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
                    simGroup.AddSystemToUpdateList(sys);
                }
            }
        }

        void StartClientServer()
        {
            if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
            {
                Debug.LogError($"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
                return;
            }

            // Pobieramy port wpisany przez u¿ytkownika
            ushort port = ushort.TryParse(PortInputField.text, out var p) ? p : (ushort)7979;

            OnBeforeConnect();
            DisableButtons();

            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");

            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = server;

            OnConnected();

            // SERWER: S³ucha na wszystkich adresach (0.0.0.0) na wybranym porcie
            NetworkEndpoint ep = NetworkEndpoint.AnyIpv4.WithPort(port);
            using (var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>()))
            {
                if (drvQuery.HasSingleton<NetworkStreamDriver>())
                    drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(ep);
            }

            // KLIENT: £¹czy siê lokalnie (Loopback)
            ep = NetworkEndpoint.LoopbackIpv4.WithPort(port);
            using (var drvQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>()))
            {
                if (drvQuery.HasSingleton<NetworkStreamDriver>())
                    drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, ep);
            }
            AddConnectionUISystemToUpdateList();
        }

        void StartClient()
        {
            // 1. Pobieramy dane z TextMeshPro
            string targetAddress = AddressInputField.text.Trim(); // Trim usuwa przypadkowe spacje
            if (!ushort.TryParse(PortInputField.text, out ushort targetPort))
            {
                targetPort = 7979;
            }

            OnBeforeConnect();
            DisableButtons();
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");

            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = client;

            OnConnected();

            // 2. Rozwi¹zywanie adresu (DNS lub IP)
            NetworkEndpoint ep = default;

            // Sprawdzamy, czy to surowy adres IP (np. 127.0.0.1)
            if (NetworkEndpoint.TryParse(targetAddress, targetPort, out ep))
            {
                ConnectWithEndpoint(client, ep);
            }
            else
            {
                // Jeœli to nie IP, traktujemy to jako domenê (np. *.ply.gg)
                // U¿ywamy GetHostAddresses, aby zamieniæ domenê na IP
                try
                {
                    var addresses = System.Net.Dns.GetHostAddresses(targetAddress);
                    if (addresses.Length > 0)
                    {
                        // Bierzemy pierwszy znaleziony adres IP i tworzymy endpoint
                        ep = NetworkEndpoint.Parse(addresses[0].ToString(), targetPort);
                        ConnectWithEndpoint(client, ep);
                    }
                    else
                    {
                        Debug.LogError($"Could not resolve DNS for: {targetAddress}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"DNS Resolution failed for {targetAddress}: {e.Message}");
                }
            }

            AddConnectionUISystemToUpdateList();
        }

        // Metoda pomocnicza do wykonania samego po³¹czenia
        void ConnectWithEndpoint(World clientWorld, NetworkEndpoint ep)
        {
            using (var drvQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>()))
            {
                if (drvQuery.HasSingleton<NetworkStreamDriver>())
                {
                    Debug.Log($"Connecting to {ep.Address}");
                    drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, ep);
                }
            }
        }

        static void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    OldFrontendWorldName = world.Name;
                    world.Dispose();
                    break;
                }
            }
        }

        void DisableButtons()
        {
            if (StartHostButton != null) StartHostButton.interactable = false;
            if (StartClientButton != null) StartClientButton.interactable = false;
            if (AddressInputField != null) AddressInputField.interactable = false;
            if (PortInputField != null) PortInputField.interactable = false;
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [DisableAutoCreation]
    public partial class ConnectionUISystem : SystemBase
    {
        public ConnectionUI UIBehaviour;
        string m_PingText;

        protected override void OnUpdate()
        {
            if (UIBehaviour == null) return;

            if (!SystemAPI.TryGetSingletonEntity<NetworkStreamConnection>(out var connectionEntity))
            {
                UIBehaviour.ConnectionStatus = "Not connected!";
                m_PingText = default;
                return;
            }

            var connection = EntityManager.GetComponentData<NetworkStreamConnection>(connectionEntity);

            if (!SystemAPI.TryGetSingleton<NetworkStreamDriver>(out var driver)) return;

            var remoteEndPoint = driver.GetRemoteEndPoint(connection);
            var address = remoteEndPoint.IsValid ? remoteEndPoint.Address : "Unknown";

            if (EntityManager.HasComponent<NetworkId>(connectionEntity))
            {
                if (string.IsNullOrEmpty(m_PingText) || UnityEngine.Time.frameCount % 30 == 0)
                {
                    var networkSnapshotAck = EntityManager.GetComponentData<NetworkSnapshotAck>(connectionEntity);
                    m_PingText = networkSnapshotAck.EstimatedRTT > 0 ? $"{(int)networkSnapshotAck.EstimatedRTT}ms" : "Connected";
                }

                UIBehaviour.ConnectionStatus = $"<color=#00FF00>{address} | {m_PingText}:</color>";
            }
            else
            {
                UIBehaviour.ConnectionStatus = $"{address} | Connecting...";
            }
        }
    }
}