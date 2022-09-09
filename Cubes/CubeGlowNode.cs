using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeGlowNode : Cube
	{
		public CubeGlowNode() : base("glow_node", RectangleF.Empty, Color.White, 1)
		{
			Transparency = TransparencyValue.Invisible;
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			player.GetWorld().EntityManager.Add(new GlowNode(position, CUBE_SCALE * 5, CUBE_SCALE * 4, Color.White));
				//new Color(Main.random.NextFloat(), Main.random.NextFloat(), Main.random.NextFloat(), 1)));
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("glow_node"), 1, 1));
		}
	}
}
