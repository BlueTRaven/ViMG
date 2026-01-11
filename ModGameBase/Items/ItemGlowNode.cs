using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Rendering;

namespace ViMG.Items
{
	public class ItemGlowNode : Item
	{
		public ItemGlowNode() : base("glow_node", new RectangleF(0, 0, 16, 16))
		{
			name = "Glow Node";
			description = "A chunk of wood coated in glowdust. It shimmers brightly, no matter the time of day.";
		}

        public override RendererDeferred.DrawMaterial GetMaterial()
        {
			Material ??= new Rendering.RendererDeferred.DrawMaterial("glow_node");
            return Material.Value;
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			base.RightClick(player, inventory, index, facing, out actionStats);

			if (player.IsLooking && player.CanPlace)
			{
                Cube glowNode = Main.Registry.CubeRegistry.Get("glow_node");

                if (player.world.PlaceCube(player, player.PlaceAtPos, glowNode.Id))
                {
                    inventory.Remove(index, 1);
					return true;
                }
            }

			return false;
		}
	}
}
