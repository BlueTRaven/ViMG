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
    [Obsolete]
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

        public override void SendMessage(NetworkMessage netMessage, object? addObj)
        {
            base.SendMessage(netMessage, addObj);

            NetworkManager.NetPlayer sendTo = addObj as NetworkManager.NetPlayer? ?? throw new NullReferenceException();
            {
                netMessage.writer.Put(SECTION_PLAYERDATA);
                netMessage.writer.Put(sendTo.playerId);
                netMessage.writer.Put(GS.GetWorld().player.Where(x => x != null).Count());

                foreach (var player in GS.GetWorld().player)
                {
                    if (player != null)
                    {
                        netMessage.writer.Put(player.playerIndex);

                        var playerEntityData = new EntityManagerIO.EntityData(player);
                        List<byte> bytes = new List<byte>();
                        playerEntityData.Save(bytes);
                  
                        netMessage.writer.PutArray(bytes.ToArray(), sizeof(byte));
                    }
                }
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
            netMessage.writer.Put(SECTION_END);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            GS.GetWorld().ChunkLoadManager.UnloadAll();
            int section = reader.GetInt();
            while (section != SECTION_END)
            {
                if (section == SECTION_PLAYERDATA)
                {
                    int localPlayerId = reader.GetInt();
                    int numPlayers = reader.GetInt();

                    for (int i = 0; i < numPlayers; i++)
                    {
                        int playerIndex = reader.GetInt();

                        byte[] bytes = reader.GetArray<byte>(sizeof(byte));
                        EntityManagerIO.EntityData data = new();
                        data.Load(bytes);
                        if (data.IsValid)
                        {
                            Player p = new Player(playerIndex, -1);
                            //p.OnLoad( data.data, data.version);

                            GS.GetWorld().EntityManager.ForceAdd(p, data.id);
                            GS.GetWorld().player[playerIndex] = p;

                            if (playerIndex == localPlayerId)
                            {
                                GS.GetWorld().ChunkLoadManager.LoadAroundTarget(GS.GetWorld());
                            }
                        }
                    }
                    //byte[] bytes = reader.GetArray<byte>(sizeof(byte));
                    //EntityManagerIO.EntityData data = new();
                    //data.Load(bytes);
                    //if (data.IsValid)
                    //{
                    //    Player p = new Player();
                    //    p.playerIndex = GS.GetWorld().localPlayerIndex;
                    //    p.OnLoad(data.data, data.version);

                    //    GS.GetWorld().EntityManager.ForceAdd(p, data.id);
                    //    GS.GetWorld().player[GS.GetWorld().localPlayerIndex] = p;
                    //}
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
