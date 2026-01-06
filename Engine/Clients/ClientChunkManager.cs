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
        private GraphicsDevice device;
        public ClientCubeView CubeView;
        public ChunkMesher ChunkMesher;
        public CopiedChunkManager CopyManager;
        public ChunkManagerIO ChunkIO;

        public int SizeInChunks = 32;

        public ClientChunkManager(GraphicsDevice device)
        {
            this.device = device;

            ChunkIO = new ChunkManagerIO(SizeInChunks, "", 0);
            CubeView = new ClientCubeView(ChunkIO, SizeInChunks);
            ChunkMesher = ChunkMesher.RenderOnly(SizeInChunks, device);
            CopyManager = new CopiedChunkManager(CubeView, ChunkIO, SizeInChunks);
        }

        public void MaybeSetToServer()
        {
            if (Main.gameStateManager.TheIsland.GetWorld() != null && ChunkIO != Main.gameStateManager.TheIsland.GetWorld().ChunkIO)
            {
                ChunkIO = Main.gameStateManager.TheIsland.GetWorld().ChunkIO;
                CubeView = new ClientCubeView(ChunkIO, SizeInChunks);
                CopyManager = new CopiedChunkManager(CubeView, ChunkIO, SizeInChunks);
                ChunkMesher.RenderMesher.FinishFlush();
                ChunkMesher.RenderMesher.UnloadAll();
                ChunkMesher = ChunkMesher.RenderOnly(SizeInChunks, device);
            }
        }

        public bool IsInWorldBounds(ChunkPosition position)
        {
            return position.X >= 0 && position.X < SizeInChunks && position.Y >= 0 && position.Y < SizeInChunks && position.Z >= 0 && position.Z < SizeInChunks;
        }
    }
}
