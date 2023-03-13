using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeChest : Cube
	{
		private string tier;
		private int rows, columns;
		private int slots;

		public CubeChest(string tier, int rows, int columns) : base("chest_" + tier, 
			new CubeFacingLayout(new RectangleF(144, 16, 16, 16), new RectangleF(160, 16, 16, 16), new RectangleF(160, 16, 16, 16)), Color.White, 8)
		{
			this.tier = tier;
			this.rows = rows;
			this.columns = columns;
			this.slots = rows * columns;
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			Vector3 dir = player.Position - (position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2));
			Vector2 dirXZ = new Vector2(dir.X, dir.Z);

			MeshHelper.CubeFace face;

			if (dirXZ.Length() > MathF.Abs(dir.Y))
			{
				if (MathF.Abs(dirXZ.X) > MathF.Abs(dirXZ.Y))
				{
					//facing left or right
					if (dirXZ.X > 0)
						face = MeshHelper.CubeFace.RIGHT;
					else face = MeshHelper.CubeFace.LEFT;
				}
				else
				{
					if (dirXZ.Y > 0)
						face = MeshHelper.CubeFace.BACK;
					else face = MeshHelper.CubeFace.FRONT;
				}
			}
			else
			{
				if (dir.Y > 0)
					face = MeshHelper.CubeFace.UP;
				else face = MeshHelper.CubeFace.DOWN;
			}

			player.GetWorld().EntityManager.Add(new EntityChest(position, rows, columns, face));
		}

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
			if (data.valid)
			{
				var meshingData = data.GetEntityMeshingData(parameters.position);

				if (meshingData != null)
				{
					EntityChest.MeshingData castedMeshingData = (EntityChest.MeshingData)meshingData;

					if (face == castedMeshingData.facing)
						return new RectangleF(128, 16, 16, 16);
				}
			}

			return base.GetSourceRect(pass, data, parameters, face);
        }

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			DropSelf(itemsToDrop);
		}
	}
}
