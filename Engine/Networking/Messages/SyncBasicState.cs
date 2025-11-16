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

            Entity entity = addData as Entity;
            if (entity == null) Debug.Assert(false);

            netMessage.writer.Put(Main.Time);
            entity.TimeSynced = Main.Time;

            netMessage.writer.Put(entity.Id);
            netMessage.writer.Put(entity.Position.X);
            netMessage.writer.Put(entity.Position.Y);
            netMessage.writer.Put(entity.Position.Z);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            double time = reader.GetDouble();

            ulong id = reader.GetULong();

            Entity? ent = GS.GetWorld().EntityManager.GetById(id);
            if (ent != null)
            {
                ent.Position.X = reader.GetFloat();
                ent.Position.Y = reader.GetFloat();
                ent.Position.Z = reader.GetFloat();
                ent.TimeSynced = time;
            }
        }
    }
}
