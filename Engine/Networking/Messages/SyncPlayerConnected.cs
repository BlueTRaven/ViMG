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

            netMessage.deliveryMethod = DeliveryMethod.ReliableOrdered;
            netMessage.writer.Put(World.MAX_PLAYERS);
            foreach (NetworkManager.NetPlayer player in GS.netManagerServer?.netPlayers)
            {
                netMessage.writer.Put(player);
            }

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            var old = GS.netManagerClient.netPlayers.ToArray();
            Array.Fill(GS.netManagerClient.netPlayers, new NetworkManager.NetPlayer());
            GS.netManagerClient.uniqueNetPlayers = 0;
            int numNetPlayers = reader.GetInt();
            for (int i = 0; i < numNetPlayers; i++)
            {
                var netPlayer = reader.Get<NetworkManager.NetPlayer>();
                if (netPlayer.playerId != -1)
                {
                    GS.netManagerClient.netPlayers[netPlayer.playerId] = netPlayer;
                    GS.netManagerClient.uniqueNetPlayers += 1;
                }
            }
            
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                if (old[i].playerId != -1 && GS.netManagerClient.netPlayers[i].playerId == -1)
                {
                    var curr = GS.GetClient().Current();
                    curr.entities.RemovePlayer(curr.entities.GetPlayerRef(old[i].playerId));
                }
            }
        }
    }
}
