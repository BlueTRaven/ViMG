using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.UIs;

namespace Engine.Networking.Messages
{
    public class SyncInventoryInput : Message
    {
        public static SyncInventoryInput Instance { get; private set; }

        public struct ClickToSync
        {
            public int player;
            public int entityId;
            public int inventoryId;
            public int inventoryIndex;
            public MenuHelper.ItemSlotClickOutput output;
        }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

        public SyncInventoryInput()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);


        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            // TODO
            //var clickToSync = new ClickToSync();

            //var player = GS.GetWorld().player[clickToSync.player];
            //if (player != null)
            //{
            //    var inventory = player.GetInventory(clickToSync.inventoryId);
            //    if (inventory != null)
            //    {
            //        inventory.AddClick
            //    }
            //}
        }
    }
}
