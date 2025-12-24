using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Clients.Entities
{
    public class CubeTrackers
    {
        public Dictionary<ChunkPosition, ChunkCubeTrackers> trackers = new();

        private ChunkCubeTrackersDummy dummy = new ChunkCubeTrackersDummy();
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
        public BasicState[] cubeTrackers;

        public ChunkPosition chunkPosition;

        public int count;

        public virtual void Add(CubePosition chunkSpacePosition, BasicState state)
        {
            Util.ThreeDToOneD(new ValuePoint3D(chunkSpacePosition.X, chunkSpacePosition.Y, chunkSpacePosition.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            if (cubeTrackers == null)
            {
                cubeTrackers = new BasicState[Chunk.NUM_CUBES_IN_CHUNK];
            }

            count++;
        }

        public virtual void Remove(CubePosition chunkSpacePosition)
        {
            Util.ThreeDToOneD(new ValuePoint3D(chunkSpacePosition.X, chunkSpacePosition.Y, chunkSpacePosition.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            cubeTrackers[i] = new();

            count--;
        }

        public virtual BasicState Get(CubePosition chunkSpacePosition)
        {
            Util.ThreeDToOneD(new ValuePoint3D(chunkSpacePosition.X, chunkSpacePosition.Y, chunkSpacePosition.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            return cubeTrackers[i];
        }
    }

    public class ChunkCubeTrackersDummy : ChunkCubeTrackers 
    {
        public override void Add(CubePosition chunkSpacePosition, BasicState state)
        {
            
        }

        public override BasicState Get(CubePosition chunkSpacePosition)
        {
            return new();
        }

        public override void Remove(CubePosition chunkSpacePosition)
        {
            
        }
    }
}
