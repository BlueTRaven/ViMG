using BrUtility;
using Engine;
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
    public class CatalystAnvilIronArmor : IRecipeCatalyst
	{
		public void RegisterRecipes(List<Recipe> recipes)
		{
			recipes.Add(new RecipeLayout("anvil_helmet_tin", this,
				new ItemInstance[]
				{
					new ItemInstance(),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 2, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 3, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 2, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 3, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("helmet_tin"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_legs_tin", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 3, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("legs_tin"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_body_tin", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 2, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 4, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 4, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 4, 1),
					new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), 4, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("body_tin"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_helmet_copper", this,
				new ItemInstance[]
				{
					new ItemInstance(),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 2, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 3, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 2, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 3, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("helmet_copper"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_anvil_legs_copper", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 3, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("legs_copper"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_body_copper", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 2, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 4, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 4, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 4, 1),
					new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), 4, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("body_copper"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_helmet_iron", this,
				new ItemInstance[]
				{
					new ItemInstance(),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 2, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 3, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 2, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 3, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("helmet_iron"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_legs_iron", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 3, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("legs_iron"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_body_iron", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 2, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 4, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 4, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 4, 1),
					new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), 4, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("body_iron"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_helmet_bronze", this,
				new ItemInstance[]
				{
					new ItemInstance(),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 2, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 2, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("helmet_bronze"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_legs_bronze", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("legs_bronze"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_body_bronze", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 2, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 4, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 4, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 4, 1),
					new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), 4, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("body_bronze"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_helmet_bone", this,
				new ItemInstance[]
				{
					new ItemInstance(),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 3, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_enchanted_bone"), 7, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 4, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 4, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("helmet_bone"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_legs_bone", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_enchanted_bone"), 7, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 4, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 4, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 4, 1), new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 4, 1)
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("legs_bone"), 1, 1) }));

			recipes.Add(new RecipeLayout("anvil_body_bone", this,
				new ItemInstance[]
				{
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 3, 1),
					new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 5, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_enchanted_bone"), 8, 1), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 5, 1),
					new ItemInstance(), new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), 5, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(GlobalState.Registry.ItemRegistry.Get("body_bone"), 1, 1) }));
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

			itemSlots[0] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
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

				itemSlots[i] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
					recipe.Layout[i]);
			}

			bounds.y += UIConstants.SIZE * 2;

			UI.MakeTexture(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += UIConstants.SIZE;

			itemSlots[7] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Outputs[0]);
		}

		public void DoRecipeUI(out Size size, Recipe recipe, float textureSize, float textureScale)
		{
			Vector2 pos = Vector2.Zero;
			RectangleF bounds = new RectangleF(pos, textureSize, textureSize);

			var itemSlotA = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Layout[0]);


			bounds.x += textureSize;

			var itemSlotB = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Layout.Length > 1 ? recipe.Layout[1] : new ItemInstance());

			bounds.x -= textureSize;
			bounds.y += textureSize;

			var itemSlotC = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Layout.Length > 2 ? recipe.Layout[2] : new ItemInstance());

			bounds.y -= textureSize;

			bounds.y += textureSize * 2;

			UI.MakeTexture(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += textureSize;

			var itemSlotOut = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Outputs[0]);

			size = new Size(textureSize * 2, bounds.y + bounds.height);
		}

		public string GetName()
		{
			return "Anvil (Armor)";
		}

		public Texture2D GetTexture()
		{
			return GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory");
		}

		public RectangleF GetSourceRect()
		{
			return new RectangleF(48, 80, 16, 16);
		}
	}
}
