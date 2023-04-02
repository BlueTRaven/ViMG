using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ViMG.VertexDeclarations;

namespace ViMG
{
    public class ChunkMesh
	{
		public VertexBuffer VBO;
		public IndexBuffer IBO;

		public static ChunkMesh Empty { get; private set; }

		public bool IsEmpty => this == Empty;

		static ChunkMesh()
		{
			Empty = new ChunkMesh();
		}

		private ChunkMesh()
        {

        }

		public ChunkMesh(GraphicsDevice device, List<VertexCube> vertices, List<int> indices)
		{
			if (Thread.CurrentThread == Main.MainThread || Main.MULTITHREAD_UPLOADMESH)
            {
				(VBO, IBO) = MeshHelper.MakeSimplerMesh(device, vertices, indices);
            }
		}
	}
}
