using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;
using ViMG.Items;
using ViMG.Recipes;
using ViMG.UIs;

namespace ViMG.Cubes
{
	public class CubeAnvilIron : Cube, IRecipeCatalyst
	{
		public CubeAnvilIron() : base("anvil_iron", new RectangleF(160, 0, 16, 16), Color.White, 6)
		{
			Main.Registry.RecipeRegistry.RegisterCatalyst(this);
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			player.GetWorld().EntityManager.Add(new EntityAnvilIron(position));
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("item_anvil_iron"), 1, 1));
		}

		public void RegisterRecipes(List<Recipe> recipes)
		{
			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 6, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 6, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_iron"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 6, 1), new ItemInstance(),
					new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 8, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_iron"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("string"), 3, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 7, 1),
					new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 7, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bow_iron"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 6, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 6, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_tin"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 6, 1), new ItemInstance(),
					new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 8, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_tin"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("string"), 3, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 7, 1),
					new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 7, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bow_tin"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 6, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 6, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_copper"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 6, 1), new ItemInstance(),
					new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 8, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_copper"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("string"), 3, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 7, 1),
					new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 7, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bow_copper"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 6, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 6, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_bronze"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 6, 1), new ItemInstance(),
					new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 8, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_bronze"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("string"), 3, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 7, 1),
					new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 7, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bow_bronze"), 1, 1) }));
		}

		public void DoRecipeUI(out Size size, Recipe recipe, float textureSize, float textureScale)
		{
			Vector2 pos = Vector2.Zero;
			RectangleF bounds = new RectangleF(pos, textureSize, textureSize);

			var itemSlotA = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								recipe.Layout[0]);


			bounds.x += textureSize;

			var itemSlotB = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								recipe.Layout.Length > 1 ? recipe.Layout[1] : new ItemInstance());

			bounds.x -= textureSize;
			bounds.y += textureSize;

			var itemSlotC = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								recipe.Layout.Length > 2 ? recipe.Layout[2] : new ItemInstance());

			bounds.y -= textureSize;

			bounds.y += textureSize * 2;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += textureSize;

			var itemSlotOut = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								recipe.Outputs[0]);

			size = new Size(textureSize * 2, bounds.y + bounds.height);
		}

		public string GetName()
		{
			return "Anvil";
		}

		public Texture2D GetTexture()
		{
			return Main.assetsManager.GetAsset<Texture2D>("ui_inventory");
		}

		public RectangleF GetSourceRect()
		{
			return new RectangleF(32, 80, 16, 16);
		}
	}
}
