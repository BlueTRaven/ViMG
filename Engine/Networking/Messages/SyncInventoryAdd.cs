using BrUtility;
using Engine.Items;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    public class SyncInventoryAdd : Message
    {
        public static SyncInventoryAdd Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public struct SendMessageParams
        {
            public FastList<Inventory> inventories;
            public FastList<InventoryManager.InventoryReference> references;
        }

        public SyncInventoryAdd()
        {
            Instance = this;
        }

        public void DoSync(FastList<Inventory> inventories, FastList<InventoryManager.InventoryReference> references)
        {
            Debug.Assert(inventories.Length == references.Length);
            SendMessageParams p = new SendMessageParams
            {
                inventories = inventories,
                references = references,
            };
            GS.netManagerServer.SendMessageToAll(this, GS.netManagerServer.netManager, p);
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            SendMessageParams p = addData as SendMessageParams? ?? throw new Exception();

            netMessage.deliveryMethod = LiteNetLib.DeliveryMethod.ReliableOrdered;

            netMessage.writer.Put(p.inventories.Length);

            for (int i = 0; i < p.inventories.Length; i++)
            {
                Inventory inv = p.inventories[i];
                InventoryManager.InventoryReference reference = p.references[i];

                reference.Serialize(netMessage.writer);

                List<byte> saveBytes = new List<byte>();
                inv.Save(saveBytes);
                netMessage.writer.PutBytesWithLength(saveBytes.ToArray());
            }
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            int num = reader.GetInt();

            for (int i = 0; i < num; i++)
            {
                var reference = InventoryManager.InventoryReference.Deserialize(reader);
                Console.WriteLine("Recv new inventory {0}", reference);
                int index = 0;
                byte[] bytes = reader.GetBytesWithLength();
                Inventory inv = Inventory.ClientLoad(bytes, ref index);
                GS.GetClient().inventoryManager.Set(reference, inv);
            }
        }
    }

    public class SyncInventoryRemove : Message
    {
        public static SyncInventoryRemove Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        private struct SendMessageParams
        {
            public FastList<InventoryManager.InventoryReference> references;
        }

        public SyncInventoryRemove()
        {
            Instance = this;
        }

        public void DoSync(FastList<InventoryManager.InventoryReference> references)
        {
            SendMessageParams p = new SendMessageParams
            {
                references = references,
            };
            GS.netManagerServer.SendMessageToAll(this, GS.netManagerServer.netManager, p);
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = LiteNetLib.DeliveryMethod.ReliableOrdered;

            SendMessageParams p = (addData as SendMessageParams?) ?? throw new Exception();

            netMessage.writer.Put(p.references.Length);

            for (int i = 0; i < p.references.Length; i++)
            {
                InventoryManager.InventoryReference reference = p.references[i];
                Console.WriteLine("Recv del inventory {0}", reference);

                reference.Serialize(netMessage.writer);
            }
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            int num = reader.GetInt();

            for (int i = 0; i < num; i++)
            {
                var reference = InventoryManager.InventoryReference.Deserialize(reader);
 
                GS.GetClient().inventoryManager.Set(reference, null);
            }
        }
    }
}
