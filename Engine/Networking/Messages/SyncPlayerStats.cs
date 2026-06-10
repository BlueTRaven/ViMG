using Engine.Entities;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    public class SyncPlayerStats : Message
    {
        public static Logger Logger = Logger.InitLogger("SyncPlayerStats", true, Logger.LogLevel.Info);
        public static SyncPlayerStats Instance;
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public SyncPlayerStats()
        {
            Instance = this;
        }

        public void SendMessage(World world)
        {
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                Player? player = world.player[i];

                if (player != null)
                {
                    GS.netManagerServer?.SendMessageToPeer(Instance, GS.netManagerServer.GetPeer(player.playerIndex), player.GetStats());
                }
            }
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = LiteNetLib.DeliveryMethod.ReliableUnordered;

            var accumulatedStats = addData as PlayerAccumulatedStats? ?? throw new Exception();

            netMessage.writer.Put(SyncWorldState.Instance.ServerSequence);
            netMessage.writer.Put(accumulatedStats);
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            int sequence = reader.GetInt();
            if (sequence < SyncWorldState.Instance.ClientSequence)
            {
                Logger.Warn("SyncPlayerStats arrived old");
                return;
            }

            var stats = reader.Get<PlayerAccumulatedStats>();

            GS.GetClient().Current().localPlayerStats = stats;
        }
    }
}
