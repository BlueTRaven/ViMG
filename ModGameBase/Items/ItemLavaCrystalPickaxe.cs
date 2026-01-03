using BrUtility;
using Engine.ChunkStuff;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemLavaCrystalPickaxe : Item, IHasAreaEffect
	{
		private ItemPickaxeHead.PickaxeStats stats = new ItemPickaxeHead.PickaxeStats(0.55f, 1, 2, 1, 1, 0);

		public ItemLavaCrystalPickaxe() : base("pickaxe_lavacrystal", new RectangleF(32, 144, 16, 16))
		{
			name = "Lavacrystal Pickaxe";
			description = "A pickaxe made of enchanted bones and lava crystal.\n" +
				stats.GetTooltip();

			flipXInHand = true;
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			base.LeftClick(player, inventory, index, facing, out actionStats);

			var lookAtResult = player.GetWorld().Raycast(player.Position, player.Position + facing * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.world.ChunkManager.IsInWorldBounds(pos) &&
					player.world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air).Touchable;
			});

			if (lookAtResult.hasHit)
			{
				if (player.ExpandedMineState)
				{
					CubePosition[] affectedPositions = GetAffectedPositions(player.world.ChunkManager.CubeView, inventory.Get(index), player.Position, lookAtResult.hit, lookAtResult.normal, out _);
					Span<ushort> ids = stackalloc ushort[affectedPositions.Length];

					player.world.ChunkManager.CubeView.GetIds(affectedPositions.AsSpan(), ids);

					float useTime =  GetStats(inventory.Get(index)).cooldownTime;
					useTime -= useTime * (player.GetStats().MiningScale);

					actionStats.useTime = useTime;
					actionStats.useAnimTime = useTime;

					for (int i = 0; i < affectedPositions.Length; i++)
					{
						if (Main.Registry.CubeRegistry.GetOrDefault(ids[i], Main.Registry.CubeRegistry.Air).Touchable)
							player.GetWorld().TryMineCube(player, affectedPositions[i], GetStats(inventory.Get(index)).mineLevel, GetStats(inventory.Get(index)).mineRate);
					}
				}
				else
				{
					if (player.GetWorld().ChunkManager.IsInWorldBounds(lookAtResult.hit))
					{
						if (player.world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(lookAtResult.hit)).GetOrDefault(Main.Registry.CubeRegistry.Air).Touchable)
							player.GetWorld().TryMineCube(player, CubePosition.FromWorldSpace(lookAtResult.hit), GetStats(inventory.Get(index)).mineLevel, GetStats(inventory.Get(index)).mineRate);
					}
				}
			}

			return true;
		}

		private CubePosition[] cachedAffectedPositions;
		public CubePosition[] GetAffectedPositions(ICubeGetter cubeView, ItemInstance item, Vector3 standingPosition, Vector3 hit, Vector3 normal, out int num)
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

			if (cachedAffectedPositions == null || cachedAffectedPositions.Length < rangeX * rangeY * rangeZ)
				cachedAffectedPositions = new CubePosition[rangeX * rangeY * rangeZ];

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

						cachedAffectedPositions[i] = minePos;
						i++;
					}
				}
			}

			num = i;
			return cachedAffectedPositions[..i];
		}

		public ref readonly ItemPickaxeHead.PickaxeStats GetStats(ItemInstance item)
		{
			return ref stats;
		}
	}
}
