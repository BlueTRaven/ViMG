using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Items
{
    public class ItemBookOfEmber : Item
    {
		private static MagicAttackStats magicStats = new MagicAttackStats(new AttackStats(Player.DamageType.Magic, 0.25f, 1, 0f), 1);
        public ItemBookOfEmber() : base("book_spell_ember", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(64, 32, 16, 16))
        {
			name = "Spellbook: Ember";
			description = "A spellbook with an explanation of how to cast \"Ember\".\n" +
				magicStats.GetTooltip() +
				"This spell will light a small fire on any surface in front of you.";

			flipXInHand = true;
        }

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
		{
			base.LeftClick(player, inventory, index, facing, out actionStats);

			if (!magicStats.CanUse(player))
			{
				actionStats = new Player.ActionStats();
				return false;
			}

			CubePosition placePos = player.IsLooking && player.CanPlace ? player.PlaceAtPos : player.LookAtEnd;

			Cube cube = Main.Registry.CubeRegistry.Get("flame");

			if (cube.CanPlace(player.world, player.world.ChunkManager, placePos) && Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton))
            {
                actionStats = new Player.ActionStats(magicStats.attackStats);
				int damage = magicStats.attackStats.damage;
				float knockback = magicStats.attackStats.knockback;
				player.PerformAttack(Player.DamageType.Magic, ref actionStats, ref damage, ref knockback);

				magicStats.Use(player);

				player.GetWorld().ChunkManager.ThreadedView.SetCube(placePos, cube.Id);

				cube.OnPlayerPlaced(player, placePos);

				return true;
			}

			return false;
		}
	}
}
