using BepuPhysics.Constraints;
using BrUtility;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using SharpDX.MediaFoundation.DirectX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.Entities.Renderers;
using ViMG.IMGUIImpl;
using static Engine.Networking.Messages.SyncEntityStateAck;
using static ViMG.LightManager;
using static ViMG.UIs.UI;

namespace Engine.Networking.Messages
{
    public class SyncBasicState : Message
    {
        public enum SyncType
        {
            // Sync with full serialization of entity
            // Entity is created if not already present
            FullSync,
            // Sync with ISyncBasicState implementation if available
            // (if not, does not do anything)
            // Unreliable
            BasicState,
            // Just id and position
            // Unreliable
            SuperSimple,
            EntityUnloaded, // Entity has been unloaded
        }

        public struct SyncEntity
        {
            public SyncType type;
            public bool firstCreation;
            public Entity entity;
        }

        public static SyncBasicState Instance { get; private set; }
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        private struct QueuedSyncEntity
        {
            public DateTime actualReceiveTime;
            public SyncType type;
            public BasicState basicState;
            public EntityManagerIO.EntityData fullState;
            public ulong entityId;
            public double time;
        }

        private List<QueuedSyncEntity> queued1 = new List<QueuedSyncEntity>();
        private List<QueuedSyncEntity> queued2 = new List<QueuedSyncEntity>();
        private List<QueuedSyncEntity> queued;

        public SyncBasicState()
        {
            Instance = this;
            Passthrough = true;

            queued = queued1;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            SyncEntity entity = addData as SyncEntity? ?? throw new Exception();

            if (entity.entity == null) IMGUIConsole.Assert(false);

            if (entity.type == SyncType.BasicState && entity.entity is not ISyncBasicState)
            {
                Console.WriteLine("Couldn't do BasicState sync for entity {0} - it does not implement ISyncBasicState!", entity.entity);
                return;
            }

            if (entity.type == SyncType.BasicState || entity.type == SyncType.SuperSimple)
            {
                netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;
            }
            else
            {
                netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;
            }

            double time = Main.Time;

            // TODO this may be necessary
            // If we receive a FullSync and EntityUnloaded message together, the former might be processed AFTER the latter,
            // and the entity would be re-created and exist on the client when it shouldn't
            //if (entity.type == SyncType.EntityUnloaded)
            //{
            //    time += 0.25;
            //}

            netMessage.writer.Put(Main.Time);
            entity.entity.TimeSynced = Main.Time;

            netMessage.writer.Put(entity.entity.Id);

            netMessage.writer.Put((int)entity.type);

            switch (entity.type)
            {
                case SyncType.SuperSimple:
                    netMessage.writer.Put(entity.entity.Position.X);
                    netMessage.writer.Put(entity.entity.Position.Y);
                    netMessage.writer.Put(entity.entity.Position.Z);
                    break;
                case SyncType.BasicState:
                    if (entity.entity is ISyncBasicState syncer)
                    {
                        syncer.Get(out BasicState bstate);
                        netMessage.writer.Put(bstate);
                    }
                    else
                    {
                        netMessage.writer.Put(new BasicState());
                    }
                    break;
                case SyncType.FullSync:
                    entity.entity.TimeMajorSynced = Main.Time;
                    var entityData = new EntityManagerIO.EntityData(entity.entity);
                    List<byte> bytes = new List<byte>();
                    entityData.Save(bytes);
                    netMessage.writer.Put(entity.firstCreation);
                    netMessage.writer.PutArray(bytes.ToArray(), sizeof(byte));
                    break;
            }

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            double time = reader.GetDouble();

            ulong id = reader.GetULong();

            SyncType type = (SyncType)reader.GetInt();
            QueuedSyncEntity local = new QueuedSyncEntity();
            local.type = type;
            local.entityId = id;
            local.time = time;

            switch (type)
            {
                case SyncType.SuperSimple:
                    local.basicState.position.X = reader.GetFloat();
                    local.basicState.position.Y = reader.GetFloat();
                    local.basicState.position.Z = reader.GetFloat();
                    break;
                case SyncType.FullSync:
                    bool firstSync = reader.GetBool();
                    byte[] bytes = reader.GetArray<byte>(sizeof(byte));
                    EntityManagerIO.EntityData data = new();
                    data.Load(bytes);
                    local.fullState = data;
                    break;
                case SyncType.BasicState:
                    local.basicState = reader.Get<BasicState>();
                    break;
                case SyncType.EntityUnloaded:
                    break;
                default:
                    throw new Exception(string.Format("Unknown SyncType {0}", type));
            }

            local.actualReceiveTime = DateTime.Now;
            //DoAction(local, GS.GetWorld().EntityManager, GS.GetWorld().EntIO);
            queued.Add(local);
        }

        public void Apply(EntityManager entityManager, EntityManagerIO entIO)
        {
            var otherBuffer = queued == queued1 ? queued2 : queued1;

            queued.OrderBy(x => x.time);

            foreach (QueuedSyncEntity queuedSync in queued)
            {
                //Console.WriteLine("{0} Delay: {1:0.02}", Main.gameStateManager.TheIsland.netManager.whoAmI, (DateTime.Now - queuedSync.actualReceiveTime).TotalSeconds);
                //if (Main.Time >= queuedSync.time)
                //{
                DoAction(queuedSync, entityManager, entIO);
                //}
                //else
                //{
                //    otherBuffer.Add(queuedSync);
                //}
            }

            queued.Clear();
            // Swap buffers
            queued = otherBuffer;
        }

        private void DoAction(QueuedSyncEntity queuedSync, EntityManager entityManager, EntityManagerIO entIO)
        {
            Entity? ent = entityManager.GetById(queuedSync.entityId);
            // NOTE: SuperSimple and BasicState state is completely ignored if the entity does not exist.
            // It is not an error for a client to receive sync state for an entity that does not exist.
            if (ent != null)
            {
                switch (queuedSync.type)
                {
                    case SyncType.SuperSimple:
                        ent.Position.X = queuedSync.basicState.position.X;
                        ent.Position.Y = queuedSync.basicState.position.Y;
                        ent.Position.Z = queuedSync.basicState.position.Z;
                        break;
                    case SyncType.BasicState:
                        if (ent is ISyncBasicState syncer)
                        {
                            var bstate = queuedSync.basicState;
                            syncer.Set(ref bstate);
                        }
                        break;
                    case SyncType.FullSync:
                        ent.TimeMajorSynced = queuedSync.time;
                        break;
                    case SyncType.EntityUnloaded:
                        entityManager.Unload(ent);
                        break;
                }

                ent.TimeSynced = queuedSync.time;
            }

            // Full Sync has special behavior; if an entity does not already exist, it is created
            // TODO: what happens if we receive an EntityUnloaded and then this?
            if (queuedSync.type == SyncType.FullSync)
            {
                if (queuedSync.fullState.IsValid)
                {
                    if (ent == null)
                        ent = entIO.DeserializeEntity(queuedSync.fullState);
                    else ent.OnLoad(queuedSync.fullState.data, queuedSync.fullState.version);

                    if (ent != null)
                        ent.TimeSynced = queuedSync.time;
                }
            }
        }
    }

    // Entities are sometimes staying when they shouldn't
    // Items in particular are easy to get this to happen to
    public class SyncEntityState : Message
    {
        public const int MAX_ENTS_PER_SYNC = 32;

        public static SyncEntityState Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public enum SyncStateType
        {
            // Unload entity
            Unload,
            // Create entity
            MajorSync,
            // Just an update
            MinorSync,
        }

        private struct ToSync
        {
            public required int playerId;
            public required EntityManager.EntityReference reference;

            public required SyncStateType type;
            public ISyncBasicState? basicSyncState;
            public EntityManagerIO.EntityData? majorSyncState;
        }

        private struct SyncedEntity
        {
            public EntityManager.EntityReference reference;
            // NOTE: equivalent to Main.Frame
            public int latestSequence;
            public bool unloadedThisSeq;
        }

        private FastList<ToSync> toSync;

        private double lastSyncTime;
        private SyncedEntity[][] entities;

        private int latestSeq;

        public SyncEntityState()
        {
            Instance = this;

            toSync = new FastList<ToSync>(EntityManager.EntMax);

            entities = new SyncedEntity[World.MAX_PLAYERS][];

            for (int i = 0; i < entities.Length; i++)
                {
                    entities[i] = new SyncedEntity[EntityManager.EntMax];
                    for (int j = 0; j < EntityManager.EntMax; j++)
                    {
                        entities[i][j] = new() { reference = new() { id = j, generation = -1 }, latestSequence = -1 };
                    }
                }
        }

        public void AddAck(SyncEntityStateAck.Ack ack, int playerId, int sequence)
        {
            for (int i = 0; i < ack.numAckd; i++)
            {
                entities[playerId][ack.ackdEntities[i].id].reference.generation = ack.ackdEntities[i].generation;
                // Note we blindly set the sequence here; earlier we discard sequences that are not the latest, so this should work fine
                entities[playerId][ack.ackdEntities[i].id].latestSequence = sequence;
            }
        }

        public void DoSync(EntityManager entityManager, Player[] players)
        {
            if (Main.Time - lastSyncTime > EntityManager.EntSyncTime)
            {
                foreach (Player player in players)
                {
                    if (player == null || !player.IsInitialized || player.IsLocalPlayer)
                        continue;

                    var peer = Main.gameStateManager.TheIsland.netManager.GetPeer(player.playerIndex);

                    if (peer == null)
                    {
                        Console.WriteLine("Peer null");
                        continue;
                    }

                    for (int i = 0; i < EntityManager.EntMax; i++)
                    {
                        var reference = entityManager.GetReference(i);
                        var ent = entityManager.GetByRef(ref reference);

                        if (entities[player.playerIndex][i].reference.generation != reference.generation)
                        {
                            if (ent != null)
                            {
                                var entSerializableAttr = ent.GetType().GetCustomAttribute<EntitySerializableAttribute>();
                                if (entSerializableAttr != null)
                                {
                                    if ((entSerializableAttr.serializationType & EntitySerializableAttribute.SerializationType.Server) == EntitySerializableAttribute.SerializationType.Server)
                                    {

                                        //if (Main.Frame - entities[player.playerIndex][ent.Id].latestSequence > EntityManager.EntPrevSrv)
                                        //{
                                        //    Console.WriteLine("Ent {0} {1} out of date, resync {2} {3}", ent.ToString(), ent.Id, entities[player.playerIndex][ent.Id].latestSequence, Main.Frame);
                                        //}

                                        Console.WriteLine("Server sent create ent {0} {1}", ent.Id, ent.ToString());
                                        var entData = new EntityManagerIO.EntityData(ent);
                                        toSync.AddAssumeCapacity(new()
                                        {
                                            type = SyncStateType.MajorSync,
                                            playerId = player.playerIndex,
                                            reference = reference,
                                            majorSyncState = entData,
                                        });
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Server sent unload ent {0}", reference.id);

                                toSync.AddAssumeCapacity(new()
                                {
                                    type = SyncStateType.Unload,
                                    playerId = player.playerIndex,
                                    reference = reference,
                                });
                            }
                        }
                        else if (ent != null && ent.DoesSync && ent is ISyncBasicState syncsBasicState)
                        {
                            toSync.AddAssumeCapacity(new()
                            {
                                type = SyncStateType.MinorSync,
                                playerId = player.playerIndex,
                                reference = reference,
                                basicSyncState = syncsBasicState,
                            });
                        }
                    }

                    //var iter = entityManager.GetEntities();

                    //foreach (var ent in iter)
                    //{
                    //    var entSerializableAttr = ent.GetType().GetCustomAttribute<EntitySerializableAttribute>();
                    //    if (entSerializableAttr != null)
                    //    {
                    //        if ((entSerializableAttr.serializationType & EntitySerializableAttribute.SerializationType.Server) == EntitySerializableAttribute.SerializationType.Server)
                    //        {
                    //            var reference = entityManager.GetReference(ent);

                    //            if (entities[player.playerIndex][ent.Id].reference.generation != reference.generation)
                    //                //|| Main.Frame - entities[player.playerIndex][ent.Id].latestSequence > EntityManager.EntPrevSrv)
                    //            {
                    //                //if (Main.Frame - entities[player.playerIndex][ent.Id].latestSequence > EntityManager.EntPrevSrv)
                    //                //{
                    //                //    Console.WriteLine("Ent {0} {1} out of date, resync {2} {3}", ent.ToString(), ent.Id, entities[player.playerIndex][ent.Id].latestSequence, Main.Frame);
                    //                //}
                    //                if (!entityManager.GetActive((int)ent.Id))
                    //                {
                    //                    toSync.AddAssumeCapacity(new()
                    //                    {
                    //                        type = SyncStateType.GenerationChanged,
                    //                        playerId = player.playerIndex,
                    //                        reference = reference,
                    //                    });
                    //                }
                    //                else
                    //                {
                    //                    var entData = new EntityManagerIO.EntityData(ent);
                    //                    toSync.AddAssumeCapacity(new()
                    //                    {
                    //                        type = SyncStateType.MajorSync,
                    //                        playerId = player.playerIndex,
                    //                        reference = reference,
                    //                        majorSyncState = entData,
                    //                    });
                    //                }
                    //            }
                    //            else if (ent.DoesSync && ent is ISyncBasicState syncsBasicState)
                    //            {
                    //                toSync.AddAssumeCapacity(new()
                    //                {
                    //                    type = SyncStateType.MinorSync,
                    //                    playerId = player.playerIndex,
                    //                    reference = reference,
                    //                    basicSyncState = syncsBasicState,
                    //                });
                    //            }
                    //        }
                    //    }
                    //}

                    Main.Registry.MessageRegistry.SendMessageToPeer(Instance, peer, player.playerIndex);
                }

                lastSyncTime = Main.Time;
                Instance.PostSend();
            }
        }

        public void PostSend()
        {
            //Console.WriteLine("clear");
            toSync.Clear();
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = DeliveryMethod.Unreliable;
            netMessage.channel = (int)NetworkMessage.Channels.Entities;

            int playerId = addData as int? ?? throw new Exception();

            int chksumpos = netMessage.writer.Length;
            netMessage.writer.Put((ulong)0);

            netMessage.writer.Put(Main.Frame);

            int numsendpos = netMessage.writer.Length;
            netMessage.writer.Put(toSync.Length);

            List<NetDataWriter> subWriters = new List<NetDataWriter>();

            foreach (var ent in toSync.Slice())
            {
                // -1 = send to all players
                if (ent.playerId == playerId || playerId == -1)
                {
                    // FIXME pre-emptive allocation, not great, we can remove this
                    // Subwriter may not be submitted
                    var subWriter = new NetDataWriter();
                    subWriter.Put(ent.reference);
                    subWriter.Put((byte)ent.type);

                    if (ent.basicSyncState != null)
                    {
                        ent.basicSyncState.Get(out BasicState state);

                        var prevState = GS.GetWorld().EntityManager.GetPrevStateAbs(ent.reference.id, entities[ent.playerId][ent.reference.id].latestSequence);
                        uint bits = state.GetDeltaBits(ref prevState);

                        // We haven't changed at all, don't bother syncing
                        if (bits != 0)
                        // We DO bother synching actually. 
                        // Nothing will be sync'd, but basically this just informs the client that it expects this entity to exist.
                        // If it does NOT have this entity, then it will not send back an ack (because it can't do anything with the entity data.)
                        // The server will see that it has no ack, and send a major sync.
                        {
                            state.SerializeDelta(subWriter, bits);

                            subWriters.Add(subWriter);
                        }
                    }
                    else if (ent.majorSyncState != null)
                    {
                        List<byte> bytes = new List<byte>();
                        ent.majorSyncState.Value.Save(bytes);

                        subWriter.PutBytesWithLength(bytes.ToArray(), 0, (ushort)bytes.Count);
                        subWriters.Add(subWriter);
                    }
                    else
                    {
                        subWriters.Add(subWriter);
                    }

                    Debug.Assert(subWriter.Length < netMessage.peer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable) - sizeof(ulong) - sizeof(int) - sizeof(int));
                }
            }

            var atStart = netMessage.writer.Length;

            var end = 0;

            int numSend = 0;
            for (int i = 0; i < subWriters.Count; i++)
            {
                NetDataWriter subWriter = subWriters[i];
                if (netMessage.writer.Length + subWriter.Length < netMessage.peer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable) - sizeof(ulong) - sizeof(int) - sizeof(int) || numSend > MAX_ENTS_PER_SYNC)
                {
                    numSend += 1;
                    end = netMessage.writer.Length;
                    netMessage.writer.SetPosition(numsendpos);
                    netMessage.writer.Put(numSend);
                    netMessage.writer.SetPosition(end);
                    netMessage.writer.Put(subWriter.AsReadOnlySpan());
                }
                else
                {
                    DoSend(netMessage, chksumpos);
                    numSend = 0;

                    Debug.Assert(netMessage.writer.Length == atStart);
                }
            }

            if (numSend > 0)
                DoSend(netMessage, chksumpos);
        }

        private void DoSend(NetworkMessage netMessage, int chksumpos)
        {
            ulong chksum = 0;
            var span = netMessage.writer.AsReadOnlySpan()[(chksumpos + sizeof(ulong))..];
            for (int i = 0; i < span.Length; i++)
            {
                chksum += span[i];
            }

            //Console.WriteLine("Send Chksum: {0}", chksum);

            int end = netMessage.writer.Length;
            netMessage.writer.SetPosition(chksumpos);
            netMessage.writer.Put(chksum);
            netMessage.writer.SetPosition(end);

            netMessage.Send();

            netMessage.writer.SetPosition(chksumpos + sizeof(ulong) + sizeof(int) + sizeof(int));
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            ulong chksum = reader.GetULong();
            var postChksumPos = reader.Position;

            ulong ourChksum = 0;
            for (int i = 0; i < reader.RawDataSize - postChksumPos; i++)
            {
                ourChksum += reader.RawData[postChksumPos + i];
            }

            // chksum is incorrect, drop
            if (chksum != ourChksum)
            {
                Console.WriteLine("Discarded SyncEntity state - chksum did not match ({0} - {1})", chksum, ourChksum);
                return;
            }

            int seq = reader.GetInt();
            if (seq < latestSeq)
            {
                Console.WriteLine("Discarding SyncBasicState - seq was old {0} - {1}", seq, latestSeq);
                return;
            }
            latestSeq = seq;

            int num = reader.GetInt();

            int ackI = 0;
            SyncEntityStateAck.EntityAckArr ackArr = new();

            for (int i = 0; i < num; i++)
            {
                EntityManager.EntityReference reference = reader.Get<EntityManager.EntityReference>();

                var ent = GS.GetWorld().EntityManager.GetByRef(ref reference);

                SyncStateType type = (SyncStateType)reader.GetByte();
                if (type == SyncStateType.MinorSync)
                {
                    BasicState state;
                    // TODO: we might want to base this on the last received state for this entity (before this ack)
                    // We'd need to store that somehow. Right now we just store the latest sequence we've ack'd globally...
                    if (ent != null) state = GS.GetWorld().EntityManager.GetPrevState((int)ent.Id, 0);// GetPrevStateAbs((int)ent.Id, seq);
                    else state = new BasicState();

                    state.DeserializeDelta(reader);

                    // Check to make sure we're not trying to create or update an entity that was unloaded this framee
                    var unloadedThisSeq = entities[0][reference.id].latestSequence == seq && entities[0][reference.id].unloadedThisSeq;
                    if (unloadedThisSeq)
                        continue;

                    if (ent != null)
                    {
                        Debug.Assert(ent is ISyncBasicState);
                        (ent as ISyncBasicState).Set(ref state);

                        ent.NetEnable = true;

                        entities[0][ent.Id] = new SyncedEntity
                        {
                            reference = reference,
                            latestSequence = seq,
                            unloadedThisSeq = false,
                        };
                        
                        ackArr[ackI] = reference;
                        ackI += 1;
                    }
                }
                else if (type == SyncStateType.MajorSync)
                {
                    byte[] bytes = reader.GetArray<byte>(sizeof(byte));
                    EntityManagerIO.EntityData data = new();
                    data.Load(bytes);

                    // Check to make sure we're not trying to create or update an entity that was unloaded this framee
                    var unloadedThisSeq = entities[0][reference.id].latestSequence == seq && entities[0][reference.id].unloadedThisSeq;
                    if (unloadedThisSeq)
                        continue;

                    if (data.IsValid && (ent == null || data.type == ent.GetType().FullName))
                    {
                        Console.WriteLine("{0} {1}", data.id, data.type);
                        if (ent == null)
                            ent = GS.GetWorld().EntIO.DeserializeEntity(data, reference.generation);
                        else
                            ent.OnLoad(data.data, data.version);

                        ent.NetEntity = true;
                        ent.NetEnable = true;

                        entities[0][ent.Id] = new SyncedEntity
                        {
                            reference = reference,
                            latestSequence = seq,
                            unloadedThisSeq = false,
                        };

                        ackArr[ackI] = reference;
                        ackI += 1;
                    }
                    else Console.WriteLine("Data invalid {0}", reference.id);
                }
                else if (type == SyncStateType.Unload)
                {
                    Console.WriteLine("Server unloaded {0} {1}", reference.id, ent?.ToString());
                    if (ent != null)
                        GS.GetWorld().EntityManager.ForceUnload(ent);

                    entities[0][reference.id] = new SyncedEntity
                    {
                        reference = reference,
                        latestSequence = seq,
                        unloadedThisSeq = true,
                    };

                    ackArr[ackI] = reference;
                    ackI += 1;
                }
            }

            if (ackI > 0)
                Main.Registry.MessageRegistry.SendMessageToPeer(SyncEntityStateAck.Instance, peer, new SyncEntityStateAck.Ack { numAckd = ackI, ackdEntities = ackArr, sequence = seq });
        }
    }

    public class SyncEntityStateAck : Message
    {
        public static SyncEntityStateAck Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

        [InlineArray(SyncEntityState.MAX_ENTS_PER_SYNC)]
        public struct EntityAckArr
        {
            private EntityManager.EntityReference _entityId0;

            public EntityManager.EntityReference this[int i]
            {
                get => this[i];
                set => this[i] = value;
            }
        }

        public struct Ack : INetSerializable
        {
            public int numAckd;
            public EntityAckArr ackdEntities;
            public int sequence;

            public void Deserialize(NetDataReader reader)
            {
                sequence = reader.GetInt();
                numAckd = reader.GetInt();

                for (int i = 0; i < numAckd; i++)
                {
                    ackdEntities[i] = reader.Get<EntityManager.EntityReference>();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(sequence);
                writer.Put(numAckd);

                for (int i = 0; i < numAckd; i++)
                {
                    writer.Put(ackdEntities[i]);
                }
            }
        }

        public SyncEntityStateAck()
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

            SyncEntityState.Instance.AddAck(ack, Main.gameStateManager.TheIsland.netManager.GetNetPlayer(peer).playerId, ack.sequence);
        }
    }
}
