using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

namespace NetworkingLab.NGO
{
    /// <summary>Small connection panel shared by Editor, client builds, and NGO server builds.</summary>
    public sealed class NgoConnectionUI : MonoBehaviour
    {
        public const ushort DefaultPort = 7979;

        [SerializeField] private InputField addressInput;
        [SerializeField] private InputField portInput;
        [SerializeField] private Text statusText;
        [SerializeField] private Text localPlayerText;

        private NetworkManager manager;

        public void Configure(InputField address, InputField port, Text status, Text localPlayer)
        {
            addressInput = address;
            portInput = port;
            statusText = status;
            localPlayerText = localPlayer;
        }

        private void Start()
        {
            manager = NetworkManager.Singleton;
            if (manager == null)
            {
                Debug.LogError("[NgoConnectionUI] NetworkManager.Singleton is null. Ensure a NetworkManager exists in the scene.");
                return;
            }

            addressInput.text = ReadArgument("-address", "127.0.0.1");
            portInput.text = ReadArgument("-port", DefaultPort.ToString());

            manager.OnClientConnectedCallback += OnClientConnected;
            manager.OnClientDisconnectCallback += OnClientDisconnected;
            manager.OnServerStarted += OnServerStarted;

            if (HasArgument("-client"))
            {
                ConnectClient();
            }
            else if (Application.isBatchMode)
            {
                StartServer();
            }
            else
            {
                SetStatus("Offline");
            }
        }

        private void OnDestroy()
        {
            if (manager == null)
            {
                return;
            }

            manager.OnClientConnectedCallback -= OnClientConnected;
            manager.OnClientDisconnectCallback -= OnClientDisconnected;
            manager.OnServerStarted -= OnServerStarted;
        }

        public void StartServer()
        {
            NgoPlayerNetwork.ResetSequence();
            ConfigureTransport(true);
            SetStatus(manager.StartServer() ? $"Server listening on UDP {ReadPort()}" : "Server start failed");
        }

        public void StartHost()
        {
            NgoPlayerNetwork.ResetSequence();
            ConfigureTransport(true);
            SetStatus(manager.StartHost() ? $"Host listening on UDP {ReadPort()}" : "Host start failed");
        }

        public void ConnectClient()
        {
            ConfigureTransport(false);
            SetStatus(manager.StartClient() ? $"Connecting to {addressInput.text}:{ReadPort()}" : "Client start failed");
        }

        public void Shutdown()
        {
            manager.Shutdown();
            localPlayerText.text = "Local player: none";
            SetStatus("Offline");
        }

        private void ConfigureTransport(bool server)
        {
            UnityTransport transport = manager.GetComponent<UnityTransport>();
            string address = string.IsNullOrWhiteSpace(addressInput.text) ? "127.0.0.1" : addressInput.text.Trim();
            transport.SetConnectionData(address, ReadPort(), server ? "0.0.0.0" : null);
        }

        private ushort ReadPort()
        {
            return ushort.TryParse(portInput.text, out ushort port) && port > 0 ? port : DefaultPort;
        }

        private void OnServerStarted()
        {
            SetStatus($"Server listening on UDP {ReadPort()}");
        }

        private void OnClientConnected(ulong clientId)
        {
            if (manager.IsClient && clientId == manager.LocalClientId)
            {
                localPlayerText.text = $"Local client ID: {clientId}";
                SetStatus($"Connected to {addressInput.text}:{ReadPort()}");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == manager.LocalClientId)
            {
                localPlayerText.text = "Local player: disconnected";
                SetStatus("Disconnected");
            }
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = $"Status: {value}";
            }

            Debug.Log($"NGO_STATUS: {value}");
        }

        private static string ReadArgument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }

            return fallback;
        }

        private static bool HasArgument(string name)
        {
            return Array.Exists(Environment.GetCommandLineArgs(), value =>
                string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
