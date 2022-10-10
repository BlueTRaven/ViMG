using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemPickaxe : ItemMetaItem<ItemPickaxeHead>
	{
		public ItemPickaxe() : base("pickaxe", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(16, 144, 16, 16))
		{
		}

		public override string GetName(ItemInstance item)
		{
			var meta = Get(item);

			if (meta != null)
			{
				return meta.GetMaterial() + " Pickaxe";
			}
			else return base.GetName(item);
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

			var lookAtResult = player.GetWorld().Raycast(player.Position, player.Position + facing * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.world.GetChunkManager().IsInWorldBounds(pos) && 
					player.world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Touchable;
			});

			var metaItem = Get(inventory.Get(index));

			if (metaItem != null && lookAtResult.hasHit)
			{
				itemCooldownTime = metaItem.GetStats().cooldownTime;
				itemCooldownTime -= itemCooldownTime * (player.GetStats().MiningScale);

				var lookAtPos = CubePosition.FromWorldSpace(lookAtResult.hit);

				Vector3 normal = lookAtResult.normal;

				int minx = 0;
				int maxx = 0;
				int miny = 0;
				int maxy = 0;
				int minz = 0;
				int maxz = 0;

				float dotx = Vector3.Dot(player.Position - lookAtResult.hit, new Vector3(1, 0, 0));
				float doty = Vector3.Dot(player.Position - lookAtResult.hit, new Vector3(0, 1, 0));
				float dotz = Vector3.Dot(player.Position - lookAtResult.hit, new Vector3(0, 0, 1));

				if (normal.Y != 0)
				{
					if (dotx > dotz)
					{
						minx = -metaItem.GetStats().width;
						maxx = metaItem.GetStats().width;

						minz = -metaItem.GetStats().height;
						maxz = metaItem.GetStats().height;

						if (normal.Y > 0)
						{
							miny = -metaItem.GetStats().depth;
							maxy = 0;
						}
						else
						{
							miny = 0;
							maxy = metaItem.GetStats().depth;
						}
					}
					else
					{
						minx = -metaItem.GetStats().height;
						maxx = metaItem.GetStats().height;

						minz = -metaItem.GetStats().width;
						maxz = metaItem.GetStats().width;

						if (normal.Y > 0)
						{
							miny = -metaItem.GetStats().depth;
							maxy = 0;
						}
						else
						{
							miny = 0;
							maxy = metaItem.GetStats().depth;
						}
					}
				}
				else if (normal.X != 0)
				{
					minz = -metaItem.GetStats().width;
					maxz = metaItem.GetStats().width;

					miny = -metaItem.GetStats().height;
					maxy = metaItem.GetStats().height;

					if (normal.X > 0)
					{
						minx = -metaItem.GetStats().depth;
						maxx = 0;
					}
					else
					{
						minx = 0;
						maxx = metaItem.GetStats().depth;
					}
				}
				else if (normal.Z != 0)
				{
					minx = -metaItem.GetStats().width;
					maxx = metaItem.GetStats().width;

					miny = -metaItem.GetStats().height;
					maxy = metaItem.GetStats().height;

					if (normal.Z > 0)
					{
						minz = -metaItem.GetStats().depth;
						maxz = 0;
					}
					else
					{
						minz = 0;
						maxz = metaItem.GetStats().depth;
					}
				}

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

							if (player.GetWorld().GetChunkManager().IsInWorldBounds(minePos))
							{
								if (player.world.GetChunkManager().GetCube(minePos).GetOrDefault(Main.Registry.CubeRegistry.Air).Touchable)
									player.GetWorld().MineCube(minePos, metaItem.GetStats().mineRate);
							}
						}
					}
				}

				//player.GetWorld().MineCube(lookAtPos);
			}

			return true;
		}

		public static ItemInstance CreatePickaxe(ItemInstance itemHead)
		{
			return new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe"), 1, itemHead.item.Id);
		}
	}
}
