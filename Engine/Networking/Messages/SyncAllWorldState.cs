using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            NetworkManager.NetPlayer sendTo = addObj as NetworkManager.NetPlayer? ?? throw new NullReferenceException();
            writer.Put(0);
            var playerEntityData = new EntityManagerIO.EntityData(GS.GetWorld().player[sendTo.playerId]);
            List<byte> bytes = new List<byte>();
            playerEntityData.Save(bytes);
            writer.PutArray(bytes.ToArray(), sizeof(byte));
            writer.Put(-1);
            //MemoryStream ms = new MemoryStream();
            //GS.GetWorld()?.ChunkIO.SaveToStream(ms);
            //int len = (int)ms.Length;
            //writer.Put(len);
            //writer.PutBytesWithLength(ms.GetBuffer(), 0, len);
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            GS.GetWorld().ChunkLoadManager.UnloadAll();
            //var len = reader.GetInt();
            //reader.GetBytes(GS.GetWorld().ChunkIO.GetBytes(), len);
            int section = reader.GetInt();
            while (section != -1)
            {
                if (section == 0)
                {
                    byte[] bytes = reader.GetArray<byte>(sizeof(byte));
                    EntityManagerIO.EntityData data = new();
                    data.Load(bytes);
                    if (data.IsValid)
                    {
                        Player p = new Player();
                        p.playerIndex = GS.GetWorld().localPlayerIndex;
                        p.OnLoad(data.data, data.version);

                        GS.GetWorld().EntityManager.Add(p);
                        GS.GetWorld().player[GS.GetWorld().localPlayerIndex] = p;
                        GS.GetWorld().ChunkLoadManager.UpdateLoadTarget(p.Position);
                        GS.GetWorld().ChunkLoadManager.LoadAroundTarget(GS.GetWorld());
                    }
                }
                section = reader.GetInt();
            }
        }
    }
}
