using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;

namespace ViMG.UIs
{
	public class MenuChest : Menu
	{
		private readonly Player player;
        private readonly Entity owner;
        private readonly InventoryManager.InventoryReference playerInventory;
		private readonly InventoryManager.InventoryReference heldInventory;
		private readonly InventoryManager.InventoryReference chestInventory;
		private readonly int rows;
		private readonly int columns;

		public MenuChest(GameStateManager gsManager, Player player, Entity owner, InventoryManager.InventoryReference playerInventory, InventoryManager.InventoryReference heldInventory, InventoryManager.InventoryReference chestInventory, int rows, int columns) : base(gsManager)
		{
			this.player = player;
            this.owner = owner;
            this.playerInventory = playerInventory;
            this.heldInventory = heldInventory;
            this.chestInventory = chestInventory;
			this.rows = rows;
			this.columns = columns;
		}

		public override void OnOpen()
		{
			base.OnOpen();

			Main.DrawCursor = true;
			Main.MouseControl = true;
		}

		public override void OnClose()
		{
			base.OnClose();

			Main.DrawCursor = false;
			Main.MouseControl = false;
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

            var playerInventory = player.world.InventoryManager.Get(this.playerInventory);
            var heldInventory = player.world.InventoryManager.Get(this.heldInventory);
            var chestInventory = player.world.InventoryManager.Get(this.chestInventory);

			UI.Start();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

			MenuHelper.DoPlayerInventory(player, playerInventory, heldInventory, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, SIZE, PADDING);

			UI.EndParent();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32 + Player.INVENTORY_ROWS * SIZE + MARGIN));

			MenuHelper.DoEntityInventory(player, owner, chestInventory, heldInventory, rows, columns, SIZE, PADDING);

			UI.EndParent();
		}

		public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);

			//MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
		}
	}
}
