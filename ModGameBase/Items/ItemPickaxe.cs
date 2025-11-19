using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemPickaxe : ItemMetaItem<ItemPickaxeHead>, IHasAreaEffect
	{
		public ItemPickaxe() : base("pickaxe", new RectangleF(16, 144, 16, 16))
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

        public override string GetDescription(ItemInstance item)
        {
			var meta = Get(item);

			if (meta != null)
			{
				return meta.GetStats(item).GetTooltip();
			}
			else return base.GetDescription(item);
		}

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			base.LeftClick(player, inventory, index, facing, out actionStats);

			var metaItem = Get(inventory.Get(index));

			if (metaItem != null && player.IsLooking)
			{
				if (player.ExpandedMineState)
				{
					CubePosition[] affectedPositions = metaItem.GetAffectedPositions(player, inventory.Get(index), player.Position, player.LookAtPos.InWorldSpace(), player.LookAtNormal, out _);

                    float useTime = GetStats(inventory.Get(index)).cooldownTime;
                    useTime -= useTime * (player.GetStats().MiningScale);

                    actionStats.useTime = useTime;
                    actionStats.useAnimTime = useTime;

                    Span<ushort> ids = stackalloc ushort[affectedPositions.Length];
					player.world.ChunkManager.CubeView.GetIds(affectedPositions.AsSpan(), ids);

					for (int i = 0; i < affectedPositions.Length; i++)
					{
						if (player.GetWorld().ChunkManager.IsInWorldBounds(affectedPositions[i]))
						{
							if (Main.Registry.CubeRegistry.GetOrDefault(ids[i], Main.Registry.CubeRegistry.Air).Touchable)
								player.GetWorld().TryMineCube(player, affectedPositions[i], metaItem.GetStats(inventory.Get(index)).mineLevel, metaItem.GetStats(inventory.Get(index)).mineRate);
						}
					}
				}
                else
                {
					if (player.world.ChunkManager.CubeView.GetCube(player.LookAtPos).GetOrDefault(Main.Registry.CubeRegistry.Air).Touchable)
						player.GetWorld().TryMineCube(player, player.LookAtPos, metaItem.GetStats(inventory.Get(index)).mineLevel, metaItem.GetStats(inventory.Get(index)).mineRate);
				}
			}

			return true;
		}

		public static ItemInstance CreatePickaxe(ItemInstance itemHead)
		{
			return new ItemInstance(Main.Registry.GetCurrentMod().Registry.ItemRegistry.Get("pickaxe"), 1, itemHead.item.Id);
		}

        public ref readonly ItemPickaxeHead.PickaxeStats GetStats(ItemInstance item)
        {
			return ref Get(item).GetStats(item);
        }

        public CubePosition[] GetAffectedPositions(Player player, ItemInstance item, Vector3 standingPosition, Vector3 hit, Vector3 normal, out int num)
        {
			return Get(item).GetAffectedPositions(player, item, standingPosition, hit, normal, out num);
        }
    }
}
