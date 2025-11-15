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
using ViMG.IMGUIImpl;

namespace Engine.Networking
{
    public class NetworkManager : INetEventListener
    {
        [ConsoleCommand("list_players", "lists currently connected players")]
        public static void ListPlayers(string[] parameters)
        {
            var players = Main.gameStateManager.TheIsland?.netManager?.netPlayers;
            if (players != null)
            {
                IMGUIConsole.LogLine(string.Format("{0} Players: ", players.Count));
                foreach (NetPlayer player in players)
                {
                    if (player.playerId == Main.gameStateManager.TheIsland?.GetWorld()?.localPlayerIndex)
                    {
                        IMGUIConsole.LogLine(string.Format("\tId: {0} (local player)", player.playerId));
                    }
                    else IMGUIConsole.LogLine(string.Format("\tId: {0}", player.playerId));
                }
            }
        }


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
                netPlayers.Add(new NetPlayer
                {
                    playerId = 0,
                    peerId = -1,
                });
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
                int index = netPlayers.Count;
                netPlayers.Add(new NetPlayer
                {
                    playerId = index,
                    peerId = peer.Id,
                });
                Player p = new Player();
                p.playerIndex = index;
                Main.gameStateManager.TheIsland.GetWorld().EntityManager.Add(p);
                Main.gameStateManager.TheIsland.GetWorld().player[index] = p;
                Main.Registry.MessageRegistry.SendMessageToPeer(SyncPlayerConnected.Instance, peer, index);
                Main.Registry.MessageRegistry.SendMessageToPeer(SyncAllWorldState.Instance, peer, netPlayers[index]);
                Console.WriteLine("Peer connected from {0}:{1}. Player id: {2}", peer.Address, peer.Port, netPlayers[index].playerId);
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (isServer)
            {
                int index = netPlayers.FindIndex(x => x.peerId == peer.Id);
                Console.WriteLine("Peer {0}:{1} disconnected. Player id: {2}\nReason: {3}", peer.Address, peer.Port, netPlayers[index].playerId, disconnectInfo.ToString());
                netPlayers.RemoveAt(index);
                var world = Main.gameStateManager.TheIsland.GetWorld();
                world.EntityManager.Remove(world.player[index]);
                world.player[index] = null;
                Main.Registry.MessageRegistry.SendMessageToAll(SyncPlayerConnected.Instance, netManager, -1);
            }
        }
    }
}
