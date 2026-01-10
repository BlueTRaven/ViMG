using Engine.Items;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.UIs;

namespace Engine.Networking.Messages
{
    public class SyncInventoryInput : Message
    {
        public static SyncInventoryInput Instance { get; private set; }

        public struct ClickToSync
        {
            public byte playerId;
            // TODO: entity reference here instead
            public EntityManager.EntityReference entity;
            public InventoryManager.InventoryReference inventory;
            // Index within the inventory that we clicked
            public int inventoryIndex;
            public int action;
            public MenuHelper.ItemSlotClickOutput output;
        }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

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

            netMessage.writer.Put((byte)clickToSync.playerId);
            netMessage.writer.Put(clickToSync.entity);
            clickToSync.inventory.Serialize(netMessage.writer);
            netMessage.writer.Put(clickToSync.inventoryIndex);
            netMessage.writer.Put(clickToSync.action);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            var playerId = reader.GetByte();
            var entityRef = reader.Get<EntityManager.EntityReference>();
            var inventoryRef = InventoryManager.InventoryReference.Deserialize(reader);
            var inventoryIndex = reader.GetInt();
            var action = reader.GetInt();

            var clickToSync = new ClickToSync
            {
                playerId = playerId,
                entity = entityRef,
                inventory = inventoryRef,
                inventoryIndex = inventoryIndex,
                action = action,
            };

            var player = GS.GetWorld().player[clickToSync.playerId];
            if (player != null)
            {
                var entity = GS.GetWorld().EntityManager.GetByRefServer(ref clickToSync.entity);
                var inventory = GS.GetWorld().InventoryManager.Get(clickToSync.inventory);
                if (inventory != null && entity != null && entity is IHasInventory hasInv)
                {
                    if (clickToSync.inventoryIndex >= 0)
                    {
                        Console.WriteLine("Remote Inventory Input: {0:02} {1} {2} {3} ", Main.Time, player.ToString(), entity.ToString(), clickToSync.inventory.id);
                        MenuHelper.DoClick(player, inventory, GS.GetWorld().InventoryManager.Get(player.heldInventory), clickToSync.inventoryIndex, false);
                    }

                    if (clickToSync.action > 0)
                    {
                        Console.WriteLine("Remove Inventory Input: Do Action {0}", clickToSync.action);
                        hasInv.InventoryAction(player.playerIndex, clickToSync.action);
                    }
                }
            }
        }
    }
}
