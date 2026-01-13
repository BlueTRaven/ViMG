using BrUtility;
using Engine.Entities;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemSwordBase : Item
	{
		public ItemSwordBase() : base("sword_base")
        {
            Client = new ClientItem(this, new RectangleF(0, 0, 16, 16));
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			base.LeftClick(player, inventory, index, facing, out actionStats);

            actionStats = new ActionStats(0.35f);
			int damage = 1;
			float knockback = 1f;
			player.PerformAttack(DamageType.Melee, ref actionStats, ref damage, ref knockback);

			player.SpawnHitboxLater(index, 1, DamageType.Melee, -(player as IRotatable).Forward, 1f);

            actionStats.animationType = UseAnimationType.SwingHorizontal;

            return true;
		}
	}
}
