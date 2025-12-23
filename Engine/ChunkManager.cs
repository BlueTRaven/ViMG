using BepuUtilities.Memory;
using BrUtility;
using Engine.ChunkStuff;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG
{
    //Try to stay away from dependance on World if possible
    public class ChunkManager
    {
        public readonly struct CubeUpdated
        {
            public readonly Player? player;
            public readonly double timeUpdated;
            public readonly CubePosition updated;
            public readonly CubePosition notified;
            public readonly ushort oldId;
            public readonly ushort newId;

            public CubeUpdated(Player? player, double timeUpdated, CubePosition updated, CubePosition notified, ushort oldId, ushort newId)
            {
                this.player = player;
                this.timeUpdated = timeUpdated;
                this.updated = updated;
                this.notified = notified;
                this.oldId = oldId;
                this.newId = newId;
            }
        }

        //[StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct CubeMeshInfo
        {
            public MeshHelper.CubeFace faces;
            public byte meshVersion;
            public byte version;

            public CubeMeshInfo(MeshHelper.CubeFace faces)
            {
                this.faces = faces;

                meshVersion = 0;
                version = 1;
            }
        }

        private static ChunkPosition[] chunkAdjacents = new ChunkPosition[6]
        {
            new ChunkPosition(-1, 0, 0),
            new ChunkPosition(1, 0, 0),
            new ChunkPosition(0, -1, 0),
            new ChunkPosition(0, 1, 0),
            new ChunkPosition(0, 0, -1),
            new ChunkPosition(0, 0, 1),
        };


        private static CubePosition[] cubeAdjacents = new CubePosition[6]
        {
            new CubePosition(-1, 0, 0),
            new CubePosition(1, 0, 0),
            new CubePosition(0, -1, 0),
            new CubePosition(0, 1, 0),
            new CubePosition(0, 0, -1),
            new CubePosition(0, 0, 1),
        };

        public const int NUM_CHUNK_MESH_PASSES = 5;
        private const int SIZEOF_CHUNK = (sizeof(ushort) * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);

        public readonly int SizeInChunksXZ;
        public readonly int SizeInCubes;
        public readonly ChunkMesher? ChunkMesher;

        public CubeView CubeView;

        //private CubeMeshInfo[] cubeMeshInfos;
        private Queue<CubeUpdated> updatedCubePositions = new Queue<CubeUpdated>();

        public ChunkManager(int sizeInChunksXZ, ChunkManagerIO io, ChunkMesher? chunkMesher)
        {
            this.SizeInChunksXZ = sizeInChunksXZ;
            this.SizeInCubes = sizeInChunksXZ * Chunk.CHUNK_SIZE;

            ChunkMesher = chunkMesher;

            int size = Marshal.SizeOf<CubeMeshInfo>();
            
            CubeView = new CubeView(this, io);
        }

        private FastList<CubeUpdated> uniqueUpdates = new FastList<CubeUpdated>();

        public void Update(double deltaTime, World world, ChunkLoadManager loadManager)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            ChunkMesher?.Update(world);

            const int MAX_UPDATE_PER_FRAME = 200;
            int updatedThisFrame = 0; 

            //Notify anyone who might want to know that a cube was updated. This includes adjacents.
            while (updatedCubePositions.Count > 0 && updatedThisFrame < MAX_UPDATE_PER_FRAME)
            {
                CubeUpdated updated = updatedCubePositions.Dequeue();

                if (updated.notified == updated.updated)
                {
                    world.OnCubeUpdate(updated.updated, updated.newId);
                    var entityTracking = world.EntityManager.GetEntityTrackingPosition(updated.updated).GetOrDefault(null);

                    if (entityTracking != null)
                    {
                        if (entityTracking is ICubeTracker tracker)
                            tracker.TrackingCubeUpdated(world, this, updated.player, updated.newId);
                        else if (entityTracking is IMultiCubeTracker multiTracker)
                            multiTracker.TrackingCubeUpdated(world, this, updated.player, updated.updated, updated.newId, updated.timeUpdated);
                    }

                    Main.gameStateManager.TheIsland.netManagerServer.SendMessageToAll(SyncCubeUpdate.Instance, Main.gameStateManager.TheIsland.netManagerServer.netManager, updated);
                }
                else CubeView.GetCube(updated.notified).GetOrDefault(Main.Registry.CubeRegistry.Air)
                        .OnAdjacentUpdated(world, this, updated.notified, updated.updated, updated.newId, updated.timeUpdated);

                updatedThisFrame++;
            }
        }

        public bool IsInWorldBounds(Vector3 position)
        {
            return IsInWorldBounds(CubePosition.FromWorldSpace(position));
        }

        public bool IsInWorldBounds(CubePosition position)
        {
            int sign = MathF.Sign(position.Y);

            //TODO: layer stuff
            //int layer = LayerFromPos(position);

            //if (layer > discoveredLayers)
                //return false;

            //position = LayerRelativePosition(position);

            if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
                return false;
            else
            {
                //positive sign/0 (Sign(0) == 0)
                //Below 0, the y axis has its inclusivity/exclusivity reversed (aka, min exclusive, max inclusive, instead of the opposite).
                //Note that this only applies to the y axis and no other.
                if (sign >= 0)
                {
                    return position.X >= 0 && position.X < SizeInCubes &&
                        position.Y >= 0 && position.Y < SizeInCubes &&
                        position.Z >= 0 && position.Z < SizeInCubes;
                }
                else if (sign == -1)
                {
                    return position.X >= 0 && position.X < SizeInCubes &&
                        position.Y > 0 && position.Y <= SizeInCubes &&
                        position.Z >= 0 && position.Z < SizeInCubes;
                }
                else return false;
            }
        }

        public bool IsInWorldBounds(ChunkPosition position)
        {
            return position.X >= 0 && position.X < SizeInChunksXZ &&
                    position.Y >= 0 && position.Y < SizeInChunksXZ &&
                    position.Z >= 0 && position.Z < SizeInChunksXZ;
        }

        public void MarkCubeMeshInfoDirty(Player? player, CubePosition position, ushort oldId, ushort updatedId)
        {
            updatedCubePositions.Enqueue(new CubeUpdated(player, Main.Time, position, position, oldId, updatedId));

            for (int i = 0; i < 6; i++)
            {
                CubePosition adjacentPosition = position + cubeAdjacents[i];

                if (IsInWorldBounds(adjacentPosition))
                {
                    //Don't bother marking the original chunk as dirty since at least 1 of these six adjacents is guaranteed to be in the same chunk.
                    ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(adjacentPosition));

                    updatedCubePositions.Enqueue(new CubeUpdated(player, Main.Time, position, adjacentPosition, oldId, updatedId));
                }
            }
        }
    }
}
