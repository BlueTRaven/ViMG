using Engine.Items;
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
            public int action;
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
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;
            netMessage.channel = (int)NetworkMessage.Channels.Inputs;

            var clickToSync = addData as ClickToSync? ?? throw new Exception();

            netMessage.writer.Put((byte)clickToSync.player);
            netMessage.writer.Put(clickToSync.entityId);
            netMessage.writer.Put(clickToSync.inventoryId);
            netMessage.writer.Put(clickToSync.inventoryIndex);
            netMessage.writer.Put(clickToSync.action);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            var playerId = reader.GetByte();
            var entityId = reader.GetULong();
            var inventoryId = reader.GetInt();
            var inventoryIndex = reader.GetInt();
            var action = reader.GetInt();

            var clickToSync = new ClickToSync
            {
                player = playerId,
                entityId = entityId,
                inventoryId = inventoryId,
                inventoryIndex = inventoryIndex,
                action = action,
            };

            var player = GS.GetWorld().player[clickToSync.player];
            if (player != null)
            {
                var entity = GS.GetWorld().EntityManager.GetById(clickToSync.entityId);
                var inventory = player.GetInventory(clickToSync.inventoryId);
                if (inventory != null && entity != null && entity is IHasInventory hasInv)
                {
                    Console.WriteLine("Remote Inventory Input: {0:02} {1} {2} {3} ", Main.Time, player.ToString(), entity.ToString(), clickToSync.inventoryId);
                    MenuHelper.DoClick(player, hasInv.GetInventory(clickToSync.inventoryId), player.GetHeldInventory(), clickToSync.inventoryIndex, false);

                    if (clickToSync.action > 0)
                    {
                        Console.WriteLine("Remove Inventory Input: Do Action {0}", clickToSync.action);
                        hasInv.InventoryAction(player, clickToSync.action);
                    }
                }
            }
        }
    }
}
