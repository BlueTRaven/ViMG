using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.UIs
{
	public class UIInventoryChest : UIInventory
	{
		private readonly Player player;
		private readonly Inventory playerInventory;
		private readonly Inventory chestInventory;
		private readonly int rows;
		private readonly int columns;

		private ItemInstance held;

		public UIInventoryChest(Player player, Inventory playerInventory, Inventory chestInventory, int rows, int columns)
		{
			this.player = player;
			this.playerInventory = playerInventory;
			this.chestInventory = chestInventory;
			this.rows = rows;
			this.columns = columns;
		}

		public override void Update()
		{
			base.Update();

			UI.Start();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

			UIInventoryHelper.DoPlayerInventory(player, playerInventory, ref held, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, SIZE, PADDING);

			UI.EndParent();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32 + Player.INVENTORY_ROWS * SIZE + MARGIN));

			UIInventoryHelper.DoPlayerInventory(player, chestInventory, ref held, rows, columns, SIZE, PADDING);

			UI.EndParent();
		}

		public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);

			UIInventoryHelper.DrawHeldItem(batch, held, SIZE, SCALE);
		}
	}
}
