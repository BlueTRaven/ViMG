using BrUtility;
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
        private const int SECTION_END = -1;
        private const int SECTION_PLAYERDATA = 0;
        private const int SECTION_CHUNKDATA = 1;

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
            {
                writer.Put(SECTION_PLAYERDATA);
                var playerEntityData = new EntityManagerIO.EntityData(GS.GetWorld().player[sendTo.playerId]);
                List<byte> bytes = new List<byte>();
                playerEntityData.Save(bytes);
                writer.PutArray(bytes.ToArray(), sizeof(byte));
            }

            //{

            //    writer.Put(SECTION_CHUNKDATA); 
            //    MemoryStream ms = new MemoryStream();
            //    GS.GetWorld()?.ChunkIO.SaveToStream(ms);
            //    byte[] arr = ms.GetBuffer();
            //    int actuallyWritten = writer.PutArray(arr, sizeof(byte));
            //    Console.WriteLine("SyncAllWorldState: Writing {0} bytes", arr.Length);
            //    Debug.Assert(actuallyWritten == arr.Length + 2);
            //}
            writer.Put(SECTION_END);
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            GS.GetWorld().ChunkLoadManager.UnloadAll();
            int section = reader.GetInt();
            while (section != SECTION_END)
            {
                if (section == SECTION_PLAYERDATA)
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
                else if (section == SECTION_CHUNKDATA)
                {
                    byte[] bytes = reader.GetArray<byte>(sizeof(byte));
                    Console.WriteLine("SyncAllWorldState: Read {0} bytes", bytes.Length);

                    //var worldBytes = GS.GetWorld().ChunkIO.GetBytes();
                    GS.GetWorld().ChunkIO.LoadFromStream(new MemoryStream(bytes));
                    //Debug.Assert(bytes.Length == worldBytes.Length);
                }
                section = reader.GetInt();
            }
        }
    }
}
