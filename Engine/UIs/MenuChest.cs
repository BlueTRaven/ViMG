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
        private readonly Inventory playerInventory;
		private readonly Inventory heldInventory;
		private readonly Inventory chestInventory;
		private readonly int rows;
		private readonly int columns;

		private ItemInstance held;

		public MenuChest(GameStateManager gsManager, Player player, Entity owner, Inventory playerInventory, Inventory heldInventory, Inventory chestInventory, int rows, int columns) : base(gsManager)
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

			MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
		}
	}
}
