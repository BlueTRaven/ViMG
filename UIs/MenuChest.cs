using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.UIs
{
	public class MenuChest : Menu
	{
		private readonly Player player;
		private readonly Inventory playerInventory;
		private readonly Inventory chestInventory;
		private readonly int rows;
		private readonly int columns;

		private ItemInstance held;

		public MenuChest(Player player, Inventory playerInventory, Inventory chestInventory, int rows, int columns)
		{
			this.player = player;
			this.playerInventory = playerInventory;
			this.chestInventory = chestInventory;
			this.rows = rows;
			this.columns = columns;
		}

		public override void Update(GraphicsDevice device, double deltaTime)
		{
			base.Update(device, deltaTime);

			UI.Start();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

			MenuHelper.DoPlayerInventory(player, playerInventory, ref held, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, SIZE, PADDING);

			UI.EndParent();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32 + Player.INVENTORY_ROWS * SIZE + MARGIN));

			MenuHelper.DoPlayerInventory(player, chestInventory, ref held, rows, columns, SIZE, PADDING);

			UI.EndParent();
		}

		public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);

			MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
		}
	}
}
