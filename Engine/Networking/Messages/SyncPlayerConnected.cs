using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using static Engine.Networking.NetworkManager;

namespace Engine.Networking.Messages
{
    public class SyncPlayerConnected : Message
    {
        // The first time SyncPlayerConnected, it is sent with addData = the new net player id, our id.
        public const int ONLY_SYNC = -1;

        public static SyncPlayerConnected Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;
        public SyncPlayerConnected()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;
            netMessage.writer.Put(World.MAX_PLAYERS);
            foreach (NetworkManager.NetPlayer player in GS.netManager.netPlayers)
            {
                netMessage.writer.Put(player);
            }

            var players = addData as Player[];
            if (players != null)
            {
                //int numPlayers = GS.GetWorld().player.Where(x => x != null).Count();
                netMessage.writer.Put(players.Length);
                Console.WriteLine("SyncPlayerConnected: write numPlayers {0}", players.Length);
                foreach (Player player in players)
                {
                    netMessage.writer.Put(player.playerIndex);
                    Console.WriteLine("SyncPlayerConnected: write index {0}", player.playerIndex);

                    var playerEntityData = new EntityManagerIO.EntityData(player);
                    List<byte> bytes = new List<byte>();
                    playerEntityData.Save(bytes);
                    netMessage.writer.PutArray(bytes.ToArray(), sizeof(byte));
                }
            }
            else netMessage.writer.Put((int)0);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            var old = GS.netManager.netPlayers.ToArray();
            Array.Fill(GS.netManager.netPlayers, new NetworkManager.NetPlayer());
            GS.netManager.uniqueNetPlayers = 0;
            int numNetPlayers = reader.GetInt();
            for (int i = 0; i < numNetPlayers; i++)
            {
                var netPlayer = reader.Get<NetworkManager.NetPlayer>();
                if (netPlayer.playerId != -1)
                {
                    GS.netManager.netPlayers[netPlayer.playerId] = netPlayer;
                    GS.netManager.uniqueNetPlayers += 1;
                }
            }
            GS.GetWorld().localPlayerIndex = GS.netManager.whoAmI;
            
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                if (old[i].playerId != -1 && GS.netManager.netPlayers[i].playerId == -1)
                {
                    var disconnectedPlayer = GS.GetWorld().player[old[i].playerId];
                    if (disconnectedPlayer != null)
                        GS.GetWorld().EntityManager.Remove(disconnectedPlayer);
                }
            }

            // NOTE: numPlayers != numNetPlayers. We always sync netPlayers, whereas we only send
            // the new Players.
            int numPlayers = reader.GetInt();
            Console.WriteLine("SyncPlayerConnected: write numPlayers {0}", numPlayers);
            for (int i = 0; i < numPlayers; i++)
            {
                int playerIndex = reader.GetInt();
                Console.WriteLine("SyncPlayerConnected: read index {0}", playerIndex);

                byte[] bytes = reader.GetArray<byte>(sizeof(byte));
                EntityManagerIO.EntityData data = new();
                data.Load(bytes);
                if (data.IsValid)
                {
                    Player p = new Player();
                    p.playerIndex = playerIndex;
                    p.OnLoad(data.data, data.version);

                    GS.GetWorld().EntityManager.ForceAdd(p, data.id);
                    GS.GetWorld().player[playerIndex] = p;

                    if (playerIndex == GS.GetWorld().localPlayerIndex)
                    {
                        GS.GetWorld().ChunkLoadManager.LoadAroundTarget(GS.GetWorld());
                    }
                }
            }
        }
    }
}
