using Engine.Items;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.Items;
using static Engine.Networking.Messages.SyncCubeUpdateAuditResponse;
using static ViMG.UIs.UI;

namespace Engine.Networking.Messages
{
    public class SyncInventoryUpdate : Message
    {
        public static SyncInventoryUpdate Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public struct QueuedInventoryUpdate
        {
            public ulong entityId;
            public int inventoryId;
            public int inventoryIndex;
            public ItemInstance oldInstance, newInstance;
            public double time;
        }
        private List<QueuedInventoryUpdate> queued1 = new();
        private List<QueuedInventoryUpdate> queued2 = new();
        private List<QueuedInventoryUpdate> queued;

        public SyncInventoryUpdate()
        {
            Instance = this;
            queued = queued1;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            var action = addData as QueuedInventoryUpdate? ?? throw new Exception();
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;

            netMessage.writer.Put(action.entityId);
            netMessage.writer.Put((byte)action.inventoryId);
            netMessage.writer.Put((ushort)action.inventoryIndex);
            netMessage.writer.Put(action.oldInstance.item?.Id ?? 0);
            netMessage.writer.Put(action.oldInstance.num);
            netMessage.writer.Put(action.oldInstance.damage);
            netMessage.writer.Put(action.newInstance.item?.Id ?? 0);
            netMessage.writer.Put(action.newInstance.num);
            netMessage.writer.Put(action.newInstance.damage);
            netMessage.writer.Put(action.time);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            var entityId = reader.GetULong();
            var inventoryId = reader.GetByte();
            var inventoryIndex = reader.GetUShort();
            var oldInstanceItemId = reader.GetInt();
            var oldInstanceNum = reader.GetInt();
            var oldInstanceDamage = reader.GetInt();
            var newInstanceItemId = reader.GetInt();
            var newInstanceNum = reader.GetInt();
            var newInstanceDamage = reader.GetInt();
            var time = reader.GetDouble();

            var action = new QueuedInventoryUpdate
            {
                entityId = entityId,
                inventoryId = inventoryId,
                inventoryIndex = inventoryIndex,
                oldInstance = new ItemInstance(Main.Registry.ItemRegistry.Get(oldInstanceItemId), oldInstanceNum, oldInstanceDamage),
                newInstance = new ItemInstance(Main.Registry.ItemRegistry.Get(newInstanceItemId), newInstanceNum, newInstanceDamage),
                time = time,
            };

            var entity = GS.GetWorld().EntityManager.GetById(entityId);
            if (entity != null && entity is IHasInventory hasInv)
            {
                queued.Add(action);
            }
        }

        public void Apply(EntityManager entityManager)
        {
            var otherBuffer = queued == queued1 ? queued2 : queued1;

            foreach (QueuedInventoryUpdate action in queued)
            {
                if (Main.Time >= action.time)
                {
                    // Invalidate any audits that may be attempting to update this position
                    //for (int i = 0; i < MAX_AUDITS; i++)
                    //{
                    //    ref var currAudit = ref SyncCubeUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][i];
                    //    if (currAudit.active && currAudit.position == qcubeupdated.position && qcubeupdated.time > currAudit.time)
                    //    {
                    //        currAudit.active = false;
                    //    }
                    //}
                    var entity = entityManager.GetById(action.entityId);
                    if (entity != null && entity is IHasInventory hasInv)
                    {
                        var inventory = hasInv.GetInventory(action.inventoryId);
                        inventory.DoUpdateAction(action);

                        Console.WriteLine("Remote Inventory action: {0:02} {1} {2} {3} -> {4}", Main.Time, entity.ToString(), action.inventoryId, action.oldInstance.item, action.newInstance.item);
                        //Console.WriteLine("Remote Inventory update: {0} {1} -> {2}", action.time, action.oldInstance.item, action.newInstance.item);
                    }
                }
                else
                {
                    otherBuffer.Add(action);
                }
            }

            queued.Clear();
            queued = otherBuffer;
        }
    }

    public class SyncInventoryUpdateAuditRequest : Message
    {
        public static SyncInventoryUpdateAuditRequest Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

        public const double TIMEOUT = 0.5;

        public struct AuditedInventoryUpdate
        {
            public bool active;
            public byte auditIndex;
            public byte auditingPlayer;
            public ulong entityId;
            public int inventoryId;
            public int inventoryIndex;
            public ItemInstance oldInstance, newInstance;
            public double time;
        }

        public AuditedInventoryUpdate[][] activeAudits = null;

        public SyncInventoryUpdateAuditRequest()
        {
            activeAudits = new AuditedInventoryUpdate[World.MAX_PLAYERS][];
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                activeAudits[i] = new AuditedInventoryUpdate[SyncCubeUpdateAuditRequest.MAX_AUDITS];
            }
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            var action = addData as SyncInventoryUpdate.QueuedInventoryUpdate ? ?? throw new Exception();
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;

            AuditedInventoryUpdate auditedAction = new AuditedInventoryUpdate
            {
                entityId = action.entityId,
                inventoryId = action.inventoryId,
                inventoryIndex = action.inventoryIndex,
                newInstance = action.newInstance,
                oldInstance = action.oldInstance,
                time = action.time,
            };

            bool found = false;
            for (int i = 0; i < activeAudits[GS.GetWorld().localPlayerIndex].Length; i++)
            {
                if (!activeAudits[GS.GetWorld().localPlayerIndex][i].active)
                {
                    auditedAction = auditedAction with
                    {
                        active = true,
                        auditIndex = (byte)i,
                        auditingPlayer = (byte)GS.GetWorld().localPlayerIndex,
                    };
                    activeAudits[GS.GetWorld().localPlayerIndex][i] = auditedAction;

                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Console.WriteLine("Couldnt find open slot");
                RollbackAction(auditedAction);
            }
            else
            {
                netMessage.writer.Put(auditedAction.time);
                netMessage.writer.Put(auditedAction.auditIndex);
                netMessage.writer.Put(auditedAction.auditingPlayer);
                netMessage.writer.Put(auditedAction.entityId);
                netMessage.writer.Put((byte)auditedAction.inventoryId);
                netMessage.writer.Put((ushort)auditedAction.inventoryIndex);
                netMessage.writer.Put(auditedAction.oldInstance.item?.Id ?? 0);
                netMessage.writer.Put(auditedAction.oldInstance.num);
                netMessage.writer.Put(auditedAction.oldInstance.damage);
                netMessage.writer.Put(auditedAction.newInstance.item?.Id ?? 0);
                netMessage.writer.Put(auditedAction.newInstance.num);
                netMessage.writer.Put(auditedAction.newInstance.damage);

                netMessage.Send();
            }
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            var time = reader.GetDouble();
            var index = reader.GetByte();
            var player = reader.GetByte();
            var entityId = reader.GetULong();
            var id = reader.GetByte();
            var inventoryIndex = reader.GetUShort();
            var oldInstanceItemId = reader.GetInt();
            var oldInstanceNum = reader.GetInt();
            var oldInstanceDamage = reader.GetInt();
            var newInstanceItemId = reader.GetInt();
            var newInstanceNum = reader.GetInt();
            var newInstanceDamage = reader.GetInt();

            Debug.Assert(!activeAudits[player][index].active);

            var action = new AuditedInventoryUpdate
            {
                active = true,
                time = time,
                auditIndex = index,
                auditingPlayer = player,
                entityId = entityId,
                inventoryId = id,
                inventoryIndex = inventoryIndex,
                oldInstance = new ItemInstance(Main.Registry.ItemRegistry.Get(oldInstanceItemId), oldInstanceNum, oldInstanceDamage),
                newInstance = new ItemInstance(Main.Registry.ItemRegistry.Get(newInstanceItemId), newInstanceNum, newInstanceDamage),
            };

            activeAudits[player][index] = action;
        }

        public void Apply(EntityManager entityManager)
        {
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                for (int j = 0; j < SyncCubeUpdateAuditRequest.MAX_AUDITS; j++)
                {
                    if (activeAudits[i][j].active)
                    {
                        if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Server)
                        {
                            var action = activeAudits[i][j];
                            var peer = GS.netManager.GetPeer(action.auditingPlayer);
                            if (peer != null)
                            {
                                var accepted = false;
                                var entity = entityManager.GetById(action.entityId);
                                if (entity != null && entity is IHasInventory hasInv)
                                {
                                    var inventory = hasInv.GetInventory(action.inventoryId);
                                    var curInstance = inventory.Get(action.inventoryIndex);

                                    // TODO: in what situations do we decline a request?
                                    if (curInstance.item != action.oldInstance.item || curInstance.damage != action.oldInstance.damage) 
                                    {
                                        accepted = false;
                                    }
                                }

                                Main.Registry.MessageRegistry.SendMessageToPeer(SyncInventoryUpdateAuditResponse.Instance, peer, new SyncInventoryUpdateAuditResponse.AcceptedInventoryUpdate()
                                {
                                    accepted = accepted,
                                    index = (byte)j,
                                    newInstance = action.newInstance,
                                });
                            }

                            // If peer was no longer alive, then we still need to mark this as inactive
                            activeAudits[i][j].active = false;
                        }
                        else
                        {
                            if (Main.Time - activeAudits[i][j].time >= TIMEOUT)
                            {
                                RollbackAction(activeAudits[i][j]);
                                activeAudits[i][j].active = false;
                            }
                        }
                    }
                }
            }
        }

        public void RollbackAction(AuditedInventoryUpdate action)
        {
            var player = GS.GetWorld().player[action.auditingPlayer];
            var inventory = player?.GetInventory(action.inventoryId);
            if (inventory != null)
            {
                inventory.DoUpdateAction(new SyncInventoryUpdate.QueuedInventoryUpdate
                {
                    entityId = action.entityId,
                    inventoryId = action.inventoryId,
                    inventoryIndex = action.inventoryIndex,
                    newInstance = action.oldInstance,
                    oldInstance = action.oldInstance,
                    time = action.time,
                });
            }
        }

        public void DoAction(AuditedInventoryUpdate action)
        {
            var player = GS.GetWorld().player[action.auditingPlayer];
            var inventory = player?.GetInventory(action.inventoryId);
            if (inventory != null)
            {
                inventory.DoUpdateAction(new SyncInventoryUpdate.QueuedInventoryUpdate
                {
                    entityId = action.entityId,
                    inventoryId = action.inventoryId,
                    inventoryIndex = action.inventoryIndex,
                    newInstance = action.newInstance,
                    oldInstance = action.oldInstance,
                    time = action.time,
                });
            }
        }
    }

    public class SyncInventoryUpdateAuditResponse : Message
    {
        public static SyncInventoryUpdateAuditResponse Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public struct AcceptedInventoryUpdate
        {
            public bool accepted;
            public byte index;
            public ItemInstance newInstance;
        }

        public SyncInventoryUpdateAuditResponse()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            var action = addData as AcceptedInventoryUpdate? ?? throw new Exception();
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;

            //Console.WriteLine("Accept {0} AuditedInventoryUpdate: {1} newInstanceItemId {2}", action.index, action.accepted, action.newInstance.item?.Id ?? 0);

            netMessage.writer.Put(action.accepted);
            netMessage.writer.Put(action.index);
            netMessage.writer.Put(action.newInstance.item?.Id ?? 0);
            netMessage.writer.Put(action.newInstance.num);
            netMessage.writer.Put(action.newInstance.damage);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            var accepted = reader.GetBool();
            var index = reader.GetByte();
            var newInstanceItemId = reader.GetInt();
            var newInstanceNum = reader.GetInt();
            var newInstanceDamage = reader.GetInt();

            var newInstance = new ItemInstance(Main.Registry.ItemRegistry.Get(newInstanceItemId), newInstanceNum, newInstanceDamage);
            //Console.WriteLine("Received accept {0} AuditedInventoryUpdate: {1} newInstanceItemId {2}", index, accepted, newInstanceItemId);

            SyncInventoryUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index].oldInstance = newInstance;
            SyncInventoryUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index].newInstance = newInstance;
            if (accepted)
            {
                SyncInventoryUpdateAuditRequest.Instance.DoAction(SyncInventoryUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index]);
            }
            else
            {
                SyncInventoryUpdateAuditRequest.Instance.RollbackAction(SyncInventoryUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index]);
            }
            SyncInventoryUpdateAuditRequest.Instance.activeAudits[GS.GetWorld().localPlayerIndex][index].active = false;
        }
    }
}
