using BrUtility;
using Engine.Items;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace Engine.Networking.Messages
{
    public class SyncInventory : Message
    {
        public const int MAX_INVS_PER_SYNC = 32;

        public static SyncInventory Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        private struct SyncedInventory
        {
            public InventoryManager.InventoryReference reference;
            public int latestSequence;
        }

        private enum ToSyncType
        {
            Update,
            Unload,
        }

        private struct ToSync
        {
            public ToSyncType type;
            public Inventory? inventory;
            public InventoryManager.InventoryReference reference;
        }

        private SyncedInventory[][] serverInventories;
        private SyncedInventory[] clientInventories;

        private readonly FastList<ToSync> toSync;

        private int serverSequence;
        private int clientSequence;

        public SyncInventory()
        {
            Instance = this;

            toSync = new FastList<ToSync>(InventoryManager.InvMax);

            serverInventories = new SyncedInventory[World.MAX_PLAYERS][];
            clientInventories = new SyncedInventory[InventoryManager.InvMax];
            for (int i = 0; i < serverInventories.Length; i++)
            {
                serverInventories[i] = new SyncedInventory[InventoryManager.InvMax];
                for (int j = 0; j < InventoryManager.InvMax; j++)
                {
                    serverInventories[i][j] = new() { reference = InventoryManager.InventoryReference.INVALID, latestSequence = -1 };
                }
            }
            for (int i = 0; i < EntityManager.EntMax; i++)
                clientInventories[i] = new() { reference = InventoryManager.InventoryReference.INVALID, latestSequence = -1 };
        }

        public void AddAck(SyncInventoryAck.Ack ack, int playerId, int sequence)
        {
            for (int i = 0; i < ack.numAckd; i++)
            {
                Console.WriteLine("Ack for Inv {0} {1} {2}", playerId, ack.ackedInvs[i].id, sequence);
                serverInventories[playerId][ack.ackedInvs[i].id - 1].reference = ack.ackedInvs[i];
                // Note we blindly set the sequence here; earlier we discard sequences that are not the latest, so this should work fine
                serverInventories[playerId][ack.ackedInvs[i].id - 1].latestSequence = sequence;
            }
        }

        public void DoSync(InventoryManager inventoryManager, Player[] players)
        {
            foreach (Player player in players)
            {
                if (player == null || !player.IsInitialized)
                    continue;

                var peer = GS.netManagerServer?.GetPeer(player.playerIndex);

                if (peer == null)
                {
                    Console.WriteLine("Peer null");
                    continue;
                }

                for (int i = 0; i < InventoryManager.InvMax; i++)
                {
                    var reference = inventoryManager.GetReference(i);

                    var inv = inventoryManager.Get(reference);

                    if (serverInventories[player.playerIndex][i].reference.generation != reference.generation)
                    {
                        Console.WriteLine("Server sent create Inventory {0}", reference.id);
                        toSync.AddAssumeCapacity(new ToSync
                        {
                            inventory = inv,
                            reference = reference,
                            type = inv == null ? ToSyncType.Unload : ToSyncType.Update,
                        });
                    }
                }

                GS.netManagerServer?.SendMessageToPeer(Instance, peer, player.playerIndex);
                toSync.Clear();
            }

            serverSequence += 1;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            int playerIndex = addData as int? ?? throw new Exception();

            netMessage.deliveryMethod = DeliveryMethod.Unreliable;
            netMessage.channel = (int)NetworkMessage.Channels.Entities;

            int playerId = addData as int? ?? throw new Exception();

            netMessage.writer.Put(serverSequence);

            var fragHelper = new FragHelper(netMessage);

            foreach (var invToSync in toSync.Slice())
            {
                var subwriter = fragHelper.StartFragmentable();
                subwriter.Put((byte)invToSync.type);
                Inventory? inv = invToSync.inventory;
                var reference = invToSync.reference;

                if (inv?.id == 0)
                {
                    Console.Write("");
                }

                reference.Serialize(subwriter);

                if (invToSync.type == ToSyncType.Update)
                {
                    Debug.Assert(inv != null);

                    List<byte> saveBytes = new List<byte>();
                    inv.Save(saveBytes);
                    subwriter.PutBytesWithLength(saveBytes.ToArray());
                }

                fragHelper.EndFragmentable();
            }


            fragHelper.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            int seq = reader.GetInt();
            // drop packet
            if (seq < clientSequence) return;
            clientSequence = seq;

            int num = reader.GetUShort();

            SyncInventoryAck.InventoryAckArr ackArr = new();
            int ackI = 0;

            for (int i = 0; i < num; i++)
            {
                ToSyncType type = (ToSyncType)reader.GetByte();

                var reference = InventoryManager.InventoryReference.Deserialize(reader);

                Inventory? inv = null;
                if (type == ToSyncType.Update)
                {
                    int index = 0;
                    byte[] bytes = reader.GetBytesWithLength();
                    inv = Inventory.ClientLoad(bytes, ref index);
                }

                GS.GetClient().inventoryManager.Set(reference, inv);

                clientInventories[reference.id] = new()
                {
                    latestSequence = seq,
                    reference = reference,
                };
                ackArr[ackI] = reference;
                ackI++;
            }

            if (ackI > 0)
                GS.netManagerClient?.SendMessageToPeer(SyncInventoryAck.Instance, peer, new SyncInventoryAck.Ack { numAckd = ackI, ackedInvs = ackArr, sequence = seq });
        }
    }

    public class SyncInventoryAck : Message
    {
        public static SyncInventoryAck Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

        [InlineArray(SyncInventory.MAX_INVS_PER_SYNC)]
        public struct InventoryAckArr
        {
            private InventoryManager.InventoryReference _invId0;

            public InventoryManager.InventoryReference this[int i]
            {
                get => this[i];
                set => this[i] = value;
            }
        }

        public struct Ack : INetSerializable
        {
            public int numAckd;
            public InventoryAckArr ackedInvs;
            public int sequence;

            public void Deserialize(NetDataReader reader)
            {
                sequence = reader.GetInt();
                numAckd = reader.GetInt();

                for (int i = 0; i < numAckd; i++)
                {
                    ackedInvs[i] = InventoryManager.InventoryReference.Deserialize(reader);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(sequence);
                writer.Put(numAckd);

                for (int i = 0; i < numAckd; i++)
                {
                    ackedInvs[i].Serialize(writer);
                }
            }
        }

        public SyncInventoryAck()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            Ack ack = addData as Ack? ?? throw new Exception();

            netMessage.writer.Put(ack);

            netMessage.channel = (int)NetworkMessage.Channels.Entities;
            netMessage.deliveryMethod = DeliveryMethod.Unreliable;

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            Ack ack = reader.Get<Ack>();

            SyncInventory.Instance.AddAck(ack, GS.netManagerServer.GetNetPlayer(peer).playerId, ack.sequence);
        }
    }
}
