using BepuPhysics.Constraints;
using BrUtility;
using Engine.Common;
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
using ViMG.Entities;

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

        private List<PalettizedChunk> chunksToLoad = new List<PalettizedChunk>();

        public SyncChunk()
        {
            Instance = this;
        }

        private static ushort[] idsCache = new ushort[Chunk.NUM_CUBES_IN_CHUNK];
        public unsafe override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;
            netMessage.channel = (int)NetworkMessage.Channels.Chunks;

            Span<ushort> queryIds = idsCache;

            var chunkToSync = addData as ChunkToSync? ?? throw new Exception();

            if (chunkToSync.ids != null)
                queryIds = chunkToSync.ids;
            else
            {
                GS.GetWorld().ChunkManager.CubeView.GetIdsForChunk(chunkToSync.chunkPosition, idsCache);
            }

            var chunk = PalettizedChunk.Palettize(chunkToSync.chunkPosition, queryIds);

            netMessage.writer.Put(chunkToSync.chunkPosition);
            if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Server)
            {
                netMessage.writer.Put((int)chunk.type);
                netMessage.writer.PutArray(chunk.palette);
                if (chunk.type != PalettizeType.AllOneId)
                    netMessage.writer.PutBytesWithLength(chunk.data, 0, (ushort)chunk.data.Length);
            }

            FastList<ICubeTracker> trackers = new();
            FastList<IMultiCubeTracker> mtrackers = new();
            GS.GetWorld().EntityManager.GetAllTrackersForChunk(chunkToSync.chunkPosition, trackers, mtrackers);
            FastList<CubePosition> trackedPositions = new FastList<CubePosition>();
            int trackerStateI = 0;

            List<NetDataWriter> subwriters = new List<NetDataWriter>();

            foreach (var cubeTracker in trackers.Slice())
            {
                Debug.Assert(cubeTracker is Entity);
                if (cubeTracker is Entity ent)
                {
                    NetDataWriter subwriter = new NetDataWriter();
                    subwriters.Add(subwriter);

                    subwriter.Put(GS.GetWorld().EntityManager.GetPrevState((int)ent.Id, 0));
                    subwriter.Put((ushort)1);
                    subwriter.Put(cubeTracker.TrackedPosition);

                    trackerStateI += 1;
                }
            }
            foreach (var cubeTracker in mtrackers.Slice())
            {
                Debug.Assert(cubeTracker is Entity);
                if (cubeTracker is Entity ent)
                {
                    NetDataWriter subwriter = new NetDataWriter();
                    subwriters.Add(subwriter);

                    subwriter.Put(GS.GetWorld().EntityManager.GetPrevState((int)ent.Id, 0));
                    subwriter.Put((ushort)cubeTracker.TrackedPositions.Count());
                    foreach (CubePosition trackedPosition in cubeTracker.TrackedPositions)
                    {
                        subwriter.Put(trackedPosition);
                    }

                    trackerStateI += 1;
                }
            }

            netMessage.writer.Put(subwriters.Count);
            for (int i = 0; i < subwriters.Count; i++)
            {
                NetDataWriter subwriter = subwriters[i];
                netMessage.writer.Put(subwriter.AsReadOnlySpan());
            }

            netMessage.Send();
        }

        public unsafe override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            var chunkPosition = reader.Get<ChunkPosition>();
            if (chunkPosition == new ChunkPosition(17, 12, 17))
            {
                Console.Write("");
            }
            if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Client)
            {
                PalettizeType paletteType = (PalettizeType)reader.GetInt();
                var palette = reader.GetUShortArray();
                var data = paletteType == PalettizeType.AllOneId ? null : reader.GetArray<byte>(sizeof(byte));
                var chunk = new PalettizedChunk
                {
                    data = data,
                    palette = palette,
                    position = chunkPosition,
                    type = paletteType,
                };
                chunksToLoad.Add(chunk);
                GS.GetClient().ChunkManager.ChunkIO.SetChunk(ref chunk);
            }
            else
            {
                GS.GetClient().ChunkManager.MaybeSetToServer();
                Debug.Assert(GS.GetClient().ChunkManager.ChunkIO.IsLoaded(chunkPosition));
            }

            GS.GetClient().ChunkManager.CopyManager.MarkDirty(chunkPosition);
            // Mark all chunks in a 3x3x3 radius around as dirty
            // We can't ignore meshing a chunk if we don't have one of its adjacent chunks
            for (int i = 0; i < 3 * 3 * 3; i++)
            {
                Util.OneDToThreeD(i, new ValuePoint3D(3), out var point);
                ChunkPosition dirtyChunk = chunkPosition + new ChunkPosition(point.x - 1, point.y - 1, point.z - 1);
                GS.GetClient().ChunkManager.ChunkMesher.MarkChunkDirty(dirtyChunk);
            }

            int numTrackers = reader.GetInt();
            for (int i = 0; i < numTrackers; i++)
            {
                var state = reader.Get<BasicState>();
                var trackers = GS.GetClient().cubeTrackers.Get(chunkPosition);
                int trackedPositionsNum = reader.GetUShort();
                for (int j = 0; j < trackedPositionsNum; j++)
                {
                    CubePosition position = reader.Get<CubePosition>();
                    trackers.Add(position, state);
                }
            }
        }

        public unsafe void Apply(ChunkManager chunkManager, ChunkLoadManager chunkLoadManager)
        {
            //foreach (var chunkToLoad in chunksToLoad) 
            //{
            //    Span<CubePosition> queryPositions = stackalloc CubePosition[Chunk.NUM_CUBES_IN_CHUNK];
            //    var ids = CubeView.Depaletteize(chunkToLoad);

            //    CubePosition basePosition = chunkToLoad.position.InCubeSpace();

            //    fixed (CubePosition* queryPositionsPtr = queryPositions)
            //    {
            //        for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            //        {
            //            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            //            {
            //                for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            //                {
            //                    Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
            //                    CubePosition pos = basePosition + new CubePosition(x, y, z);
            //                    queryPositionsPtr[i] = pos;
            //                }
            //            }
            //        }
            //    }

            //    chunkManager.CubeView.SetCubes(queryPositions, ids);
            //}

            //foreach (var chunkToLoad in chunksToLoad)
            //{
            //    chunkLoadManager.Unload(chunkToLoad.position);
            //    chunkLoadManager.MarkDirty(chunkToLoad.position);
            //}

            //foreach (CubeView.PalettizedChunk chunkToLoad in chunksToLoad)
            //{
            //    var ids = chunkManager.CubeView.Depaletteize(chunkToLoad);

            //    CubePosition basePosition = chunkToLoad.position.InCubeSpace();

            //    fixed (CubePosition* queryPositionsPtr = queryPositions)
            //    {
            //        for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            //        {
            //            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            //            {
            //                for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            //                {
            //                    Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
            //                    CubePosition pos = basePosition + new CubePosition(x, y, z);
            //                    queryPositionsPtr[i] = pos;
            //                }
            //            }
            //        }
            //    }

            //    chunkManager.CubeView.SetCubes(queryPositions, ids);
            //    chunkLoadManager.Unload(chunkToLoad.position);
            //    chunkLoadManager.MarkDirty(chunkToLoad.position);
            //}

            chunksToLoad.Clear();
        }
    }
}
