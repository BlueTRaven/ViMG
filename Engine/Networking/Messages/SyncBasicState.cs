using BepuPhysics.Constraints;
using BrUtility;
using Engine.IMGUIImpl;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using SharpDX.MediaFoundation.DirectX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.Entities.Renderers;
using ViMG.IMGUIImpl;

namespace Engine.Networking.Messages
{
    [Obsolete]
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

            double time = GlobalState.Time;

            // TODO this may be necessary
            // If we receive a FullSync and EntityUnloaded message together, the former might be processed AFTER the latter,
            // and the entity would be re-created and exist on the client when it shouldn't
            //if (entity.type == SyncType.EntityUnloaded)
            //{
            //    time += 0.25;
            //}

            netMessage.writer.Put(GlobalState.Time);
            entity.entity.TimeSynced = GlobalState.Time;

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
                    entity.entity.TimeMajorSynced = GlobalState.Time;
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
                //Console.WriteLine("{0} Delay: {1:0.02}", GlobalState.gameStateManager.TheIsland.netManager.whoAmI, (DateTime.Now - queuedSync.actualReceiveTime).TotalSeconds);
                //if (GlobalState.Time >= queuedSync.time)
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
                        ent = entIO.DeserializeEntity(GS.GetWorld(), queuedSync.fullState);
                    else ent.OnLoad(GS.GetWorld(), queuedSync.fullState.data, queuedSync.fullState.version);

                    if (ent != null)
                        ent.TimeSynced = queuedSync.time;
                }
            }
        }
    }

    public class SyncEntityState : Message
    {
        private Logger Logger = Logger.InitLogger("SyncBasicState", true, Logger.LogLevel.Warn);
        public const int MAX_ENTS_PER_SYNC = 256;

        public static SyncEntityState Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public enum SyncStateType
        {
            // Unload entity
            Unload,
            // Create entity
            MajorSync,
            // Tracks a cube position (needs to mark it dirty!)
            // Note that we don't ever expect these positions to move, so this is only done on creation
            MajorSyncTracker,
            // Just an update
            MinorSync,
        }

        private struct ToSync
        {
            public required int playerId;
            public required EntityManager.EntityReference reference;

            public required SyncStateType type;
            public CubePosition[]? trackedPositions;
            public ISyncBasicState? basicSyncState;
            public int typeNameMapping;
            public EntityManagerIO.EntityData? majorSyncState;
        }

        private struct SyncedEntity
        {
            public EntityManager.EntityReference reference;
            // NOTE: equivalent to Main.Frame
            public int latestSequence;
            //public BasicState latestAckedVersion;
            public bool unloadedThisSeq;
        }

        private FastList<ToSync> toSync;

        private SyncedEntity[][] serverEntities;
        private SyncedEntity[] clientEntities;

        private int playerTypeId = -1;

        public SyncEntityState()
        {
            Instance = this;

            toSync = new FastList<ToSync>(EntityManager.EntMax);

            serverEntities = new SyncedEntity[World.MAX_PLAYERS][];
            clientEntities = new SyncedEntity[EntityManager.EntMax];
            for (int i = 0; i < serverEntities.Length; i++)
            {
                serverEntities[i] = new SyncedEntity[EntityManager.EntMax];
                for (int j = 0; j < EntityManager.EntMax; j++)
                {
                    serverEntities[i][j] = new() { reference = new() { id = j, generation = -1 }, latestSequence = -1 };
                }
            }
            for (int i = 0; i < EntityManager.EntMax; i++)
                clientEntities[i] = new() { reference = new() { id = i, generation = -1 }, latestSequence = -1 };

            var counter = new NetDataWriter();
            AddEntBasics(counter, new EntityManager.EntityReference(), SyncStateType.Unload, 0);
            numBytesHeader = counter.Length;
        }

        public void ServerShutdown()
        {
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                PlayerDisconnected(i);
            }
        }

        public void PlayerDisconnected(int playerIndex)
        {
            for (int i = 0; i < EntityManager.EntMax; i++)
            {
                serverEntities[playerIndex][i] = new() { reference = new() { id = i, generation = -1 }, latestSequence = -1 }; 
            }
        }

        public void AddAck(SyncEntityStateAck.Ack ack, int playerId, int sequence)
        {
            IMGUINetworkDebug.AddClientEntAck(sequence, (DateTime.Now - ack.initialSend).TotalSeconds);

            TimeSpan delay = DateTime.Now - ack.initialRecv;
            //Console.WriteLine("seq {0} now {1}", sequence, serverSequence);
            //if (SyncWorldState.Instance.ServerSequence - sequence > 1)
            //{
            //    Console.WriteLine("Late {0} {1:0.00}", SyncWorldState.Instance.ServerSequence - sequence, delay.TotalSeconds);
            //}

            for (int i = 0; i < ack.numAckd; i++)
            {
                //Console.WriteLine("Ack for {0} {1} {2}", playerId, ack.ackdEntities[i].id, sequence);
                serverEntities[playerId][ack.ackdEntities[i].id].reference.generation = ack.ackdEntities[i].generation;
                // Note we blindly set the sequence here; earlier we discard sequences that are not the latest, so this should work fine
                serverEntities[playerId][ack.ackdEntities[i].id].latestSequence = sequence;
                //serverEntities[playerId][ack.ackdEntities[i].id].latestAckedVersion = GS.GetWorld().EntityManager.GetPrevStateAbs(ack.ackdEntities[i].id, sequence);
            }
        }

        public void DoSync(EntityManager entityManager, Player[] players)
        {
            foreach (Player player in players)
            {
                if (player == null || !player.IsInitialized)
                    continue;

                var peer = GS.netManagerServer?.GetPeer(player.playerIndex);

                if (peer == null)
                {
                    Logger.Log(Logger.LogLevel.Warn, "Peer null");
                    Console.WriteLine("Peer null");
                    continue;
                }

                for (int i = 0; i < EntityManager.EntMax; i++)
                {
                    var reference = entityManager.GetReference(i);
                    var ent = entityManager.GetByRefServer(ref reference);

                    if (serverEntities[player.playerIndex][i].reference.generation != reference.generation)
                    {
                        if (ent != null && ent.DoesSync && ent is ISyncBasicState syncsBasicState)
                        {
                            var entSerializableAttr = ent.GetType().GetCustomAttribute<EntitySerializableAttribute>();
                            if (entSerializableAttr != null)
                            {
                                if ((entSerializableAttr.serializationType & EntitySerializableAttribute.SerializationType.Server) == EntitySerializableAttribute.SerializationType.Server)
                                {
                                    var regId = GlobalState.Registry.EntityRegistry.GetFromEntity(ent).Id;
                                    CubePosition[]? trackedPositions = null;
                                    if (ent is ICubeTracker tracker)
                                    {
                                        if (trackedPositions == null) trackedPositions = new CubePosition[1];
                                        trackedPositions[0] = tracker.TrackedPosition;
                                    }
                                    else if (ent is IMultiCubeTracker mtracker)
                                    {
                                        if (trackedPositions == null) trackedPositions = new CubePosition[mtracker.TrackedPositions.Count()];
                                        for (int j = 0; j < mtracker.TrackedPositions.Count(); j++)
                                            trackedPositions[j] = mtracker.TrackedPositions.ElementAt(j);
                                    }

                                    Logger.Log(Logger.LogLevel.Info, "send create ent {0} {1} ({2}) {3}", ent.Id, ent.ToString(), regId, reference.id);
                                    //Console.WriteLine("Server sent create ent {0} {1} ({2}) {3}", ent.Id, ent.ToString(), regId, reference.id);
                                    toSync.AddAssumeCapacity(new()
                                    {
                                        type = SyncStateType.MajorSync,
                                        playerId = player.playerIndex,
                                        reference = reference,
                                        trackedPositions = trackedPositions,
                                        typeNameMapping = regId,
                                        basicSyncState = syncsBasicState,
                                    });
                                }
                            }
                        }
                        else
                        {
                            // Client never had it loaded in the first place
                            if (serverEntities[player.playerIndex][i].reference.generation != -1)
                            {
                                Logger.Log(Logger.LogLevel.Warn, "send unload ent {0}", reference.id);

                                //Console.WriteLine("Server sent unload ent {0}", reference.id);

                                toSync.AddAssumeCapacity(new()
                                {
                                    type = SyncStateType.Unload,
                                    playerId = player.playerIndex,
                                    reference = reference,
                                });
                            }
                        }
                    }
                    else if (ent != null && ent.DoesSync && ent is ISyncBasicState syncsBasicState)
                    {
                        toSync.AddAssumeCapacity(new()
                        {
                            type = SyncStateType.MinorSync,
                            playerId = player.playerIndex,
                            reference = reference,
                            typeNameMapping = GlobalState.Registry.EntityRegistry.GetFromEntity(ent).Id, //typeNameToTypeId[ent.GetType().FullName],
                            basicSyncState = syncsBasicState,
                        });
                    }
                }

                GS.netManagerServer?.SendMessageToPeer(Instance, peer, player.playerIndex);
                toSync.Clear();
            }
        }

        private List<NetDataWriter> subwriters = new List<NetDataWriter>();
        private int numBytesHeader = 0;
        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = DeliveryMethod.Unreliable;
            netMessage.channel = (int)NetworkMessage.Channels.Entities;

            int playerId = addData as int? ?? throw new Exception();

            netMessage.writer.Put(DateTime.Now.Ticks);
            netMessage.writer.Put(SyncWorldState.Instance.ServerSequence);

            int numsendpos = netMessage.writer.Length;
            netMessage.writer.Put(toSync.Length);

            subwriters.Clear();

            foreach (var ent in toSync.Slice())
            {
                // -1 = send to all players
                if (ent.playerId == playerId || playerId == -1)
                {
                    if (ent.basicSyncState != null)
                    {
                        var useType = (byte)ent.type;

                        ent.basicSyncState.Get(out BasicState state);

                        BasicState prevState;
                        if (ent.type == SyncStateType.MinorSync)
                        {
                            var latestSeq = serverEntities[ent.playerId][ent.reference.id].latestSequence;
                            if (latestSeq - SyncWorldState.Instance.ServerSequence > EntityManager.EntPrevSrv)
                            {
                                // Too old - do a major sync
                                var entType = GlobalState.Registry.EntityRegistry.Get(ent.typeNameMapping);
                                Logger.Log(Logger.LogLevel.Warn, "ent {0} {1} sync timeout", entType.Identifier, ent.reference.id);
                                //Console.WriteLine("Ent {0}:{1} sync timeout", entType.Identifier, ent.reference.id);
                                prevState = new();
                                useType = (byte)SyncStateType.MajorSync;
                            }
                            else
                            {
                                prevState = GS.GetWorld().EntityManager.GetPrevStateAbs(ent.reference.id, latestSeq);
                            }
                        }
                        else if (ent.type == SyncStateType.MajorSync)
                        {
                            //var entType = GlobalState.Registry.EntityRegistry.Get(ent.typeNameMapping);
                            //Console.WriteLine("Server sent create ent {0} {1} ({2})", ent.reference.id, entType.Identifier, ent.typeNameMapping);
                            prevState = new();
                        }
                        else
                            throw new Exception();

                        uint bits = state.GetDeltaBits(ref prevState);
                        ulong extraBits = state.GetExtraBytesBits(ref prevState);

                        //ulong numBytes = state.GetNumBytesFromBits(bits, extraBits);

                        // We haven't changed at all, don't bother syncing
                        if (bits != 0)
                        {
                            var subwriter = new NetDataWriter();

                            //if (netMessage.writer.Length + subwriter.Length + numBytesHeader + (int)numBytes > netMessage.peer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable))
                            //{
                            //    netMessage.writer.Put(subwriter.AsReadOnlySpan());
                            //    netMessage.Send();
                            //    subwriter = new NetDataWriter();
                            //}

                            subwriter.Put(ent.reference);

                            subwriter.Put(useType);
                            subwriter.Put((ushort)ent.typeNameMapping);

                            state.SerializeDelta(subwriter, bits);
                            state.SerializeDeltaExtraFields(subwriter, extraBits);

                            if (ent.type == SyncStateType.MajorSync)
                            {
                                subwriter.Put(ent.trackedPositions?.Length ?? 0);
                                if (ent.trackedPositions != null)
                                {
                                    for (int i = 0; i < ent.trackedPositions.Length; i++)
                                        ent.trackedPositions[i].Serialize(subwriter);
                                }
                            }

                            subwriters.Add(subwriter);
                        }
                    }
                    else
                    {
                        var subwriter = new NetDataWriter();
                        subwriter.Put(ent.reference);
                        subwriter.Put((byte)ent.type);
                        subwriters.Add(subwriter);
                    }

                    //Debug.Assert(subwriter.Length < netMessage.peer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable) - sizeof(int) - sizeof(int));
                }
            }

            var atStart = netMessage.writer.Length;

            int numSend = 0;
            for (int i = 0; i < subwriters.Count; i++)
            {
                NetDataWriter subwriter = subwriters[i];
                if (netMessage.writer.Length + subwriter.Length < netMessage.peer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable) -  sizeof(int) - sizeof(int) && numSend < MAX_ENTS_PER_SYNC)
                {
                    numSend += 1;
                    
                    netMessage.writer.Put(subwriter.AsReadOnlySpan());
                }
                else
                {
                    int end = netMessage.writer.Length;
                    netMessage.writer.SetPosition(numsendpos);
                    netMessage.writer.Put(numSend);
                    netMessage.writer.SetPosition(end);
                    netMessage.Send();

                    netMessage.writer.SetPosition(atStart);
                    numSend = 1;
                    netMessage.writer.Put(subwriter.AsReadOnlySpan());
                }
            }

            if (numSend > 0)
            {
                int end = netMessage.writer.Length;
                netMessage.writer.SetPosition(numsendpos);
                netMessage.writer.Put(numSend);
                netMessage.writer.SetPosition(end);
                netMessage.Send();
            }
        }

        private void AddEntBasics(NetDataWriter writer, EntityManager.EntityReference reference, SyncStateType syncType, ushort typeId)
        {
            writer.Put(reference);

            writer.Put((byte)syncType);
            writer.Put(typeId);
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            var client = GS.GetClient();

            long ticks = reader.GetLong();
            DateTime sendTime = new DateTime(ticks);
            DateTime recvTime = DateTime.Now;
            TimeSpan delay = recvTime - sendTime;
            //Console.WriteLine("ent recv {0} diff {1}", sendTime, delay);
            //if (delay.TotalSeconds > World.SyncTime)
            //{
            //    Console.WriteLine("ent LATE!!! {0}s", delay.TotalSeconds - World.SyncTime);
            //}

            int seq = reader.GetInt();
            if (seq < SyncWorldState.Instance.ClientSequence)
            {
                Logger.Log(Logger.LogLevel.Warn, "Discarding SyncEntityState seq was old {0} - {1}", seq, SyncWorldState.Instance.ClientSequence);

                //Console.WriteLine("Discarding SyncBasicState - seq was old {0} - {1}", seq, SyncWorldState.Instance.ClientSequence);
                return;
            }

            int num = reader.GetInt();

            int ackI = 0;
            SyncEntityStateAck.EntityAckArr ackArr = new();

            if (playerTypeId == -1)
                playerTypeId = GlobalState.Registry.EntityRegistry.Get(typeof(Player).FullName).Id;

            int numBits = 0;
            int numBytes = 0;
            int beforeFirstEntRead = reader.Position;

            for (int i = 0; i < num; i++)
            {
                EntityManager.EntityReference reference = reader.Get<EntityManager.EntityReference>();

                SyncStateType type = (SyncStateType)reader.GetByte();
                if (type == SyncStateType.MinorSync || type == SyncStateType.MajorSync)
                {
                    int typeId = reader.GetUShort();

                    // TODO: we might want to base this on the last received state for this entity (before this ack)
                    // We'd need to store that somehow. Right now we just store the latest sequence we've ack'd globally...
                    var state = client.Current().entities.GetByRef(ref reference);
                    // Also deserializes extra fields
                    uint bits = state.DeserializeDelta(reader, false);
                    numBits += System.Numerics.BitOperations.PopCount(bits);

                    state.DeserializeDeltaExtraFields(reader, bits);

                    if (type == SyncStateType.MajorSync)
                    {
                        int numTrackedPos = reader.GetInt();
                        for (int j = 0; j < numTrackedPos; j++)
                        {
                            var cubePos = reader.Get<CubePosition>();
                            var chunkPos = ChunkPosition.CubeChunk(cubePos);
                            // NOTE:
                            // Cube tracker refs are sent to the server as a part of SyncChunk, but the actual entities are sent here.
                            // Since SyncChunk is not sent at any particular time, it can arrive before the entities it references are available.
                            // In this scenario we need to mark the chunks as dirty after the entities arrive,
                            // because trackers can be used to change how chunks are meshed (chest front face is different, for instance).
                            client.ChunkManager.CopyManager.MarkDirty(chunkPos);
                            client.ChunkManager.ChunkMesher.MarkChunkDirty(chunkPos);
                        }
                    }

                    var typeName = GlobalState.Registry.EntityRegistry.Get((int)typeId)?.Identifier;
                    Logger.Log(Logger.LogLevel.Info, "recv ent {0} {1}", reference.id, typeName);
                    //Console.WriteLine("Recv {0} {1}", reference.id, typeName);
                    if (typeName != null)
                    {
                        if (typeId == playerTypeId)
                        {
                            if (state.counters[3] == client.LocalPlayerIndex)
                            {
                                if (client.Current().entities.IsActive(ref reference) && client.LocalPlayer != null)
                                {
                                    // Update player
                                    // we do this slightly differently since the player entity has some stuff we don't want to overwrite
                                    ref var player = ref client.Current().entities.GetByRefPtr(ref reference);
                                    // Always keep client's rotation
                                    state.rotation = player.rotation;
                                    player = state;
                                    client.ChunkManager.PhysicsInfo.Simulation.Bodies[client.LocalPlayer.Body].Pose.Position = state.position.ToNumerics();
                                    client.ChunkManager.PhysicsInfo.Simulation.Bodies[client.LocalPlayer.Body].Velocity.Linear = state.velocity.ToNumerics();
                                    client.Current().camera.Position = state.position;
                                }
                                else
                                {
                                    // Create a new player
                                    client.Current().entities.Set(reference, typeName, state);
                                    client.LocalPlayer = new Clients.ClientLocalPlayer(ref reference, ref state);
                                    client.LocalPlayer.MakeNew(ref state, client.ChunkManager.PhysicsInfo);
                                    client.Current().camera.Rotation = state.rotation;
                                }
                            }
                            else
                                client.Current().entities.Set(reference, typeName, state);
                            client.Current().entities.AddPlayer(reference, state.counters[2], state.counters[3]);
                        } 
                        else
                        {
                            if (client.Current().entities.GetTypeById(reference.id) == playerTypeId) 
                            {
                                // We're killing or overwriting a player?

                            }
                            client.Current().entities.Set(reference, typeName, state);
                        }
                        // Check to make sure we're not trying to create or update an entity that was unloaded this framee
                        //var unloadedThisSeq = entities[0][reference.id].latestSequence == seq && entities[0][reference.id].unloadedThisSeq;
                        //if (unloadedThisSeq)
                        //    continue;

                        clientEntities[reference.id] = new SyncedEntity
                        {
                            reference = reference,
                            latestSequence = seq,
                            unloadedThisSeq = false,
                        };

                        ackArr[ackI] = reference;
                        ackI += 1;
                    } else
                    {
                        Console.WriteLine("Tried to create entity with type id {0}. This id does not exist!", typeId);
                        // Remove the entity to keep it from being outdated?
                        client.Current().entities.Remove(reference);
                    }
                }
                else if (type == SyncStateType.Unload)
                {
                    //Console.WriteLine("Server unloaded {0}", reference.id);

                    client.Current().entities.Remove(reference);

                    clientEntities[reference.id] = new SyncedEntity
                    {
                        reference = reference,
                        latestSequence = seq,
                        unloadedThisSeq = true,
                    };

                    int playerIndex = client.Current().entities.GetPlayerIndex(reference);
                    if (playerIndex != -1)
                    {
                        client.Current().entities.RemovePlayer(reference);
                        if (playerIndex == client.LocalPlayerIndex)
                        {
                            client.LocalPlayer.Unload(client.ChunkManager.PhysicsInfo);
                            client.LocalPlayer = null;
                        }
                    }

                    ackArr[ackI] = reference;
                    ackI += 1;
                }
                else
                {
                    throw new Exception();
                }
            }

            IMGUINetworkDebug.AddClientEntSync(seq, delay.TotalSeconds, num, numBits, reader.Position - beforeFirstEntRead);

            if (ackI > 0)
            {
                GS.netManagerClient?.SendMessageToPeer(SyncEntityStateAck.Instance, peer,
                    new SyncEntityStateAck.Ack
                    {
                        numAckd = ackI,
                        ackdEntities = ackArr,
                        sequence = seq,
                        initialSend = sendTime,
                        initialRecv = recvTime
                    });
            }
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

            public DateTime initialSend;
            public DateTime initialRecv;

            public void Deserialize(NetDataReader reader)
            {
                initialSend = new DateTime(reader.GetLong());
                initialRecv = new DateTime(reader.GetLong());
                sequence = reader.GetInt();
                numAckd = reader.GetInt();

                for (int i = 0; i < numAckd; i++)
                {
                    ackdEntities[i] = reader.Get<EntityManager.EntityReference>();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(initialSend.Ticks);
                writer.Put(initialRecv.Ticks);
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

            SyncEntityState.Instance.AddAck(ack, GS.netManagerServer.GetNetPlayer(peer).playerId, ack.sequence);
        }
    }
}
