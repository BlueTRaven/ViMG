using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation.DirectX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
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
            public Entity entity;
        }

        public static SyncBasicState Instance { get; private set; }
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Both;

        private struct QueuedSyncEntity
        {
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

            if (entity.entity == null) Debug.Assert(false);

            if (entity.type == SyncType.BasicState && entity.entity is not ISyncBasicState)
            {
                Console.WriteLine("Couldn't do BasicState sync for entity {0} - it does not implement ISyncBasicState!", entity.entity);
                return;
            }

            if (entity.type == SyncType.BasicState || entity.type == SyncType.SuperSimple)
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
                    netMessage.writer.PutArray(bytes.ToArray(), sizeof(byte));
                    break;
            }

            //if (entity.entity != null && entity.entity is Player)
            //    Console.WriteLine("Do sync: {0}", entity.type.ToString());
            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

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
            }

            //Console.WriteLine("Received player sync 2 {0} {1} {2}", time, type.ToString(), id);
            queued.Add(local);

            //Entity? ent = GS.GetWorld().EntityManager.GetById(id);
            //// NOTE: SuperSimple and BasicState state is completely ignored if the entity does not exist.
            //// It is not an error for a client to receive sync state for an entity that does not exist.
            //if (ent != null)
            //{
            //    switch (type)
            //    {
            //        case SyncType.SuperSimple:
            //            ent.Position.X = reader.GetFloat();
            //            ent.Position.Y = reader.GetFloat();
            //            ent.Position.Z = reader.GetFloat();
            //            break;
            //        case SyncType.BasicState:
            //            var bstate = reader.Get<BasicState>();
            //            if (ent is ISyncBasicState syncer)
            //            {
            //                syncer.Set(ref bstate);
            //            }
            //            break;
            //        case SyncType.FullSync:
            //            break;
            //        case SyncType.EntityUnloaded:
            //            GS.GetWorld().EntityManager.Unload(ent);
            //            break;
            //    }

            //    ent.TimeSynced = time;
            //} 
            
            //// Full Sync has special behavior; if an entity does not already exist, it is created
            //// TODO: what happens if we receive an EntityUnloaded and then this?
            //if (type == SyncType.FullSync)
            //{
            //    byte[] bytes = reader.GetArray<byte>(sizeof(byte));
            //    EntityManagerIO.EntityData data = new();
            //    data.Load(bytes);
            //    if (data.IsValid)
            //    {
            //        if (ent == null)
            //            ent = GS.GetWorld().EntIO.DeserializeEntity(data);
            //        else ent.OnLoad(data.data, data.version);

            //        if (ent != null)
            //            ent.TimeSynced = time;
            //    }
            //}
        }

        public void Apply(EntityManager entityManager, EntityManagerIO entIO)
        {
            var otherBuffer = queued == queued1 ? queued2 : queued1;

            foreach (QueuedSyncEntity queuedSync in queued)
            {
                if (Main.Time > queuedSync.time)
                {
                    //Console.WriteLine("Received player sync 1 {0}", queuedSync.type.ToString());

                    Entity? ent = entityManager.GetById(queuedSync.entityId);
                    // NOTE: SuperSimple and BasicState state is completely ignored if the entity does not exist.
                    // It is not an error for a client to receive sync state for an entity that does not exist.
                    if (ent != null)
                    {
                        //if (ent is Player)
                        //{
                        //    Console.WriteLine("Received player sync {0}", queuedSync.type.ToString());
                        //}
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
                else
                {
                    otherBuffer.Add(queuedSync);
                }
            }

            queued.Clear();
            // Swap buffers
            queued = otherBuffer;
        }
    }
}
