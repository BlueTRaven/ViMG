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

            netMessage.writer.Put(GS.netManager.netPlayers.Count);
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

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            GS.netManager.netPlayers.Clear();
            int numNetPlayers = reader.GetInt();
            for (int i = 0; i < numNetPlayers; i++)
                GS.netManager.netPlayers.Add(reader.Get<NetworkManager.NetPlayer>());
            GS.GetWorld().localPlayerIndex = GS.netManager.netPlayers[GS.netManager.whoAmI].playerId;

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
