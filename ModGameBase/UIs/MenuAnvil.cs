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
	public class MenuAnvil : Menu
	{
		private readonly EntityManager.EntityReference player;
		private readonly EntityManager.EntityReference owner;
		private readonly InventoryManager.InventoryReference playerInventory;
		private readonly InventoryManager.InventoryReference heldInventory;
		private readonly InventoryManager.InventoryReference anvilInventory;
        private bool inventoryUpdated;
		private Recipe currentRecipe;

		private Items.ItemInstance held;

		private TextHelper.FontInfo fi;

		private static Vector2 inventoryRight = new Vector2(MARGIN + Player.INVENTORY_COLUMNS * SIZE + Player.INVENTORY_COLUMNS * PADDING + MARGIN_CRAFTING, MARGIN + SIZE);

		public MenuAnvil(GameStateManager gsManager, EntityManager.EntityReference player, EntityManager.EntityReference owner, InventoryManager.InventoryReference playerInventory, InventoryManager.InventoryReference heldInventory, InventoryManager.InventoryReference anvilInventory) : base(gsManager)
		{
			this.player = player;
            this.owner = owner;
            this.playerInventory = playerInventory;
			this.heldInventory = heldInventory;
			this.anvilInventory = anvilInventory;

			fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
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

            var invManager = gsManager.TheIsland.GetClient().inventoryManager;
            var playerInventory = invManager.Get(this.playerInventory);
            var heldInventory = invManager.Get(this.heldInventory);
            MenuHelper.DoPlayerInventory(player, playerInventory, heldInventory, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, SIZE, PADDING);

			UI.EndParent();

			DoTools();
		}

		private UI.ItemSlot[] itemSlots;

		private void DoTools()
        {
            var invManager = gsManager.TheIsland.GetClient().inventoryManager;
            var playerInventory = invManager.Get(this.playerInventory);
            var heldInventory = invManager.Get(this.heldInventory);
            var anvilInventory = invManager.Get(this.anvilInventory);

            UI.StartParent(inventoryRight);

			UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, 0, SIZE * 4f, SIZE * 5 + MARGIN * 2));

			UI.StartParent(new Vector2(MARGIN));

			Vector2 pos = Vector2.Zero;
			RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

			if (itemSlots == null)
				itemSlots = new UI.ItemSlot[7];

			bounds.x += SIZE;
            
            itemSlots[0] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				anvilInventory.Get(0));

			bounds.x -= SIZE;
			bounds.y += SIZE;

			for (int i = 1; i < 7; i++)
            {
				int j = i - 1;

				//copies
				RectangleF b = bounds;

				b.x += (j % 3) * SIZE;
				b.y += (int)(j / 3f) * SIZE;

				itemSlots[i] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(b, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
					new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
					anvilInventory.Get(i));
			}

			for (int i = 0; i < 7; i++)
            {
				MenuHelper.ItemSlotClickOutput output = MenuHelper.HandleItemSlot(owner, anvilInventory, i, itemSlots[i], heldInventory);
				if (output != MenuHelper.ItemSlotClickOutput.None)
				{
					if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
						MenuHelper.SwapInventory(anvilInventory, playerInventory, i);
					inventoryUpdated = true;
				}
			}

			if (inventoryUpdated)
			{
				currentRecipe = FindRecipe(anvilInventory);

				if (currentRecipe != null)
				{
					for (int i = 0; i < Math.Min(1, currentRecipe.Outputs.Length); i++)
					{
						anvilInventory.Set(currentRecipe.Outputs[i], 7 + i);
					}
				}
				else
				{
					anvilInventory.Set(new ItemInstance(), 7);
				}
			}

			bounds.y += SIZE * 2;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += SIZE;

			UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				anvilInventory.Get(7));

			bounds.x += SIZE;

			UI.Button craftRecipeButton = UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), 
				new RectangleF(0, 96, 16, 16), new RectangleF(16, 96, 16, 16), new RectangleF(16, 96, 16, 16)));
			if (craftRecipeButton.clickLeft)
			{
				if (currentRecipe != null)
					CraftItem(currentRecipe);
			}
			else if (craftRecipeButton.hovered)
			{
				UI.DisableParent();
				UI.MakeLabel("Craft Recipe", fi, 256, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16));
				UI.EnableParent();
			}

			bounds.x += SIZE;

			UI.Button recipeBookButton = UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 80, 16, 16), new RectangleF(16, 80, 16, 16), new RectangleF(16, 80, 16, 16)));

			if (recipeBookButton.hovered)
			{
				UI.DisableParent();
				UI.MakeLabel("Recipe Book", fi, 128, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16));
				UI.EnableParent();
			}

			if (recipeBookButton.clickLeft)
			{
                gsManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(gsManager, Main.Registry.RecipeRegistry.catalystByName["Anvil (Tools)"], new ItemInstance()));
			}

			UI.EndParent();
			UI.EndParent();
		}

		private Recipe FindRecipe(Inventory inventory)
		{
			var recipesTools = Main.Registry.RecipeRegistry.GetRecipesByCatalyst(Main.Registry.RecipeRegistry.catalystByName["Anvil (Tools)"]);
			var recipesArmor = Main.Registry.RecipeRegistry.GetRecipesByCatalyst(Main.Registry.RecipeRegistry.catalystByName["Anvil (Armor)"]);

			Recipe foundRecipe = null;

			for (int i = 0; i < recipesTools.Count; i++)
			{
				Recipe recipe = recipesTools[i];

				if (recipe.Matches(inventory))
				{
					if (foundRecipe == null || recipe.Weight > foundRecipe.Weight)
						foundRecipe = recipe;
				}
			}

			for (int i = 0; i < recipesArmor.Count; i++)
			{
				Recipe recipe = recipesArmor[i];

				if (recipe.Matches(inventory))
				{
					if (foundRecipe == null || recipe.Weight > foundRecipe.Weight)
						foundRecipe = recipe;
				}
			}

			return foundRecipe;
		}

		private void CraftItem(Recipe recipe)
		{
            var invManager = gsManager.TheIsland.GetClient().inventoryManager;
            var playerInventory = invManager.Get(this.playerInventory);
            var anvilInventory = invManager.Get(this.anvilInventory);

            if (recipe.Matches(anvilInventory))
			{
				for (int i = 0; i < recipe.Layout.Length; i++)
				{
					if (recipe.Layout[i].valid)
					{
						int numLeft = recipe.Layout[i].num;

						anvilInventory.FindExact(recipe.Layout[i], 7, out int index);

						int overflow = anvilInventory.Get(i).num - numLeft;
						anvilInventory.Remove(index, numLeft);
						inventoryUpdated  = true;

						if (overflow < 0)
							numLeft -= Math.Abs(overflow);
						else numLeft -= numLeft;
					}
				}

				for (int i = 0; i < recipe.Outputs.Length; i++)
				{
					playerInventory.Add(recipe.Outputs[i]);
				}
			}
		}

		public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);

			MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
		}
	}
}
