using BepuPhysics;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace DemoRenderer.ShapeDrawing
{
    /// <summary>
    /// Stores references to triangle data between usages to avoid the need to regather it every frame.
    /// </summary>
    public class MeshCache : IDisposable
    {
        Buffer<Vector3> vertices;
        public StructuredBuffer TriangleBuffer;
        QuickSet<ulong, PrimitiveComparer<ulong>> previouslyAllocatedIds;
        QuickList<ulong> requestedIds;

        struct UploadRequest
        {
            public int Start;
            public int Count;
        }
        QuickList<UploadRequest> pendingUploads;

        public BufferPool Pool { get; private set; }
        Allocator allocator;
        public MeshCache(GraphicsDevice device, BufferPool pool, int initialSizeInVertices = 1 << 22)
        {
            Pool = pool;
            pool.TakeAtLeast(initialSizeInVertices, out vertices);
            TriangleBuffer = new StructuredBuffer(device, typeof(Vector3), initialSizeInVertices, BufferUsage.WriteOnly, ShaderAccess.Read);
            allocator = new Allocator(initialSizeInVertices, pool);

            pendingUploads = new QuickList<UploadRequest>(128, pool);
            requestedIds = new QuickList<ulong>(128, pool);
            previouslyAllocatedIds = new QuickSet<ulong, PrimitiveComparer<ulong>>(256, pool);
        }

        public unsafe bool TryGetExistingMesh(ulong id, out int start, out Buffer<Vector3> vertices)
        {
            if (allocator.TryGetAllocationRegion(id, out var allocation))
            {
                start = (int)allocation.Start;
                vertices = this.vertices.Slice(start, (int)(allocation.End - start));
                return true;
            }
            start = default;
            vertices = default;
            return false;
        }

        public unsafe bool Allocate(ulong id, int vertexCount, out int start, out Buffer<Vector3> vertices)
        {
            requestedIds.Add(id, Pool);
            if (TryGetExistingMesh(id, out start, out vertices))
            {
                return false;
            }
            if (allocator.Allocate(id, vertexCount, out var longStart))
            {
                start = (int)longStart;
                vertices = this.vertices.Slice(start, vertexCount);
                pendingUploads.Add(new UploadRequest { Start = start, Count = vertexCount }, Pool);
                return true;
            }
            //Didn't fit. We need to resize.
            var copyCount = TriangleBuffer.ElementCount + vertexCount;
            var newSize = (int)BitOperations.RoundUpToPowerOf2((uint)copyCount);
            Pool.ResizeToAtLeast(ref this.vertices, newSize, 0);
            allocator.Capacity = newSize;
            allocator.Allocate(id, vertexCount, out longStart);
            start = (int)longStart;
            vertices = this.vertices.Slice(start, vertexCount);
            //A resize forces an upload of everything, so any previous pending uploads are unnecessary.
            pendingUploads.Count = 0;
            pendingUploads.Add(new UploadRequest { Start = 0, Count = copyCount }, Pool);
            return true;
        }


        public unsafe void FlushPendingUploads(GraphicsDevice device)
        {
            if (allocator.Capacity > TriangleBuffer.ElementCount)
            {
                TriangleBuffer = new StructuredBuffer(device, typeof(Vector3), (int)allocator.Capacity, BufferUsage.WriteOnly, ShaderAccess.Read);
            }
            for (int i = 0; i < pendingUploads.Count; ++i)
            {
                ref var upload = ref pendingUploads[i];
                TriangleBuffer.SetData(new Span<Vector3>(vertices.Memory, vertices.Length).ToArray(), upload.Start, upload.Count);
            }
            pendingUploads.Count = 0;

            //Get rid of any stale allocations.
            for (int i = 0; i < requestedIds.Count; ++i)
            {
                previouslyAllocatedIds.FastRemove(requestedIds[i]);
            }
            for (int i = 0; i < previouslyAllocatedIds.Count; ++i)
            {
                allocator.Deallocate(previouslyAllocatedIds[i]);
            }
            previouslyAllocatedIds.FastClear();
            for (int i = 0; i < requestedIds.Count; ++i)
            {
                previouslyAllocatedIds.Add(requestedIds[i], Pool);
            }
            requestedIds.Count = 0;

            //This executes at the end of the frame. The next frame will read the compacted location, which will be valid because the pending upload will be handled.
            if (allocator.IncrementalCompact(out var compactedId, out var compactedSize, out var oldStart, out var newStart))
            {
                vertices.CopyTo((int)oldStart, vertices, (int)newStart, (int)compactedSize);
                pendingUploads.Add(new UploadRequest { Start = (int)newStart, Count = (int)compactedSize }, Pool);
            }

        }

        bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                TriangleBuffer.Dispose();
                pendingUploads.Dispose(Pool);
                Pool.Return(ref vertices);
                requestedIds.Dispose(Pool);
                previouslyAllocatedIds.Dispose(Pool);
                allocator.Dispose();
                disposed = true;
            }
        }
    }
}