using BepuPhysics.Constraints;
using Engine.Networking.Messages;
using Engine.UIs;
using Hexa.NET.ImGui;
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
    public class NetworkManager : INetEventListener, INatPunchListener
    {
        private static Engine.Logger Logger = Engine.Logger.InitLogger("NetworkManager", true, Engine.Logger.LogLevel.Warn);

        public const double TIME_TRAVEL_DELAY = 0;//0.75;// Main.FIXED_STEP * 3;

        [ConsoleCommand("list_players", "lists currently connected players", ConsoleCommandRunSide.Server)]
        public static void ListPlayers(string[] parameters)
        {
            var networkManager = GlobalState.GameStateManager.TheIsland?.netManagerServer != null ? GlobalState.GameStateManager.TheIsland?.netManagerServer : GlobalState.GameStateManager.TheIsland?.netManagerClient;
            var players = networkManager.netPlayers.Where(x => x.playerId != -1);
            if (players != null)
            {
                IMGUIConsole.LogLine(string.Format("{0} Players: ", players.Count()));
                foreach (NetPlayer player in players)
                {
                    if (GlobalState.GameStateManager.GetCurrentGameState() is GameStateTheIsland theIsland && theIsland.GetClient().LocalPlayerIndex == player.playerId)
                    {
                        IMGUIConsole.LogLine(string.Format("\t(You) Name: {0} Id: {1}", player.playerName, player.playerId));
                    }
                    else IMGUIConsole.LogLine(string.Format("\tName: {0} Id: {1}", player.playerName, player.playerId));
                }
            }
        }

        [ConsoleCommandVar("net_client_sim_latency", "Simulate latency on client. Use net_client_latency_min and net_client_latency_max to configure. Default: false.")]
        public static bool ClientSimLatency = false;

        [ConsoleCommandVar("net_server_sim_latency", "Simulate latency on server. Use net_server_latency_min and net_server_latency_max to configure. Default: false.")]
        public static bool ServerSimLatency = false;

        [ConsoleCommandVar("net_client_latency_min", "Minimum simulated client latency, in milliseconds. Default: 0.")]
        public static int ClientLatencyMin = 0;
        [ConsoleCommandVar("net_client_latency_max", "Maximum simulated client latency, in milliseconds. Default: 0.")]
        public static int ClientLatencyMax = 0;
        [ConsoleCommandVar("net_server_latency_min", "Minimum simulated server latency, in milliseconds. Default: 0.")]
        public static int ServerLatencyMin = 50;
        [ConsoleCommandVar("net_server_latency_max", "Maximum simulated server latency, in milliseconds. Default: 0.")]
        public static int ServerLatencyMax = 100;

        [Flags]
        public enum NetworkSide
        {
            None = 0,
            Client = 1 << 0,
            Server = 1 << 1,
            Both = Client | Server
        };

        private bool isServer;
        public bool IsServer => isServer;
        public bool IsClient => !isServer;
        public NetManager netManager;

        // Local player id
        public int whoAmI = -1;
        public NetPlayer[] netPlayers = new NetPlayer[World.MAX_PLAYERS];
        public int uniqueNetPlayers = 0;

        private bool startedConnecting;
        public double StartTime;
        private DateTime clientDCTime;

        public int Port = 9050;
        public string Ip = "localhost";

        public Statistics[] statistics = new Statistics[Main.FIXED_FPS];
        private ulong maxSent;
        private ulong maxRecieved;
        private double lastStatisticCheck;

        private double timeUpdateTime = 0;

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
            public bool isAdmin;

            public int latency = 0;
            public bool initialized = false;

            public NetPlayer() { }

            public void Deserialize(NetDataReader reader)
            {
                playerId = reader.GetInt();
                playerName = reader.GetString();
                isAdmin = reader.GetBool();
                latency = 0;
                peerId = -1;
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(playerId);
                writer.Put(playerName);
                writer.Put(isAdmin);
                latency = 0;
            }
        }

        public NetworkManager()
        {
            netManager = new NetManager(this);
            
            netManager.EnableStatistics = true;
            netManager.ChannelsCount = 4;
            netManager.NatPunchEnabled = false;
            netManager.NatPunchModule.Init(this);

#if DEBUG
            netManager.DisconnectTimeout = 120 * 1000;
#endif

            Array.Fill(netPlayers, new NetPlayer());

            netManager.UseNativeSockets = true;
            // SimulateLatency seems to be pretty buggy. It'll sometimes just hold onto packets for a long time for no apparent reason.
            //netManager.SimulateLatency = true;
            //netManager.SimulationMaxLatency = 500;
            //netManager.SimulationMinLatency = 200;
        }

        public void Connect(GameStateManager.NetworkingMode netMode)
        {
            isServer = netMode == GameStateManager.NetworkingMode.Server;

            StartTime = GlobalState.Time;
            if (IsServer)
            {
                netManager.Start(Port);
                Logger.Log(Engine.Logger.LogLevel.Info, "Started server on port {0}", Port);
            }
            else if (IsClient)
            {
                netManager.Start();
                if (netManager.NatPunchEnabled)
                    netManager.NatPunchModule.SendNatIntroduceRequest(Ip, Port, "");
                else netManager.Connect(Ip, Port, "");
                clientDCTime = DateTime.Now;
            }

            startedConnecting = true;
        }

        public bool ClientHasConnected()
        {
            if (!startedConnecting) return false;

            return whoAmI != -1;
        }

        public void CheckConnected()
        {
            Debug.Assert(whoAmI == -1);

            if (!startedConnecting) return;

            netManager.TriggerUpdate();
            netManager.PollEvents();

            if ((DateTime.Now - clientDCTime).TotalSeconds > 5)
            {
                Disconnect();
                GlobalState.GameStateManager.SetGameState(GlobalState.GameStateManager.MainMenu);
                GlobalState.GameStateManager.GetCurrentGameState().PushMenu(new MenuFailedToConnect(GlobalState.GameStateManager, MenuFailedToConnect.ConnectionFailureReason.Refused, Ip, Port));
                Logger.Log(Engine.Logger.LogLevel.Warn, "Client failed to receive whoami after 5 seconds. Could not connect.");
            }
        }

        public void Disconnect()
        {
            bool wasConnected = netManager.ConnectedPeersCount > 0;
            whoAmI = -1;
            uniqueNetPlayers = 0;
            Array.Fill(netPlayers, new NetPlayer());
            netManager.DisconnectAll();
            netManager.Stop();

            if (wasConnected)
                Logger.Log(Engine.Logger.LogLevel.Info, "Disconnected");
        }

        public void PollEvents()
        {
            if (IsServer)
            {
                if (ServerLatencyMin > ServerLatencyMax)
                    ServerLatencyMax = ServerLatencyMin;

                if (ServerLatencyMax < ServerLatencyMin)
                    ServerLatencyMin = ServerLatencyMax;

                netManager.SimulateLatency = ServerSimLatency;
                netManager.SimulationMinLatency = ServerLatencyMin;
                netManager.SimulationMaxLatency = ServerLatencyMax;
            }
            else
            {
                if (ClientLatencyMin > ClientLatencyMax)
                    ClientLatencyMax = ClientLatencyMin;

                if (ClientLatencyMax < ClientLatencyMin)
                    ClientLatencyMin = ClientLatencyMax;

                netManager.SimulateLatency = ClientSimLatency;
                netManager.SimulationMinLatency = ClientLatencyMin;
                netManager.SimulationMaxLatency = ClientLatencyMax;
            }
            
            netManager.TriggerUpdate();
            netManager.PollEvents();
            if (netManager.NatPunchEnabled)
                netManager.NatPunchModule.PollEvents();

            if (GlobalState.Time - lastStatisticCheck > 1)
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

                lastStatisticCheck = GlobalState.Time;
            }

            if (IsServer)
            {
                if (GlobalState.Time - timeUpdateTime > 0.125)
                {
                    SendMessageToAll(WhoAmI.Instance, netManager, null);
                    timeUpdateTime = GlobalState.Time;
                }
            }

            if (IsClient)
            {
                if (netManager.ConnectedPeersCount == 0)
                {
                    if ((DateTime.Now - clientDCTime).TotalSeconds > 5)
                    {
                        Disconnect();
                        GlobalState.GameStateManager.SetGameState(GlobalState.GameStateManager.MainMenu);
                    }
                }
                else clientDCTime = DateTime.Now;
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
            GlobalState.Registry.MessageRegistry.Dispatch(this, reader, peer, channelNumber, deliveryMethod);
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
                    Logger.Log(Engine.Logger.LogLevel.Info, "Connected to server at {0}.", peer);
                    SendMessageToAll(WhoAmIRequest.Instance, netManager, null);
                }
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (IsServer)
            {
                var world = GlobalState.GameStateManager.TheIsland.GetWorld();
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

                // This just asserts that the local player has not disconnected - not necessary
                //IMGUIConsole.Assert(world.localPlayerIndex != playerIndex);
                IMGUIConsole.Assert(world.player[playerIndex] != null);
                Logger.Log(Engine.Logger.LogLevel.Info, "Peer {0} disconnected. Player id: {1}\nReason: {2}", peer, playerIndex, disconnectInfo.Reason.ToString());
                SyncEntityState.Instance.PlayerDisconnected(index);
                SyncInventory.Instance.PlayerDisconnected(index);
                GlobalState.GameStateManager.TheIsland.playerIO?.Serialize(world, playerIndex);
                world.EntityManager.Unload(world.player[playerIndex]);
                world.player[playerIndex] = null;
                netPlayers[index] = new NetPlayer();
                SendMessageToAll(SyncPlayerConnected.Instance, netManager, null);

                GlobalState.GameStateManager.TheIsland.GetWorld().ChunkLoadManager.UnloadAllFor(playerIndex);
            }
            else
            {
                // Server has disconnected from us? We should go back to main menu.
                Logger.Log(Engine.Logger.LogLevel.Error, "Lost connection to server (Peer {0}).\nReason: {1}", peer, disconnectInfo.Reason.ToString());
                GlobalState.GameStateManager.SetGameState(GlobalState.GameStateManager.MainMenu);
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

        public NetPlayer GetNetPlayer(int id)
        {
            return netPlayers[id];
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
            if (name == "self" && whoAmI != -1)
            {
                return netPlayers[whoAmI];
            }

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
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            var world = GlobalState.GameStateManager.TheIsland.GetWorld();
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

            Logger.Log(Engine.Logger.LogLevel.Info, "New Player {0}: {1} connected from {2}", playerName, index, peer);
            ViMG.TracyImpl.Tracy.EmitMessage(string.Format("NewPlayer {0}", playerName));

            SendMessageToPeer(WhoAmI.Instance, peer, index);
            GlobalState.GameStateManager.TheIsland.playerIO?.Deserialize(world, PlayerManagerIO.GetHashCodeForName(playerName), index);
            world.ChunkLoadManager.LoadAroundTarget(world);
            // Inform others of new player
            SendMessageToAll(SyncPlayerConnected.Instance, netManager, null);
        }

        public void SendMessageToPeer(Message message, NetPeer peer, object? addData)
        {
            ViMG.TracyImpl.Tracy.EmitMessage(string.Format("[{0}] Send Message {1}", IsServer ? "Server" : "Client", message.GetType().FullName));

            if (IsServer)
            {
                IMGUIConsole.Assert((message.SendableFrom & NetworkSide.Server) == NetworkManager.NetworkSide.Server);
            }
            else if (IsClient)
            {
                IMGUIConsole.Assert((message.SendableFrom & NetworkSide.Client) == NetworkManager.NetworkSide.Client);
            }

            NetworkMessage netMessage = new NetworkMessage(message.Id, netManager, peer);
            netMessage.writer.Put(message.Id);

            message.SendMessage(netMessage, addData);
        }

        // TODO: remove netManager, use field
        public void SendMessageToAll(Message message, NetManager netManager, object? addData, NetPeer? excludePeer = null)
        {
            NetworkMessage netMessage = new NetworkMessage(message.Id, netManager, null);
            netMessage.excludePeer = excludePeer;
            netMessage.writer.Put(message.Id);

            message.SendMessage(netMessage, addData);
        }

        //public void SendMessageToAll<T>(Message message, T addData, NetPeer? excludePeer = null)
        //{
        //    NetworkMessage netMessage = new NetworkMessage(message.Id, netManager, null);
        //    netMessage.excludePeer = excludePeer;
        //    netMessage.writer.Put(message.Id);

        //    message.SendMessage(netMessage, addData);
        //}

        public void IMGUIDebug()
        {
            float[] sent = new float[statistics.Length - 1];
            float[] received = new float[statistics.Length - 1];
            for (int i = 0; i < statistics.Length - 1; i++)
            {
                sent[i] = (float)(statistics[i].BytesSent - statistics[i + 1].BytesSent) / 10000.0f;
                received[i] = (float)(statistics[i].BytesReceived - statistics[i + 1].BytesReceived) / 10000.0f;
            }
            ImGui.PlotLines("Bytes Sent", ref sent[0], statistics.Length, (string)null, (float)(maxSent) / 10000.0f);
            ImGui.PlotLines("Bytes Recieved", ref received[0], statistics.Length, (string)null, (float)(maxRecieved) / 10000.0f);
        }

        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            if (IsServer)
            {
                netManager.NatPunchModule.NatIntroduce(localEndPoint, remoteEndPoint, localEndPoint, remoteEndPoint, token);
            }
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            if (IsClient)
            {
                netManager.Connect(targetEndPoint, "");
            }
        }
    }
}
