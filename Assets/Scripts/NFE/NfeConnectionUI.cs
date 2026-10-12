using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

namespace NetworkingLab.NFE
{
    /// <summary>Creates explicit NFE listen/connect requests from editable address and port fields.</summary>
    public sealed class NfeConnectionUI : MonoBehaviour
    {
        public const ushort DefaultPort = 7980;

        [SerializeField] private InputField addressInput;
        [SerializeField] private InputField portInput;
        [SerializeField] private Text statusText;
        [SerializeField] private Text localPlayerText;

        public void Configure(InputField address, InputField port, Text status, Text localPlayer)
        {
            addressInput = address;
            portInput = port;
            statusText = status;
            localPlayerText = localPlayer;
        }

        private void Start()
        {
            addressInput.text = ReadArgument("-address", "127.0.0.1");
            portInput.text = ReadArgument("-port", DefaultPort.ToString());

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

        private void Update()
        {
            World client = ClientServerBootstrap.ClientWorld;
            if (client == null || !client.IsCreated)
            {
                return;
            }

            EntityQuery query = client.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
            if (!query.IsEmptyIgnoreFilter)
            {
                int networkId = query.GetSingleton<NetworkId>().Value;
                localPlayerText.text = $"Local client ID: {networkId}";
                statusText.text = $"Status: Connected to {addressInput.text}:{ReadPort()}";
            }

            query.Dispose();
        }

        public void StartServer()
        {
            World server = ClientServerBootstrap.ServerWorld;
            if (server == null || !server.IsCreated)
            {
                SetStatus("Server World is not available in this play mode");
                return;
            }

            SetStatus($"Server endpoint prepared on UDP {ReadPort()}");
            // STUDENT TODO NFE-01: obtain NetworkStreamDriver from ServerWorld and call
            // Listen(NetworkEndpoint.AnyIpv4.WithPort(ReadPort())).
            using (EntityQuery query = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>()))
            {
                query.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(NetworkEndpoint.AnyIpv4.WithPort(ReadPort()));
            }
        }

        public void ConnectClient()
        {
            World client = ClientServerBootstrap.ClientWorld;
            if (client == null || !client.IsCreated)
            {
                SetStatus("Client World is not available in this play mode");
                return;
            }

            NetworkEndpoint endpoint = NetworkEndpoint.Parse(addressInput.text.Trim(), ReadPort());
            SetStatus($"Client endpoint prepared for {addressInput.text.Trim()}:{ReadPort()}");
            // STUDENT TODO NFE-01: obtain NetworkStreamDriver from ClientWorld and call
            // Connect(client.EntityManager, endpoint). The endpoint above is ready to use.
            using (EntityQuery query = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>()))
            {
                query.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, endpoint);
            }
        }

        public void StartHost()
        {
            StartServer();
            ConnectClient();
        }

        private ushort ReadPort()
        {
            return ushort.TryParse(portInput.text, out ushort port) && port > 0 ? port : DefaultPort;
        }

        private void SetStatus(string value)
        {
            statusText.text = $"Status: {value}";
            Debug.Log($"NFE_STATUS: {value}");
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
