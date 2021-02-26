using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class ChunkMesh : SimpleMesh<VertexPositionColorTextureNormal, int>
	{
		public new static ChunkMesh Empty { get; private set; }

		static ChunkMesh()
		{
			Empty = new ChunkMesh();
		}

		private ChunkMesh() : base()
		{

		}

		public ChunkMesh(GraphicsDevice device, List<VertexPositionColorTextureNormal> vertices, List<int> indices) : base(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"))
		{
		}
	}
}
