using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace Engine.Clients.Entities
{
    public class CubeTrackers
    {
        public Dictionary<ChunkPosition, ChunkCubeTrackers> trackers = new();

        public CubeTrackers()
        {
        }

        public ChunkCubeTrackers Get(ChunkPosition chunkPosition)
        {
            if (!trackers.TryGetValue(chunkPosition, out ChunkCubeTrackers? ret))
            {
                ret = new();
                trackers.Add(chunkPosition, ret);
            } 

            return ret;
        }
    }

    public class ChunkCubeTrackers
    {
        public EntityManager.EntityReference[] cubeTrackers;

        public ChunkPosition chunkPosition;

        public int count;

        public virtual void Add(CubePosition chunkSpacePosition, EntityManager.EntityReference reference)
        {
            Util.ThreeDToOneD(new ValuePoint3D(chunkSpacePosition.X, chunkSpacePosition.Y, chunkSpacePosition.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            if (cubeTrackers == null)
            {
                cubeTrackers = new EntityManager.EntityReference[Chunk.NUM_CUBES_IN_CHUNK];
                //cubeTrackers = new BasicState[Chunk.NUM_CUBES_IN_CHUNK];
            }

            cubeTrackers[i] = reference;

            count++;
        }

        public virtual void Remove(CubePosition chunkSpacePosition)
        {
            Util.ThreeDToOneD(new ValuePoint3D(chunkSpacePosition.X, chunkSpacePosition.Y, chunkSpacePosition.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            cubeTrackers[i] = new();

            count--;
        }

        public virtual EntityManager.EntityReference Get(CubePosition chunkSpacePosition)
        {
            Util.ThreeDToOneD(new ValuePoint3D(chunkSpacePosition.X, chunkSpacePosition.Y, chunkSpacePosition.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            return cubeTrackers[i];
        }
    }

    public class ChunkCubeTrackersDummy : ChunkCubeTrackers 
    {
        public override void Add(CubePosition chunkSpacePosition, EntityManager.EntityReference state)
        {
            
        }

        public override EntityManager.EntityReference Get(CubePosition chunkSpacePosition)
        {
            return new();
        }

        public override void Remove(CubePosition chunkSpacePosition)
        {
            
        }
    }
}
