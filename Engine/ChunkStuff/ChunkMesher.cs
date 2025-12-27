using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.Physics;

namespace Engine.ChunkStuff
{
    // TODO: This should be valid to be created in headless mode
    // This is because we need to be able to create collision meshes even if running headless
    // This is an issue right now because the CollisionMesher depends on the RenderMesher (kinda gross)
    // so these two just need to be separated better.
    public class ChunkMesher : IDisposable
    {
        private readonly struct CubeUpdated
        {
            public readonly double timeUpdated;
            public readonly CubePosition updated;
            public readonly CubePosition notified;
            public readonly ushort oldId;
            public readonly ushort newId;

            public CubeUpdated(double timeUpdated, CubePosition updated, CubePosition notified, ushort oldId, ushort newId)
            {
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

        public readonly ChunkRenderMesher RenderMesher;
        public readonly ChunkCollisionMesher CollisionMesher;
        public BufferPool bufferPool;

        public ChunkMesher(int sizeInChunksXZ, PhysicsInfo physicsInfo, GraphicsDevice device)
        {
            this.bufferPool = new BufferPool();
            RenderMesher = new ChunkRenderMesher(device, sizeInChunksXZ, bufferPool);
            CollisionMesher = new ChunkCollisionMesher(physicsInfo, RenderMesher, sizeInChunksXZ, bufferPool);
        }

        public void Unload(ChunkPosition pos)
        {
            RenderMesher.Unload(pos);
            CollisionMesher.Unload(pos);
        }

        public void MarkChunkDirty(ChunkPosition position)
        {
            var a = RenderMesher.MarkDirty(position);
            var b = CollisionMesher.MarkDirty(position);

            //Debug.Assert(a == b);
        }

        public void Dispose()
        {
            //There may still be things in the queue, including active threads, so wait on those
            //TODO: maybe this isn't necessary? Mesh Resources aren't created anywhere but the main thread
            RenderMesher.FinishFlush();
            CollisionMesher.FinishFlush();

            RenderMesher.UnloadAll();
            CollisionMesher.UnloadAll();
        }

        public void Update(CubeView cubeView, EntityManager entityManager, CopiedChunkManager copyManager)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();
            RenderMesher.Update(cubeView, entityManager, copyManager);
            CollisionMesher.Update(cubeView, entityManager, copyManager);
        }
    }
}
