using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Items
{
	public class ItemSwordBase : Item
	{
		protected static SimpleMesh<VertexPositionColorTextureNormal, int> mesh;

		public ItemSwordBase() : base()
		{
			Texture = Main.assetsManager.GetAsset<Texture2D>("swrod");
			SourceRect = new BrUtility.RectangleF(0, 0, 16, 16);
		}

		public override bool LeftClick(Player player, Vector3 facing)
		{
			player.SpawnHitbox();

			return true;
		}

		public override void Draw(GraphicsDevice device, Player player, Vector3 facing)
		{
			base.Draw(device, player, facing);

			if (mesh == null)
			{
				Vector3 min = -new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE, 0);
				Vector3 max = new Vector3(Cube.CUBE_SCALE / 2f, 0, 0);

				Vector3 a = new Vector3(max.X, min.Y, max.Z);
				Vector3 b = new Vector3(min.X, min.Y, max.Z);
				Vector3 c = new Vector3(min.X, max.Y, max.Z);
				Vector3 d = new Vector3(max.X, max.Y, max.Z);

				List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
				List<int> indices = new List<int>();

				Vector2 atx = new Vector2(0, 1);
				Vector2 btx = new Vector2(1, 1);
				Vector2 ctx = new Vector2(1, 0);
				Vector2 dtx = new Vector2(0, 0);

				int offset = vertices.Count;
				indices.Add(offset + 0);
				indices.Add(offset + 1);
				indices.Add(offset + 3);
				indices.Add(offset + 1);
				indices.Add(offset + 2);
				indices.Add(offset + 3);

				vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, 1)));
				vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, 1)));
				vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, 1)));
				vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, 1)));

				mesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices);
			}

			mesh.Draw(device, Main.CubeEffect, player.GetHeldMatrix(), Texture, SourceRect);
		}
	}
}
