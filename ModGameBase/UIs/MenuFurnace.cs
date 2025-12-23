using BepuPhysics.Constraints;
using BrNineSlice;
using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;
using ViMG.Recipes;

namespace ViMG.UIs
{
	public class MenuFurnace<T> : Menu where T : Entity, IHasInventory
    {
        private EntityManager.EntityReference player;
		private readonly EntityManager.EntityReference owner;
		private InventoryManager.InventoryReference playerInventory;
		private InventoryManager.InventoryReference heldInventory;
		private InventoryManager.InventoryReference furnaceInventory;
		private bool furnaceInventoryUpdated;
		private Recipe currentRecipe;

		private Items.ItemInstance held;

        public MenuFurnace(GameStateManager gsManager, EntityManager.EntityReference player, EntityManager.EntityReference owner, InventoryManager.InventoryReference playerInventory, InventoryManager.InventoryReference heldInventory, InventoryManager.InventoryReference furnaceInventory) : base(gsManager)
		{
			this.player = player;
            this.owner = owner;
            this.playerInventory = playerInventory;
			this.heldInventory = heldInventory;
			this.furnaceInventory = furnaceInventory;
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

			var invManager = gsManager.TheIsland.GetWorld().InventoryManager;
            var furnaceInventory = invManager.Get(this.furnaceInventory);
            var playerInventory = invManager.Get(this.playerInventory);
            var heldInventory = invManager.Get(this.heldInventory);

            TextHelper.FontInfo fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);

			UI.Start();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

            MenuHelper.DoPlayerInventory(player, playerInventory, heldInventory, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * 2f, 2);

			UI.EndParent();

			UI.StartParent(new Vector2(MenuHelper.GetInventorySize(Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * 2f, 2f).Width + 18 * 2f, MARGIN + 32));

			UI.MakePanel(Color.White, new RectangleF(0, 0, 18 * 2 * 4.5f, 18 * 2 * 3 + 16 * 2), MenuHelper.MainPanelNS);

			UI.StartParent(new Vector2(MARGIN));

			RectangleF bounds = new RectangleF(Vector2.Zero, SIZE, SIZE);

			var buttonParams = MenuHelper.ButtonParameters;
			buttonParams.bounds.Size = new Size(18 * 2);
			var itemSlotA = UI.MakeItemSlot(UI.MakeButton(buttonParams), furnaceInventory.Get(0));

			UI.StartParent(new Vector2(18 * 2, 0));
			//bounds.x += SIZE;

			var itemSlotB = UI.MakeItemSlot(UI.MakeButton(buttonParams), furnaceInventory.Get(1));

            UI.StartParent(new Vector2(18 * 2 + 16, 0));
            //bounds.x += SIZE + MARGIN;

			var itemSlotFuel = UI.MakeItemSlot(UI.MakeButton(buttonParams), furnaceInventory.Get(2));

			UI.EndParent();
			//bounds.x -= SIZE + MARGIN;

			MenuHelper.ItemSlotClickOutput output = MenuHelper.HandleItemSlot(owner, furnaceInventory, 0, itemSlotA, heldInventory);
			if (output != MenuHelper.ItemSlotClickOutput.None)
			{
				if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
					MenuHelper.SwapInventory(furnaceInventory, playerInventory, 0);

				furnaceInventoryUpdated = true;
			}

			output = MenuHelper.HandleItemSlot(owner, furnaceInventory, 1, itemSlotB, heldInventory);
            if (output != MenuHelper.ItemSlotClickOutput.None)
			{
				if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
					MenuHelper.SwapInventory(furnaceInventory, playerInventory, 1);

				furnaceInventoryUpdated = true;
			}

			output = MenuHelper.HandleItemSlot(owner, furnaceInventory, 2, itemSlotFuel, heldInventory);
            if (output != MenuHelper.ItemSlotClickOutput.None)
			{
				if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
					MenuHelper.SwapInventory(furnaceInventory, playerInventory, 2);

				furnaceInventoryUpdated = true;
			}

			// TODO: this should be server-side only
			//if (furnaceInventoryUpdated)
			//{
			//	furnaceInventoryUpdated = false;
			//	currentRecipe = furnace.FindRecipe();

			//	if (currentRecipe != null)
			//	{
			//		for (int i = 0; i < Math.Min(2, currentRecipe.Outputs.Length); i++)
			//		{
			//			furnaceInventory.Set(currentRecipe.Outputs[i], 3 + i);
			//		}
			//	}
			//	else
			//	{
			//		furnaceInventory.Set(new ItemInstance(), 3);
			//		furnaceInventory.Set(new ItemInstance(), 4);
			//	}
			//}

			UI.EndParent();

			UI.StartParent(new Vector2(0, 18 * 2));
			//bounds.x -= SIZE;
			//bounds.y += SIZE;
			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));
            UI.StartParent(new Vector2(0, 18 * 2));
            //bounds.y += SIZE;

			UI.MakeItemSlot(UI.MakeButton(buttonParams), furnaceInventory.Get(3));

            UI.StartParent(new Vector2(18 * 2, 0));
            //bounds.x += SIZE;

            UI.MakeItemSlot(UI.MakeButton(buttonParams), furnaceInventory.Get(4));

            UI.StartParent(new Vector2(18 * 2, 0));
            //bounds.x += SIZE;

            UI.Button craftRecipeButton = UI.MakeButton(MenuHelper.ActionButtonParameters);
			UI.MakeTexture(new RectangleF(1, 1, 16, 16).Scale(2), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(0, 96, 16, 16));

			if (craftRecipeButton.clickLeft)
			{
				MenuHelper.InventoryAction(owner, gsManager.TheIsland.GetClient().Current().entities.GetPlayerIndex(player), 1);
			}
			else if (craftRecipeButton.hovered)
			{
				UI.DisableParent();
				UI.MakeLabel("Craft Recipe", fi, 256, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16));
				UI.EnableParent();
			}

			UI.EndParent();

            UI.EndParent();
            UI.EndParent();
            UI.EndParent();
            UI.EndParent();

            //bounds.x = MARGIN;
			//bounds.y += SIZE * 2 + MARGIN;

			UI.Button recipeBookButton = UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(16, 18 * 2 * 4.5f, 16 * 2, 16 * 2), 
				Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), 
				new RectangleF(0, 80, 16, 16), new RectangleF(16, 80, 16, 16), new RectangleF(16, 80, 16, 16)));

			if (recipeBookButton.hovered)
			{
				UI.DisableParent();
				UI.MakeLabel("Recipe Book", fi, 128, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16));
				UI.EnableParent();
			}

			if (recipeBookButton.clickLeft)
			{
				gsManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(gsManager, Main.Registry.CubeRegistry.Get("furnace_t1") as CubeFurnace, new ItemInstance()));
			}

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
