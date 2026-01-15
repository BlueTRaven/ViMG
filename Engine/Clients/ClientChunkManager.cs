using Engine.ChunkStuff;
using Engine.Clients.Entities;
using Engine.Common;
using Engine.Common.Entities;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
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
        public CubeTrackers CubeTrackers;
        public CubeBreakProgressTracker CubeProgressTracker;

        public int SizeInChunks = 32;

        public ClientChunkManager(GraphicsDevice device)
        {
            this.device = device;
            
            CubeTrackers = new CubeTrackers();
            ChunkIO = new ChunkManagerIO(SizeInChunks, "", 0);
            CubeView = new ClientCubeView(ChunkIO, SizeInChunks);
            ChunkMesher = ChunkMesher.RenderOnly(SizeInChunks, device);
            CopyManager = new CopiedChunkManager(CubeView, ChunkIO, CubeTrackers, SizeInChunks);

            CubeProgressTracker = new();
        }

        // TODO:
        // This is kinda hacky.
        // If we're playing in singleplayer, we want to avoid having duplicate copies of voxel data if at all possible.
        // Client-server naturally duplicates this info. However, if we're singleplayer, we're running in one process and can
        // inter-communicate no problem. In this case we can just use the server's ChunkIO instead of the Client's separate,
        // duplicated version.
        // This is hacky because it's lazily evaluated. I think we could probably change this to not be lazily evaluated, but
        // that would mean client can only be fully initialized on full connection established (As that's when the server's
        // ChunkIO is definitely available).
        public void MaybeSetToServer()
        {
            if (Main.gameStateManager.TheIsland.GetWorld() != null && ChunkIO != Main.gameStateManager.TheIsland.GetWorld().ChunkIO)
            {
                CubeTrackers = new CubeTrackers(); // Might not be necessary

                ChunkIO = Main.gameStateManager.TheIsland.GetWorld().ChunkIO;
                CubeView = new ClientCubeView(ChunkIO, SizeInChunks);
                CopyManager = new CopiedChunkManager(CubeView, ChunkIO, CubeTrackers, SizeInChunks);
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
