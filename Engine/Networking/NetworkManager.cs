using Engine.Networking.Messages;
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
using ViMG;

namespace Engine.Networking
{
    public class NetworkManager : INetEventListener
    {
        [Flags]
        public enum NetworkSide
        {
            None = 0,
            Client,
            Server,
            Both = Client | Server
        };

        public readonly bool isServer;
        private NetManager netManager;

        public int whoAmI = -1;
        public List<NetPlayer> netPlayers = new List<NetPlayer>();

        public struct NetPlayer : INetSerializable
        {
            public int playerId;
            public int peerId; // -1 if client (we can't send messages to other clients, just to server

            public void Deserialize(NetDataReader reader)
            {
                playerId = reader.GetInt();
                peerId = -1;
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(playerId);
            }
        }

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
            Main.Registry.MessageRegistry.Dispatch(reader);
            //Console.WriteLine("Received {0} from {1}", result, peer);

            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            throw new NotImplementedException();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            if (isServer)
            {
                netPlayers.Add(new NetPlayer
                {
                    playerId = netPlayers.Count,
                    peerId = peer.Id,
                });
                Main.Registry.MessageRegistry.SendMessageToPeer(SyncPlayerConnected.Instance, peer, netPlayers.Count - 1);
                //Main.Registry.MessageRegistry.SendMessageToPeer(SyncAllWorldState.Instance, peer);
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (isServer)
            {
                netPlayers.RemoveAt(netPlayers.FindIndex(x => x.peerId == peer.Id));
                Main.Registry.MessageRegistry.SendMessageToPeer(SyncPlayerConnected.Instance, peer, -1);
            }
        }
    }
}
