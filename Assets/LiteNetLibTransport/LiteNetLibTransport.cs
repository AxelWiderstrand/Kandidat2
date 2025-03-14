using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LiteNetLib;
using LiteNetLibMirror;
using UnityEngine;

namespace Mirror
{
    public class LiteNetLibTransport : Transport
    {
        [Header("Config")]
        public ushort port = 8888;
        public int updateTime = 15;
        public int disconnectTimeout = 5000;
        public bool ipv6Enabled;

        [Tooltip("Maximum connection attempts before client stops and call disconnect event.")]
        public int maxConnectAttempts = 10;

        [Tooltip("Caps the number of messages the server will process per tick. Allows LateUpdate to finish to let the reset of unity contiue incase more messages arrive before they are processed")]
        public int serverMaxMessagesPerTick = 10000;

        [Tooltip("Caps the number of messages the client will process per tick. Allows LateUpdate to finish to let the reset of unity contiue incase more messages arrive before they are processed")]
        public int clientMaxMessagesPerTick = 1000;

        [Tooltip("Uses index in list to map to DeliveryMethod. eg channel 0 => DeliveryMethod.ReliableOrdered")]
        public List<DeliveryMethod> channels = new List<DeliveryMethod>()
        {
            DeliveryMethod.ReliableOrdered,
            DeliveryMethod.Unreliable
        };

        [Tooltip("Key that client most give server in order to connect, this is handled automatically by the transport.")]
        public string connectKey = "MIRROR_LITENETLIB";

        /// <summary>
        /// Active Client, null is no client is active
        /// </summary>
        Client client;
        /// <summary>
        /// Active Server, null is no Server is active
        /// </summary>
        Server server;

        /// <summary>
        /// Client message recieved while Transport was disabled
        /// </summary>
        readonly Queue<ClientDataMessage> clientDisabledQueue = new Queue<ClientDataMessage>();

        /// <summary>
        /// Server message recieved while Transport was disabled
        /// </summary>
        readonly Queue<ServerDataMessage> serverDisabledQueue = new Queue<ServerDataMessage>();
        /// <summary>
        /// If messages were added to DisabledQueues
        /// </summary>
        bool checkMessageQueues;

        private void OnValidate()
        {
            Debug.Assert(channels.Distinct().Count() == channels.Count, "LiteNetLibTransport: channels should only use each DeliveryMethod");
            Debug.Assert(channels.Count > 0, "LiteNetLibTransport: There should be atleast 1 channel");
        }

        void Awake()
        {
            Debug.Log("LiteNetLibTransport initialized!");
        }

        public override void Shutdown()
        {
            Debug.Log("LiteNetLibTransport Shutdown");
            client?.Disconnect();
            server?.Stop();
        }

        public override bool Available()
        {
            // all except WebGL
            return Application.platform != RuntimePlatform.WebGLPlayer;
        }

        public override int GetMaxPacketSize(int channelId = Channels.Reliable)
        {
            // LiteNetLib NetPeer construct calls SetMTU(0), which sets it to
            // NetConstants.PossibleMtu[0] which is 576-68.
            // (bigger values will cause TooBigPacketException even on loopback)
            //
            // see also: https://github.com/RevenantX/LiteNetLib/issues/388
            return NetConstants.PossibleMtu[0];
        }

        new private void LateUpdate()
        {
            // check for messages in queue before processing new messages
            if (enabled && checkMessageQueues)
            {
                ProcessClientQueue();
                ProcessServerQueue();

                // if enabled becomes false not all message will be processed, so need to check if queues are empty before clearing flag
                if (clientDisabledQueue.Count == 0 && serverDisabledQueue.Count == 0)
                {
                    checkMessageQueues = false;
                }
            }

            if (client != null)
            {
                client.OnUpdate();
            }
            if (server != null)
            {
                server.OnUpdate();
            }
        }

        private void ProcessClientQueue()
        {
            int processedCount = 0;
            while (
                enabled &&
                processedCount < clientMaxMessagesPerTick &&
                clientDisabledQueue.Count > 0
                )
            {
                processedCount++;

                ClientDataMessage data = clientDisabledQueue.Dequeue();
                OnClientDataReceived.Invoke(data.data, data.channel);
            }
        }

        private void ProcessServerQueue()
        {
            int processedCount = 0;
            while (
                enabled &&
                processedCount < serverMaxMessagesPerTick &&
                serverDisabledQueue.Count > 0
                )
            {
                processedCount++;
                ServerDataMessage data = serverDisabledQueue.Dequeue();
                OnServerDataReceived.Invoke(data.clientId, data.data, data.channel);
            }
        }

        public override string ToString()
        {
            if (server != null)
            {
                // printing server.listener.LocalEndpoint causes an Exception
                // in UWP + Unity 2019:
                //   Exception thrown at 0x00007FF9755DA388 in UWF.exe:
                //   Microsoft C++ exception: Il2CppExceptionWrapper at memory
                //   location 0x000000E15A0FCDD0. SocketException: An address
                //   incompatible with the requested protocol was used at
                //   System.Net.Sockets.Socket.get_LocalEndPoint ()
                // so let's use the regular port instead.
                return "LiteNetLib Server port: " + port;
            }
            else if (client != null)
            {
                if (client.Connected)
                {
                    return "LiteNetLib Client ip: " + client.RemoteEndPoint;
                }
                else
                {
                    return "LiteNetLib Connecting...";
                }
            }
            return "LiteNetLib (inactive/disconnected)";
        }

        #region CLIENT
        public override bool ClientConnected() => client != null && client.Connected;

        public override void ClientConnect(string address)
        {
            if (client != null)
            {
                Debug.LogWarning("Can't start client as one was already connected");
                return;
            }

            client = new Client(port, updateTime, disconnectTimeout);

            client.onConnected += OnClientConnected.Invoke;
            client.onData += Client_onData;
            client.onDisconnected += OnClientDisconnected.Invoke;

            client.Connect(address, maxConnectAttempts, ipv6Enabled, connectKey);
        }

        private void Client_onData(ArraySegment<byte> data, DeliveryMethod deliveryMethod)
        {
            int channel = channels.IndexOf(deliveryMethod);
            if (enabled)
            {
                OnClientDataReceived.Invoke(data, channel);
            }
            else
            {
                clientDisabledQueue.Enqueue(new ClientDataMessage(data, channel));
                checkMessageQueues = true;
            }
        }

        public override void ClientDisconnect()
        {
            if (client != null)
            {
                // remove events before calling disconnect so stop loops within mirror
                client.onConnected -= OnClientConnected.Invoke;
                client.onData -= Client_onData;
                client.onDisconnected -= OnClientDisconnected.Invoke;

                client.Disconnect();
                client = null;
            }
        }

#if MIRROR_26_0_OR_NEWER
        public override void ClientSend(ArraySegment<byte> segment, int channelId)
        {
            if (client == null || !client.Connected)
            {
                Debug.LogWarning("Can't send when client is not connected");
                return;
            }

            DeliveryMethod deliveryMethod = channels[channelId];
            client.Send(deliveryMethod, segment);
        }
#else
        public override void ClientSend(ArraySegment<byte> segment, int channelId)
        {
            if (client == null || !client.Connected)
            {
                Debug.LogWarning("Can't send when client is not connected");
                return;
            }

            DeliveryMethod deliveryMethod = channels[channelId];
            client.Send(deliveryMethod, segment);
        }
#endif
        #endregion


        #region SERVER
        public override bool ServerActive() => server != null;

        public override void ServerStart()
        {
            if (server != null)
            {
                Debug.LogWarning("Can't start server as one was already active");
                return;
            }

            server = new Server(port, updateTime, disconnectTimeout, connectKey);

            server.onConnected += OnServerConnected.Invoke;
            server.onData += Server_onData;
            server.onDisconnected += OnServerDisconnected.Invoke;

            server.Start();
        }

        private void Server_onData(int clientId, ArraySegment<byte> data, DeliveryMethod deliveryMethod)
        {
            int channel = channels.IndexOf(deliveryMethod);
            if (enabled)
            {
                OnServerDataReceived.Invoke(clientId, data, channel);
            }
            else
            {
                serverDisabledQueue.Enqueue(new ServerDataMessage(clientId, data, channel));
                checkMessageQueues = true;
            }
        }

        public override void ServerStop()
        {
            if (server != null)
            {
                server.onConnected -= OnServerConnected.Invoke;
                server.onData -= Server_onData;
                server.onDisconnected -= OnServerDisconnected.Invoke;

                server.Stop();
                server = null;
            }
            else
            {
                Debug.LogWarning("Can't stop server as no server was active");
            }
        }
#if MIRROR_26_0_OR_NEWER 
        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId)
        {
            if (server == null)
            {
                Debug.LogWarning("Can't send when Server is not active");
                return;
            }

            DeliveryMethod deliveryMethod = channels[channelId];
            server.SendOne(connectionId, deliveryMethod, segment);
        }
#else
        public  bool ServerSend(System.Collections.Generic.List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            if (server == null)
            {
               // logger.LogWarning("Can't send when Server is not active");
                return false;
            }

            DeliveryMethod deliveryMethod = channels[channelId];
            return server.Send(connectionIds, deliveryMethod, segment);
        }
#endif

        public override void ServerDisconnect(int connectionId)
        {
            if (server == null)
            {
                Debug.LogWarning("Can't disconnect when Server is not active");
            } else {
                server.Disconnect(connectionId);
            }
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            return server?.GetClientAddress(connectionId);
        }

        public IPEndPoint ServerGetClientIPEndPoint(int connectionId)
        {
            return server?.GetClientIPEndPoint(connectionId);
        }

        public override Uri ServerUri()
        {
            return server?.GetUri();
        }
        #endregion
    }
}
