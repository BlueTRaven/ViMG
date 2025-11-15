using BepuPhysics.Constraints;
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
                var world = Main.gameStateManager.TheIsland.GetWorld();
                int index = netPlayers.Count;
                netPlayers.Add(new NetPlayer
                {
                    playerId = index,
                    peerId = peer.Id,
                });
                Player p = new Player();
                p.playerIndex = index;
                // TODO
                p.FirstCreated(world.WorldInfo);
                world.EntityManager.Add(p);
                world.player[index] = p;
                world.ChunkLoadManager.LoadAroundTarget(world);
                Main.Registry.MessageRegistry.SendMessageToPeer(SyncPlayerConnected.Instance, peer, index);
                Main.Registry.MessageRegistry.SendMessageToPeer(SyncAllWorldState.Instance, peer, netPlayers[index]);
                Main.Registry.MessageRegistry.SendMessageToAll(SyncChunk.Instance, netManager, ChunkPosition.CubeChunk(world.GetLocalPlayer().SpawnPosition));
                foreach (var chunkPosition in world.ChunkLoadManager.GetLoaded())
                {
                    Main.Registry.MessageRegistry.SendMessageToAll(SyncChunk.Instance, netManager, chunkPosition);
                }
                Console.WriteLine("Peer connected from {0}. Player id: {1}", peer, netPlayers[index].playerId);
            }
            else
            {
                Console.WriteLine("Connected to server at {0}.", peer);
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (isServer)
            {
                var world = Main.gameStateManager.TheIsland.GetWorld();
                int index = netPlayers.FindIndex(x => x.peerId == peer.Id);
                int playerIndex = netPlayers[index].playerId;
                Debug.Assert(world.localPlayerIndex != playerIndex);
                Debug.Assert(world.player[playerIndex] != null);
                Console.WriteLine("Peer {0} disconnected. Player id: {1}\nReason: {1}", peer, playerIndex, disconnectInfo.ToString());
                world.EntityManager.Remove(world.player[playerIndex]);
                world.player[playerIndex] = null;
                netPlayers.RemoveAt(index);
                Main.Registry.MessageRegistry.SendMessageToAll(SyncPlayerConnected.Instance, netManager, -1);
            }
            else
            {
                // Server has disconnected from us? We should go back to main menu.
                Console.WriteLine("Lost connection to server (Peer {0}).\nReason: {1}", peer, disconnectInfo.ToString());
                Main.gameStateManager.SetGameState(Main.gameStateManager.MainMenu);
            }
        }
    }
}
