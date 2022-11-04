using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;
using ViMG.UIs;

namespace ViMG.Recipes
{
    public class CatalystAnvilIronTools : IRecipeCatalyst
    {
        public void RegisterRecipes(List<Recipe> recipes)
		{
			recipes.Add(new RecipeLayout(this,
				new ItemInstance[]
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("item_lava_crystal"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("brittle_enchanted_bone"), 12, 1),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("brittle_enchanted_bone"), 12, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 7, 1)
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_lavacrystal"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(), 
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 6, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 6, 1),
					new ItemInstance(), new ItemInstance(), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_iron"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 6, 1), new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 8, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_iron"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("string"), 3, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 7, 1),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 7, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bow_iron"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 6, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 6, 1),
					new ItemInstance(), new ItemInstance(), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_tin"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 6, 1), new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 8, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_tin"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("string"), 3, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 7, 1),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 7, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bow_tin"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 6, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 6, 1),
					new ItemInstance(), new ItemInstance(), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_copper"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 6, 1), new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 8, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_copper"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("string"), 3, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 7, 1),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 7, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bow_copper"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 6, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 6, 1),
					new ItemInstance(), new ItemInstance(), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_bronze"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 6, 1), new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 8, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_bronze"), 1, 1) }));

			recipes.Add(new RecipeLayout(this,
				new ItemInstance[] 
				{
					new ItemInstance(),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("string"), 3, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 7, 1),
					new ItemInstance(), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 7, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bow_bronze"), 1, 1) }));
		}

		public Size GetSize()
        {
			return new Size(UIConstants.SIZE * 3f, UIConstants.SIZE * 5);
        }

		public void DoRecipeUI2(UI.ItemSlot[] itemSlots, Recipe recipe)
        {
			Vector2 pos = Vector2.Zero;
			RectangleF bounds = new RectangleF(pos, UIConstants.SIZE, UIConstants.SIZE);

			bounds.x += UIConstants.SIZE;

			itemSlots[0] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Layout[0]);

			bounds.x -= UIConstants.SIZE;
			bounds.y += UIConstants.SIZE;

			for (int i = 1; i < 7; i++)
			{
				int j = i - 1;

				//copies
				RectangleF b = bounds;

				b.x += (j % 3) * UIConstants.SIZE;
				b.y += (int)(j / 3f) * UIConstants.SIZE;

				itemSlots[i] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(b, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
					recipe.Layout[i]);
			}

			bounds.y += UIConstants.SIZE * 2;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += UIConstants.SIZE;

			itemSlots[7] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Outputs[0]);
		}

		public void DoRecipeUI(out Size size, Recipe recipe, float textureSize, float textureScale)
		{
			Vector2 pos = Vector2.Zero;
			RectangleF bounds = new RectangleF(pos, textureSize, textureSize);

			var itemSlotA = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Layout[0]);

			bounds.x += textureSize;

			var itemSlotB = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Layout.Length > 1 ? recipe.Layout[1] : new ItemInstance());

			bounds.x -= textureSize;
			bounds.y += textureSize;

			var itemSlotC = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Layout.Length > 2 ? recipe.Layout[2] : new ItemInstance());

			bounds.y -= textureSize;

			bounds.y += textureSize * 2;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += textureSize;

			var itemSlotOut = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Outputs[0]);

			size = new Size(textureSize * 2, bounds.y + bounds.height);
		}

		public string GetName()
		{
			return "Anvil (Tools)";
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
