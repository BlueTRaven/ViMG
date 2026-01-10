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
		private readonly EntityManager.EntityReference player;
        private readonly EntityManager.EntityReference owner;
        private readonly InventoryManager.InventoryReference playerInventory;
		private readonly InventoryManager.InventoryReference heldInventory;
		private readonly InventoryManager.InventoryReference chestInventory;
		private readonly int rows;
		private readonly int columns;

		public MenuChest(GameStateManager gsManager, EntityManager.EntityReference player, EntityManager.EntityReference owner, InventoryManager.InventoryReference playerInventory, InventoryManager.InventoryReference heldInventory, InventoryManager.InventoryReference chestInventory, int rows, int columns) : base(gsManager)
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

            var invManager = gsManager.TheIsland.GetClient().inventoryManager;
            var playerInventory = invManager.Get(this.playerInventory);
            var heldInventory = invManager.Get(this.heldInventory);
            var chestInventory = invManager.Get(this.chestInventory);

			if (Main.gameStateManager.GetCurrentGameState().GetCurrentMenu() == this && Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.E) || Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
				Main.gameStateManager.GetCurrentGameState().PopMenu();

			UI.Start();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

			MenuHelper.DoPlayerInventory(player, playerInventory, heldInventory, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, SIZE, PADDING);

			UI.EndParent();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32 + Player.INVENTORY_ROWS * SIZE + MARGIN));

			MenuHelper.DoEntityInventory(owner, chestInventory, heldInventory, rows, columns, SIZE, PADDING);

			UI.EndParent();
		}

		public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);

            var invManager = gsManager.TheIsland.GetClient().inventoryManager;
            var heldInventory = invManager.Get(this.heldInventory);

            MenuHelper.DrawHeldItem(batch, heldInventory.Get(0), SIZE, SCALE);
		}
	}
}
