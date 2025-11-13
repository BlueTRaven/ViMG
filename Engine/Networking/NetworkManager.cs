using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking
{
    public class NetworkManager : INetEventListener
    {
        public readonly bool isServer;
        private NetManager netManager;

        public int ConnectedPlayers => netManager.ConnectedPeersCount;

        public NetworkManager(bool isServer)
        {
            netManager = new NetManager(this);
            this.isServer = isServer;
        }

        public void Connect()
        {
            if (isServer)
            {
                netManager.Start(9050);
                Console.WriteLine("Started server on port 9050");
            }
            else
            {
                netManager.Start();
                netManager.Connect("localhost", 9050, "");
                Console.WriteLine("Connected to server");
            }
        }

        public void Disconnect()
        {
            netManager.DisconnectAll();
            Console.WriteLine("Disconnected");
        }

        public void PollEvents()
        {
            netManager.PollEvents();
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            string result = reader.GetString();
            Console.WriteLine("Received {0} from {1}", result, peer);

            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            throw new NotImplementedException();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put("Hello, world!");

            peer.Send(writer, DeliveryMethod.ReliableUnordered);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
        }
    }
}
