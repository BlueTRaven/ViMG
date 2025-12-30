using Engine.ChunkStuff;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;

namespace Engine.Clients
{
    public class ClientChunkManager
    {
        public ClientCubeView CubeView;
        public ChunkManagerIO ChunkIO;
        public ChunkMesher ChunkMesher;
        public CopiedChunkManager CopyManager;

        public int SizeInChunks = 32;

        public ClientChunkManager(GraphicsDevice device)
        {
            ChunkIO = new ChunkManagerIO(SizeInChunks, "", 0);
            CubeView = new ClientCubeView(ChunkIO, SizeInChunks);
            ChunkMesher = ChunkMesher.RenderOnly(SizeInChunks, device);
            CopyManager = new CopiedChunkManager(CubeView, ChunkIO, SizeInChunks);
        }

        public bool IsInWorldBounds(ChunkPosition position)
        {
            return position.X >= 0 && position.X < SizeInChunks && position.Y >= 0 && position.Y < SizeInChunks && position.Z >= 0 && position.Z < SizeInChunks;
        }
    }
}
