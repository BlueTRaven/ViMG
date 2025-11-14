using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    public class SyncAllWorldState : Message
    {
        public static SyncAllWorldState Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public SyncAllWorldState()
        {
            Instance = this;
        }

        public override void SendMessage(NetDataWriter writer, object? addObj)
        {
            base.SendMessage(writer, addObj);

            MemoryStream ms = new MemoryStream();
            GS.GetWorld()?.ChunkIO.SaveToStream(ms);
            writer.Put(ms.Length);
            writer.PutBytesWithLength(ms.GetBuffer());
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            var len = reader.GetInt();
            reader.GetBytes(GS.GetWorld().ChunkIO.GetBytes(), len);
        }
    }
}
