using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.UIs;
using static Engine.Networking.Messages.SyncInventoryInput;

namespace Engine.Networking.Messages
{
    public class SyncInventoryInput : Message
    {
        public static SyncInventoryInput Instance { get; private set; }

        public struct ClickToSync
        {
            public byte player;
            public ulong entityId;
            public int inventoryId;
            public int inventoryIndex;
            public MenuHelper.ItemSlotClickOutput output;
        }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Both;

        public SyncInventoryInput()
        {
            Instance = this;
            Passthrough = true;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            var clickToSync = addData as ClickToSync? ?? throw new Exception();

            netMessage.writer.Put((byte)clickToSync.player);
            netMessage.writer.Put(clickToSync.entityId);
            netMessage.writer.Put(clickToSync.inventoryId);
            netMessage.writer.Put(clickToSync.inventoryIndex);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            var playerId = reader.GetByte();
            var entityId = reader.GetULong();
            var inventoryId = reader.GetInt();
            var inventoryIndex = reader.GetInt();

            var clickToSync = new ClickToSync
            {
                player = playerId,
                entityId = entityId,
                inventoryId = inventoryId,
                inventoryIndex = inventoryIndex,
            };

            var player = GS.GetWorld().player[clickToSync.player];
            if (player != null)
            {
                var inventory = player.GetInventory(clickToSync.inventoryId);
                if (inventory != null)
                {
                    MenuHelper.DoClick(player, player.GetInventory(), player.GetHeldInventory(), clickToSync.inventoryIndex, false);
                }
            }
        }
    }
}
