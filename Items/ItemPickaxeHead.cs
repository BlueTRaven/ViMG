using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemPickaxeHead : Item, IHasAreaEffect
	{
		public readonly struct PickaxeStats 
		{
			public readonly float cooldownTime;
			public readonly int mineLevel;
            public readonly int mineRate;
            public readonly int height;
			public readonly int width;
			public readonly int depth;

			public PickaxeStats(float cooldownTime, int mineLevel, int mineRate, int height, int width, int depth)
			{
				this.cooldownTime = cooldownTime;
				this.mineLevel = mineLevel;
                this.mineRate = mineRate;
                this.height = height;
				this.width = width;
				this.depth = depth;
			}

			public string GetTooltip()
            {
				return String.Format("{0} Mining Level\n" +
					"Mining Rate: {1}\n" +
					"{2} Speed\n" +
					"Size: {3}x{4}x{5} Width by Height by Depth\n", ((Util.MineTier)mineLevel).ToString(), mineRate, Util.CooldownToString(cooldownTime), width + 1, height + 1, depth + 1);
            }
		}

		private Color color;
		private PickaxeStats stats;
		private string materialName;

		public ItemPickaxeHead(string material, Color color, PickaxeStats stats) : base("pickaxe_head_" + material,
            StaticMaterials.Items, new RectangleF(0, 144, 16, 16))
		{
			this.materialName = char.ToUpper(material[0]) + material.Substring(1);

			this.color = color;
			this.stats = stats;
		}

		public string GetMaterial()
		{
			return materialName;
		}

		public override string GetName(ItemInstance item)
		{
			return materialName + " Pickaxe Head";
		}

		public ref readonly PickaxeStats GetStats(ItemInstance item)
		{
			return ref stats;
		}

		private CubePosition[] cachedAffectedPositions;
		public CubePosition[] GetAffectedPositions(Player player, ItemInstance item, Vector3 standingPosition, Vector3 hit, Vector3 normal, out int num)
		{
			var lookAtPos = CubePosition.FromWorldSpace(hit);

			int minx = 0;
			int maxx = 0;
			int miny = 0;
			int maxy = 0;
			int minz = 0;
			int maxz = 0;

			float dotx = Vector3.Dot(standingPosition - hit, new Vector3(1, 0, 0));
			float doty = Vector3.Dot(standingPosition - hit, new Vector3(0, 1, 0));
			float dotz = Vector3.Dot(standingPosition - hit, new Vector3(0, 0, 1));

			if (normal.Y != 0)
			{
				if (dotx > dotz)
				{
					minx = -stats.width;
					maxx = stats.width;

					minz = -stats.height;
					maxz = stats.height;

					if (normal.Y > 0)
					{
						miny = -stats.depth;
						maxy = 0;
					}
					else
					{
						miny = 0;
						maxy = stats.depth;
					}
				}
				else
				{
					minx = -stats.height;
					maxx = stats.height;

					minz = -stats.width;
					maxz = stats.width;

					if (normal.Y > 0)
					{
						miny = -stats.depth;
						maxy = 0;
					}
					else
					{
						miny = 0;
						maxy = stats.depth;
					}
				}
			}
			else if (normal.X != 0)
			{
				minz = -stats.width;
				maxz = stats.width;

				miny = -stats.height;
				maxy = stats.height;

				if (normal.X > 0)
				{
					minx = -stats.depth;
					maxx = 0;
				}
				else
				{
					minx = 0;
					maxx = stats.depth;
				}
			}
			else if (normal.Z != 0)
			{
				minx = -stats.width;
				maxx = stats.width;

				miny = -stats.height;
				maxy = stats.height;

				if (normal.Z > 0)
				{
					minz = -stats.depth;
					maxz = 0;
				}
				else
				{
					minz = 0;
					maxz = stats.depth;
				}
			}

			int rangeX = maxx - minx + 1;
			int rangeY = maxy - miny + 1;
			int rangeZ = maxz - minz + 1;

			if (cachedAffectedPositions == null)
				cachedAffectedPositions = new CubePosition[rangeX * rangeY * rangeZ];

			if (rangeX * rangeY * rangeZ > cachedAffectedPositions.Length)
			{
				Console.WriteLine("What?");
			}

			int i = 0;
			for (int x = minx; x <= maxx; x++)
			{
				for (int y = miny; y <= maxy; y++)
				{
					for (int z = minz; z <= maxz; z++)
					{
						CubePosition minePos = lookAtPos;
						minePos.X += x;
						minePos.Y += y;
						minePos.Z += z;

						cachedAffectedPositions[i++] = minePos;
					}
				}
			}

			num = cachedAffectedPositions.Length;
			return cachedAffectedPositions;
		}

		public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			if (meshItemQuadInWorld.IBO == null)
				MakeMesh(device);

			Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(Material,
                meshItemQuadInWorld, transform, SourceRect, color.ToVector3()));
		}

		public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
		{
			//base.DrawInInventory(batch, position, scale);

			batch.Draw(Material.Diffuse, position, SourceRect.ToRectangle(), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
		}
	}
}
