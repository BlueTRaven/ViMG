using BepuPhysics.Constraints;
using Engine.Networking.Messages;
using ImGuiNET;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;
using ViMG.IMGUIImpl;

namespace Engine.Networking
{
    public class NetworkManager : INetEventListener
    {
        public const double TIME_TRAVEL_DELAY = 0;//0.75;// Main.FIXED_STEP * 3;

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

        public bool IsServer => Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Server;
        public bool IsClient => Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Client;
        public NetManager netManager;

        // Local player id
        public int whoAmI = -1;
        public NetPlayer[] netPlayers = new NetPlayer[World.MAX_PLAYERS];
        public int uniqueNetPlayers = 0;

        public double StartTime;

        public int Port = 9050;
        public string Ip = "localhost";

        public Statistics[] statistics = new Statistics[Main.FIXED_FPS];
        private ulong maxSent;
        private ulong maxRecieved;
        private double lastStatisticCheck;

        private double timeUpdateTime = 0;
        private double dcTime = 0;

        public struct Statistics
        {
            public ulong BytesSent;
            public ulong BytesReceived;
        }
        public struct NetPlayer : INetSerializable
        {
            public int playerId = -1;
            public int peerId = -1; // -1 if client (we can't send messages to other clients, just to server

            public string playerName;

            public int latency = 0;
            public bool initialized = false;

            public NetPlayer() { }

            public void Deserialize(NetDataReader reader)
            {
                playerId = reader.GetInt();
                playerName = reader.GetString();
                latency = 0;
                peerId = -1;
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(playerId);
                writer.Put(playerName);
                latency = 0;
            }
        }

        public NetworkManager()
        {
            netManager = new NetManager(this);
            
            netManager.EnableStatistics = true;
            netManager.ChannelsCount = 4;
            netManager.NatPunchEnabled = true;

            Array.Fill(netPlayers, new NetPlayer());

            if (IsServer)
            {
                netManager.UseNativeSockets = true;
                // SimulateLatency seems to be pretty buggy. It'll sometimes just hold onto packets for a long time for no apparent reason.
                //netManager.SimulateLatency = true;
                //netManager.SimulationMaxLatency = 500;
                //netManager.SimulationMinLatency = 200;
            }
        }

        public void Connect(GameStateManager.NetworkingMode netMode)
        {
            StartTime = Main.Time;
            if (IsServer)
            {
                netManager.Start(Port);
                netPlayers[0] = new NetPlayer
                {
                    playerId = 0,
                    peerId = -1,
                    playerName = Main.gameStateManager.TheIsland.localPlayerName,
                };
                uniqueNetPlayers += 1;
                Console.WriteLine("Started server on port 9050");
            }
            else if (IsClient)
            {
                netManager.Start();
                netManager.Connect(Ip, Port, "");
                var dcTime = DateTime.Now;

                while (whoAmI == -1)
                {
                    netManager.TriggerUpdate();
                    netManager.PollEvents();

                    if ((DateTime.Now - dcTime).TotalSeconds > 5)
                    {
                        Disconnect();
                        return;
                    }
                }
                Console.WriteLine("Connected to server. Our id: {0}", whoAmI);
            }
        }

        public void Disconnect()
        {
            whoAmI = -1;
            uniqueNetPlayers = 0;
            Array.Fill(netPlayers, new NetPlayer());
            netManager.DisconnectAll();

            if (netManager.ConnectedPeersCount != 0)
                Console.WriteLine("Disconnected");
        }

        public void PollEvents()
        {
            netManager.TriggerUpdate();
            netManager.PollEvents();

            if (Main.Time - lastStatisticCheck > 1)
            {
                for (int i = statistics.Length - 1; i >= 1; i--)
                {
                    statistics[i] = statistics[i - 1];
                }
                statistics[0] = new Statistics
                {
                    BytesReceived = (ulong)netManager.Statistics.BytesReceived,
                    BytesSent = (ulong)netManager.Statistics.BytesSent,
                };

                maxSent = ulong.Max(statistics[0].BytesSent, maxSent);
                maxRecieved = ulong.Max(statistics[0].BytesReceived, maxRecieved);

                lastStatisticCheck = Main.Time;
            }

            if (IsServer)
            {
                if (Main.Time - timeUpdateTime > 0.125)
                {
                    Main.Registry.MessageRegistry.SendMessageToAll(WhoAmI.Instance, netManager, null);
                    timeUpdateTime = Main.Time;
                }
            }

            if (IsClient)
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
            request.AcceptIfKey("");
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
            lock (this)
            {
                if (IsServer)
                {
                }
                else
                {
                    Console.WriteLine("Connected to server at {0}.", peer);
                    Main.Registry.MessageRegistry.SendMessageToAll(WhoAmIRequest.Instance, netManager, null);
                }
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (IsServer)
            {
                var world = Main.gameStateManager.TheIsland.GetWorld();
                int index = -1; 
                for (int i = 0; i < World.MAX_PLAYERS; i++)
                {
                    if (netPlayers[i].peerId == peer.Id)
                        index = i;
                }

                // Not a fully connected peer
                // This is valid and can happen if we have an error when receiving WhoAmIRequest
                if (index == -1) return;

                int playerIndex = netPlayers[index].playerId;
                uniqueNetPlayers -= 1;

                IMGUIConsole.Assert(world.localPlayerIndex != playerIndex);
                IMGUIConsole.Assert(world.player[playerIndex] != null);
                Console.WriteLine("Peer {0} disconnected. Player id: {1}\nReason: {2}", peer, playerIndex, disconnectInfo.Reason.ToString());
                Main.gameStateManager.TheIsland.playerIO?.Serialize(world, playerIndex);
                world.EntityManager.Unload(world.player[playerIndex]);
                world.player[playerIndex] = null;
                netPlayers[index] = new NetPlayer();
                Main.Registry.MessageRegistry.SendMessageToAll(SyncPlayerConnected.Instance, netManager, null);

                Main.gameStateManager.TheIsland.GetWorld().ChunkLoadManager.UnloadAllFor(playerIndex);
            }
            else
            {
                // Server has disconnected from us? We should go back to main menu.
                Console.WriteLine("Lost connection to server (Peer {0}).\nReason: {1}", peer, disconnectInfo.Reason.ToString());
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

        public NetPlayer GetNetPlayer(NetPeer peer)
        {
            for (int i = 0; i < netPlayers.Length; i++)
            {
                if (netPlayers[i].peerId == peer.Id) return netPlayers[i];
            }

            return new NetPlayer();
        }

        public NetPlayer GetNetPlayerByName(string name)
        {
            foreach (var nplayer in netPlayers)
            {
                if (nplayer.playerName == name)
                {
                    return nplayer;
                }
            }
            return new NetPlayer();
        }

        public void NewPlayer(NetPeer peer, string playerName)
        {
            var world = Main.gameStateManager.TheIsland.GetWorld();
            int index = -1;
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                if (netPlayers[i].playerId == -1)
                {
                    index = i;
                    break;
                }
            }

            netPlayers[index] = new NetPlayer
            {
                playerId = index,
                peerId = peer.Id,
                playerName = playerName,
            };
            uniqueNetPlayers += 1;

            Main.Registry.MessageRegistry.SendMessageToPeer(WhoAmI.Instance, peer, index);
            Main.gameStateManager.TheIsland.playerIO?.Deserialize(world, PlayerManagerIO.GetHashCodeForName(playerName), index);
            world.ChunkLoadManager.LoadAroundTarget(world);
            // Inform others of new player
            Main.Registry.MessageRegistry.SendMessageToAll(SyncPlayerConnected.Instance, netManager, null);
            var sync = new SyncChunk.ChunkToSync
            {
                chunkPosition = ChunkPosition.WorldSpaceChunk(world.WorldInfo.spawnPosition),
            };
            Main.Registry.MessageRegistry.SendMessageToPeer(SyncChunk.Instance, peer, sync);

            Console.WriteLine("Peer connected from {0}. {1} {2} {3}", peer, netPlayers[index].playerId, netPlayers[index].playerName, netPlayers[index].playerName.GetHashCode());
        }

        public void IMGUIDebug()
        {
            float[] sent = new float[statistics.Length - 1];
            float[] received = new float[statistics.Length - 1];
            for (int i = 0; i < statistics.Length - 1; i++)
            {
                sent[i] = (float)(statistics[i].BytesSent - statistics[i + 1].BytesSent) / 10000.0f;
                received[i] = (float)(statistics[i].BytesReceived - statistics[i + 1].BytesReceived) / 10000.0f;
            }
            ImGui.PlotLines("Bytes Sent", ref sent[0], statistics.Length, 0, null, 0, (float)(maxSent) / 10000.0f, new(0, 80));
            ImGui.PlotLines("Bytes Recieved", ref received[0], statistics.Length, 0, null, 0, (float)(maxRecieved) / 10000.0f, new(0, 80));
        }
    }
}
