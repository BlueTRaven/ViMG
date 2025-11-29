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

        public struct ChunkToSync
        {
            public ChunkPosition chunkPosition;
            public ushort[]? ids;
        }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        private List<CubeView.PalettizedChunk> chunksToLoad = new List<CubeView.PalettizedChunk>();

        public SyncChunk()
        {
            Instance = this;
        }

        private static ushort[] idsCache = new ushort[Chunk.NUM_CUBES_IN_CHUNK];
        public unsafe override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;
            netMessage.channel = 1;

            Span<ushort> queryIds = idsCache;

            var chunkToSync = addData as ChunkToSync? ?? throw new Exception();

            if (chunkToSync.ids != null)
                queryIds = chunkToSync.ids;
            else
            {
                GS.GetWorld().ChunkManager.CubeView.GetIdsForChunk(chunkToSync.chunkPosition, idsCache);
            }

            var chunk = GS.GetWorld().ChunkManager.CubeView.Palettize(chunkToSync.chunkPosition, queryIds);

            //Span<byte> bytes = MemoryMarshal.AsBytes(queryIds);
            netMessage.writer.Put(chunkToSync.chunkPosition);
            netMessage.writer.Put((int)chunk.type);
            netMessage.writer.PutArray(chunk.palette);
            if (chunk.type != CubeView.PalettizeType.AllOneId)
                netMessage.writer.PutBytesWithLength(chunk.data, 0, (ushort)chunk.data.Length);
            //netMessage.writer.PutSpan(bytes);

            netMessage.Send();
        }

        public unsafe override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            var chunkPosition = reader.Get<ChunkPosition>();

            CubeView.PalettizeType paletteType = (CubeView.PalettizeType)reader.GetInt();
            var palette = reader.GetUShortArray();
            var data = paletteType == CubeView.PalettizeType.AllOneId ? null : reader.GetArray<byte>(sizeof(byte));

            var chunk = new CubeView.PalettizedChunk
            {
                data = data,
                palette = palette,
                position = chunkPosition,
                type = paletteType,
            };
            chunksToLoad.Add(chunk);
        }

        public unsafe void Apply(ChunkManager chunkManager, ChunkLoadManager chunkLoadManager)
        {
            Span<CubePosition> queryPositions = stackalloc CubePosition[Chunk.NUM_CUBES_IN_CHUNK];

            foreach (CubeView.PalettizedChunk chunkToLoad in chunksToLoad)
            {
                var ids = chunkManager.CubeView.Depaletteize(chunkToLoad);

                CubePosition basePosition = chunkToLoad.position.InCubeSpace();

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

                chunkManager.CubeView.SetCubes(queryPositions, ids);
                chunkLoadManager.Unload(chunkToLoad.position);
                chunkLoadManager.MarkDirty(chunkToLoad.position);
            }

            chunksToLoad.Clear();
        }
    }
}
