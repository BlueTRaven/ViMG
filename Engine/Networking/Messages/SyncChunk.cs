using BepuPhysics.Constraints;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    // Syncs chunk data. Doesn't include entities?
    public class SyncChunk : Message
    {
        public static SyncChunk Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public SyncChunk()
        {
            Instance = this;
        }
 
        public unsafe override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            ChunkPosition chunkPos = addData as ChunkPosition? ?? throw new Exception();

            Span<ushort> queryIds = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];
            GS.GetWorld().ChunkManager.CubeView.GetIdsForChunk(chunkPos, queryIds);
            //GS.GetWorld().ChunkManager.CubeView.GetIds(queryPositions, queryIds);

            Span<byte> bytes = MemoryMarshal.AsBytes(queryIds);
            //var writer1 = new NetDataWriter();
            netMessage.writer.Put(chunkPos);
            netMessage.writer.PutSpan(bytes);

            netMessage.Send();
        }

        public unsafe override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            var chunkPosition = reader.Get<ChunkPosition>();

            var ids = reader.GetArray<ushort>(sizeof(byte));

            CubePosition basePosition = chunkPosition.InCubeSpace();

            Span<CubePosition> queryPositions = stackalloc CubePosition[Chunk.NUM_CUBES_IN_CHUNK];
            fixed (CubePosition* queryPositionsPtr = queryPositions)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                        {
                            Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
                            CubePosition pos = basePosition + new CubePosition(x, y, z);
                            queryPositionsPtr[i] = pos;
                        }
                    }
                }
            }

            GS.GetWorld().ChunkManager.CubeView.SetCubes(queryPositions, ids);
            GS.GetWorld().ChunkLoadManager.Unload(chunkPosition);
            GS.GetWorld().ChunkLoadManager.MarkDirty(chunkPosition);
        }
    }
}
