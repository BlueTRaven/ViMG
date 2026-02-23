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
        public SyncPlayerStats Instance;
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
                    GS.netManagerServer?.SendMessageToPeer(Instance, GS.netManagerServer.GetPeer(player.playerIndex), null);
                    var accumulatedStats = player.GetStats();
                }
            }
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = LiteNetLib.DeliveryMethod.ReliableUnordered;

            
        }
    }
}
