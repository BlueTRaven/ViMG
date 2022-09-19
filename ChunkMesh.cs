using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class ChunkMesh : SimpleMesh<VertexCube, int>
	{
		public new static ChunkMesh Empty { get; private set; }

		static ChunkMesh()
		{
			Empty = new ChunkMesh();
		}

		private ChunkMesh() : base()
		{

		}

		public ChunkMesh(GraphicsDevice device, List<VertexCube> vertices, List<int> indices) : base(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"))
		{
		}

		protected override void UploadLater(List<VertexCube> vertices, List<int> indices)
		{
			base.UploadLater(vertices, indices);

			Main.DelayedUploaderChunkMesh.meshesToUploadLater.Enqueue(new DelayedUploader<VertexCube, int>.ToUploadLater(this, vertices, indices));
		}
	}
}
