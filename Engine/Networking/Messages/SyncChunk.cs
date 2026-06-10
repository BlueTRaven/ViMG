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
    // Syncs chunk data.
    public class SyncChunk : Message
    {
        public static SyncChunk Instance { get; private set; }

        public struct ChunkToSync
        {
            public ChunkPosition chunkPosition;
            public ushort[]? ids;
        }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        private List<ChunkToSync> chunksToApply;

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
            if (GlobalState.NetMode == NetworkingMode.Server)
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

                    var reference = GS.GetWorld().EntityManager.GetReference((int)ent.Id);
                    subwriter.Put(reference);
                    //subwriter.Put(GS.GetWorld().EntityManager.GetPrevState((int)ent.Id, 0));
                    subwriter.Put((ushort)1);
                    subwriter.Put(cubeTracker.TrackedPosition.InChunkSpace());

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

                    var reference = GS.GetWorld().EntityManager.GetReference((int)ent.Id);
                    subwriter.Put(reference);
                    //subwriter.Put(GS.GetWorld().EntityManager.GetPrevState((int)ent.Id, 0));
                    subwriter.Put((ushort)cubeTracker.TrackedPositions.Count());
                    foreach (CubePosition trackedPosition in cubeTracker.TrackedPositions)
                    {
                        subwriter.Put(trackedPosition.InChunkSpace());
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
            if (GlobalState.NetMode == NetworkingMode.Client)
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
                var reference = reader.Get<EntityManager.EntityReference>();
                var trackers = GS.GetClient().ChunkManager.CubeTrackers.Get(chunkPosition);
                int trackedPositionsNum = reader.GetUShort();
                for (int j = 0; j < trackedPositionsNum; j++)
                {
                    CubePosition position = reader.Get<CubePosition>();
                    trackers.Add(position, reference);
                }
            }
        }

        public void Apply(ChunkManager chunkManager)
        {
        }
    }
}
