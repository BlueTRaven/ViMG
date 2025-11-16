using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace Engine.Networking.Messages
{
    public class SyncBasicState : Message
    {
        public enum SyncType
        {
            FullSync, // Sync with full serialization of entity
            // Sync with ISyncBasicState implementation if available
            // (if not, does not do anything)
            BasicState, 
            SuperSimple, // Just id and position
        }

        public struct SyncEntity
        {
            public SyncType type;
            public Entity entity;
        }

        public static SyncBasicState Instance { get; private set; }
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Both;

        public SyncBasicState()
        {
            Instance = this;
            Passthrough = true;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            SyncEntity entity = addData as SyncEntity? ?? throw new Exception();
            if (entity.entity == null) Debug.Assert(false);

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
                    break;
                case SyncType.FullSync:
                    if (entity.entity.Serialize)
                    {
                        var entityData = new EntityManagerIO.EntityData(entity.entity);
                        List<byte> bytes = new List<byte>();
                        entityData.Save(bytes);
                        netMessage.writer.PutArray(bytes.ToArray(), sizeof(byte));
                    }
                    break;
            }
            

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            double time = reader.GetDouble();

            ulong id = reader.GetULong();

            SyncType type = (SyncType)reader.GetInt();

            Entity? ent = GS.GetWorld().EntityManager.GetById(id);
            if (ent != null)
            {
                switch (type)
                {
                    case SyncType.SuperSimple:
                        ent.Position.X = reader.GetFloat();
                        ent.Position.Y = reader.GetFloat();
                        ent.Position.Z = reader.GetFloat();
                        break;
                    case SyncType.BasicState:
                        if (ent is ISyncBasicState syncer)
                        {
                            var bstate = reader.Get<BasicState>();
                            syncer.Set(ref bstate);
                        }
                        break;
                    case SyncType.FullSync:
                        byte[] bytes = reader.GetArray<byte>(sizeof(byte));
                        EntityManagerIO.EntityData data = new();
                        data.Load(bytes);
                        if (data.IsValid)
                        {
                            ent.OnLoad(data.data, data.version);
                            // TODO get by id and set. If not created, add
                        }
                        break;
                }

                ent.TimeSynced = time;
            }
        }
    }
}
