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
        public const double TIME_TRAVEL_DELAY = Main.FIXED_STEP * 3;

        [ConsoleCommand("list_players", "lists currently connected players")]
        public static void ListPlayers(string[] parameters)
        {
            var players = Main.gameStateManager.TheIsland?.netManager?.netPlayers.Where(x => x.playerId != -1);
            if (players != null)
            {
                IMGUIConsole.LogLine(string.Format("{0} Players: ", players.Count()));
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
            Client = 1 << 0,
            Server = 1 << 1,
            Both = Client | Server
        };

        public readonly bool isServer;
        public NetManager netManager;

        // Local player id
        public int whoAmI = -1;
        public NetPlayer[] netPlayers = new NetPlayer[World.MAX_PLAYERS];
        public int uniqueNetPlayers = 0;

        public double StartTime;

        private double dcTime = 0;

        public struct NetPlayer : INetSerializable
        {
            public int playerId = -1;
            public int peerId = -1; // -1 if client (we can't send messages to other clients, just to server

            public int latency = 0;

            public NetPlayer() { }

            public void Deserialize(NetDataReader reader)
            {
                playerId = reader.GetInt();
                peerId = -1;
                latency = 0;
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(playerId);
                latency = 0;
            }
        }

        public NetworkManager(bool isServer)
        {
            netManager = new NetManager(this);
            netManager.EnableStatistics = true;
            netManager.ChannelsCount = 4;
            this.isServer = isServer;

            Array.Fill(netPlayers, new NetPlayer());

            if (isServer)
            {
                // SimulateLatency seems to be pretty buggy. It'll sometimes just hold onto packets for a long time for no apparent reason.
                //netManager.SimulateLatency = true;
                //netManager.SimulationMaxLatency = 500;
                //netManager.SimulationMinLatency = 200;
            }
        }

        public void Connect()
        {
            StartTime = Main.Time;
            if (isServer)
            {
                netManager.Start(9050);
                netPlayers[0] = new NetPlayer
                {
                    playerId = 0,
                    peerId = -1,
                };
                uniqueNetPlayers += 1;
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
            uniqueNetPlayers = 0;
            netManager.DisconnectAll();
            Console.WriteLine("Disconnected");
        }

        public void PollEvents()
        {
            netManager.TriggerUpdate();
            netManager.PollEvents();

            if (!isServer)
            {
                if (netManager.ConnectedPeersCount == 0)
                {
                    if (Main.Time - dcTime > 5)
                    {
                        Disconnect();
                        Main.gameStateManager.SetGameState(Main.gameStateManager.MainMenu);
                    }
                }
                else dcTime = Main.Time;
            }
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
            for (int i = 0; i < netPlayers.Length; i++)
            {
                if (netPlayers[i].peerId == peer.Id)
                {
                    netPlayers[i].latency = latency;
                }
            }
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            Main.Registry.MessageRegistry.Dispatch(netManager, reader, peer, channelNumber, deliveryMethod);
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
                int index = -1;
                for (int i = 0; i < World.MAX_PLAYERS; i++)
                {
                    if (netPlayers[i].playerId == -1)
                    {
                        index = i;
                    }
                }

                netPlayers[index] = new NetPlayer
                {
                    playerId = index,
                    peerId = peer.Id,
                };
                uniqueNetPlayers += 1;

                Player p = new Player();
                p.playerIndex = index;
                // TODO
                p.FirstCreated(world.WorldInfo);
                world.EntityManager.Add(p);
                world.player[index] = p;
                world.ChunkLoadManager.LoadAroundTarget(world);
                // Inform peer of its id
                Main.Registry.MessageRegistry.SendMessageToPeer(WhoAmI.Instance, peer, index);
                // Inform peer of existant entities and ids, including its own Player
                Main.Registry.MessageRegistry.SendMessageToPeer(SyncPlayerConnected.Instance, peer, world.player.Where(x => x != null).ToArray());
                // Inform others of new entity and id
                Main.Registry.MessageRegistry.SendMessageToAll(SyncPlayerConnected.Instance, netManager, new Player[] { p }, peer);
                //Main.Registry.MessageRegistry.SendMessageToPeer(SyncAllWorldState.Instance, peer, netPlayers[index]);
                var sync = new SyncChunk.ChunkToSync
                {
                    chunkPosition = ChunkPosition.CubeChunk(world.GetLocalPlayer().SpawnPosition),
                };
                Main.Registry.MessageRegistry.SendMessageToPeer(SyncChunk.Instance, peer, sync);
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
                int index = -1; 
                for (int i = 0; i < World.MAX_PLAYERS; i++)
                {
                    if (netPlayers[i].peerId == peer.Id)
                        index = i;
                }
                int playerIndex = netPlayers[index].playerId;
                uniqueNetPlayers -= 1;

                Debug.Assert(world.localPlayerIndex != playerIndex);
                Debug.Assert(world.player[playerIndex] != null);
                Console.WriteLine("Peer {0} disconnected. Player id: {1}\nReason: {1}", peer, playerIndex, disconnectInfo.ToString());
                world.EntityManager.Remove(world.player[playerIndex]);
                world.player[playerIndex] = null;
                netPlayers[index] = new NetPlayer();
                Main.Registry.MessageRegistry.SendMessageToAll(SyncPlayerConnected.Instance, netManager, null);

                Main.gameStateManager.TheIsland.GetWorld().ChunkLoadManager.UnloadAllFor(playerIndex);
            }
            else
            {
                // Server has disconnected from us? We should go back to main menu.
                Console.WriteLine("Lost connection to server (Peer {0}).\nReason: {1}", peer, disconnectInfo.ToString());
                Main.gameStateManager.SetGameState(Main.gameStateManager.MainMenu);
            }
        }

        public NetPeer? GetPeer(NetPlayer player)
        {
            if (player.peerId == -1) return null;

            foreach (var peer in netManager.ConnectedPeerList)
            {
                if (peer.Id == player.peerId)
                {
                    return (NetPeer)peer;
                }
            }

            return null;
        }

        public NetPeer? GetPeer(int playerId)
        {
            foreach (var peer in netManager.ConnectedPeerList)
            {
                if (peer.Id == netPlayers[playerId].peerId)
                {
                    return (NetPeer)peer;
                }
            }

            return null;
        }
    }
}
