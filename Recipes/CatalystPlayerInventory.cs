using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;
using ViMG.UIs;

namespace ViMG.Recipes
{
	public class CatalystPlayerInventory : IRecipeCatalyst
	{
		public void DoRecipeUI(out Size size, Recipe recipe, float textureSize, float textureScale)
		{
			RectangleF bounds = new RectangleF(0, 0, textureSize, textureSize);

			for (int i = 0; i < 6; i++)
			{
				int x = i % 3;
				int y = i / 3;

				bounds.x = x * textureSize;
				bounds.y = y * textureSize;
				ItemInstance instance = new ItemInstance();

				if (i < recipe.Layout.Length)
				{
					instance = recipe.Layout[i];
				}

				UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
									instance);
			}

			bounds.x += textureSize;
			bounds.y = 0;
		
			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 48, 16, 16));

			bounds.x += textureSize;

			for (int i = 0; i < recipe.Outputs.Length; i++)
			{
				UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
									recipe.Outputs[i]);

				bounds.x += textureSize;
			}

			size = new Size(bounds.x, textureSize + bounds.height);
		}

		public void RegisterRecipes(List<Recipe> recipes)
		{
			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("brittle_bone"), 6, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_brittle_bone_block"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("brittle_bone"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("glowdust"), 20, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("brittle_enchanted_bone"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("brittle_bone"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("altar_dust"), 20, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("brittle_infused_bone"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("altar_dust"), 4, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_ancient_altar"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("flask_empty"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("slime_chunk"), 2, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("flask_healthpotion1"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("glowdust"), 4, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("glow_node"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_stone"), 30, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("glow_node"), 2, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_furnace_t1"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("iron_ingot"), 6, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_anvil_iron"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_iron"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemPickaxe.CreatePickaxe(new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_iron"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_iron"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemSword.CreateSword(new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_iron"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_tin"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemPickaxe.CreatePickaxe(new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_tin"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_tin"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemSword.CreateSword(new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_tin"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_copper"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemPickaxe.CreatePickaxe(new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_copper"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_copper"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemSword.CreateSword(new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_copper"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_bronze"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemPickaxe.CreatePickaxe(new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_bronze"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_bronze"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemSword.CreateSword(new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_bronze"), 1, 1)) }));
		}
	}
}
