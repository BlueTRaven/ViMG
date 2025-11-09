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
		public Size GetSize()
		{
			return new Size(UIConstants.SIZE * 5f, UIConstants.SIZE * 2);
		}

		public void DoRecipeUI2(UI.ItemSlot[] itemSlots, Recipe recipe)
		{
			RectangleF bounds = new RectangleF(0, 0, UIConstants.SIZE, UIConstants.SIZE);

			for (int i = 0; i < 6; i++)
			{
				int x = i % 3;
				int y = i / 3;

				bounds.x = x * UIConstants.SIZE;
				bounds.y = y * UIConstants.SIZE;
				
				ItemInstance instance;
				if (i < recipe.Layout.Length)
					instance = recipe.Layout[i];
				else instance = new ItemInstance();

				itemSlots[i] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
					new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
					instance);
			}

			bounds.x += UIConstants.SIZE;
			bounds.y = 0;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 48, 16, 16));

			bounds.x += UIConstants.SIZE;

			for (int i = 0; i < 4; i++)
			{
				RectangleF b = bounds;
				b.x += (i % 2) * UIConstants.SIZE;
				b.y += (int)(i / 2f) * UIConstants.SIZE;

				ItemInstance instance;
				if (i < recipe.Outputs.Length)
					instance = recipe.Outputs[i];
				else instance = new ItemInstance();
					
				itemSlots[6 + i] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
					new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
					instance);
			}
		}

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

				UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
					new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
					instance);
			}

			bounds.x += textureSize;
			bounds.y = 0;
		
			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 48, 16, 16));

			bounds.x += textureSize;

			for (int i = 0; i < recipe.Outputs.Length; i++)
			{
				UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
					new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
					recipe.Outputs[i]);

				bounds.x += textureSize;
			}

			size = new Size(bounds.x, textureSize + bounds.height);
		}

		public void RegisterRecipes(List<Recipe> recipes)
		{
			ItemRegistry registry = Main.Registry.ItemRegistry;

			recipes.Add(new RecipeLayout("wood_platform", this, 
				new ItemInstance[6] 
				{ 
					new ItemInstance(registry.Get("item_wood"), 1, 1), new ItemInstance(registry.Get("item_wood"), 1, 1), new ItemInstance(),
					new ItemInstance(registry.Get("wood"), 1, 1), new ItemInstance(registry.Get("wood"), 1, 1), new ItemInstance()
				},
				new ItemInstance[] { new ItemInstance(registry.Get("item_wood_platform"), 6, 1) }));

			recipes.Add(new RecipeFuzzy("book", this,
				new ItemInstance[] { new ItemInstance(registry.Get("paper"), 12, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("book_blank"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("paper", this,
				new ItemInstance[] { new ItemInstance(registry.Get("item_fibrous_plant"), 1, 1), new ItemInstance(registry.Get("wood"), 1, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("paper"), 4, 1) }, 2));

			recipes.Add(new RecipeLayout("arrow_stone", this,
				new ItemInstance[] { new ItemInstance(registry.Get("item_stone"), 1, 1), new ItemInstance(registry.Get("wood"), 1, 1), new ItemInstance(registry.Get("string"), 1, 1) }, 
				new ItemInstance[] { new ItemInstance(registry.Get("ammo_arrow_stone"), 4, 1) }));

			recipes.Add(new RecipeLayout("musketball", this,
				new ItemInstance[] { new ItemInstance(registry.Get("item_stone"), 4, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("ammo_bullet_musketball"), 4, 1) }));

			recipes.Add(new RecipeLayout("wood_chest", this,
				new ItemInstance[] 
				{
					new ItemInstance(registry.Get("wood"), 2, 1), new ItemInstance(registry.Get("item_wood"), 1, 1), new ItemInstance(registry.Get("wood"), 2, 1),
					new ItemInstance(registry.Get("wood"), 2, 1), new ItemInstance(registry.Get("wood"), 1, 1), new ItemInstance(registry.Get("wood"), 2, 1)
				},
				new ItemInstance[]
                {
					new ItemInstance(registry.Get("item_chest_wood"), 1, 1)
                }));

			recipes.Add(new RecipeFuzzy("string", this,
				new ItemInstance[] { new ItemInstance(registry.Get("item_fibrous_plant"), 1, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("string"), 4, 1) }));

			recipes.Add(new RecipeFuzzy("wood", this,
				new ItemInstance[] { new ItemInstance(registry.Get("wood"), 2, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("item_wood"), 4, 1) }));

			recipes.Add(new RecipeFuzzy("rope", this,
				new ItemInstance[] { new ItemInstance(registry.Get("string"), 4, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("rope"), 2, 1) }));

			recipes.Add(new RecipeFuzzy("bundled_wood", this,
				new ItemInstance[] { new ItemInstance(registry.Get("wood"), 4, 1), new ItemInstance(registry.Get("string"), 1, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("item_bundled_wood"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("brittle_bone_block", this,
				new ItemInstance[] { new ItemInstance(registry.Get("brittle_bone"), 6, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("item_brittle_bone_block"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("scroll_sonar", this,
				new ItemInstance[] { new ItemInstance(registry.Get("paper"), 2, 1), new ItemInstance(registry.Get("glowdust"), 7, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("scroll_sonar"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("brittle_enchanted_bone", this,
				new ItemInstance[] { new ItemInstance(registry.Get("brittle_bone"), 1, 1), new ItemInstance(registry.Get("glowdust"), 20, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("brittle_enchanted_bone"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("brittle_infused_bone", this,
				new ItemInstance[] { new ItemInstance(registry.Get("brittle_bone"), 1, 1), new ItemInstance(registry.Get("altar_dust"), 2, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("brittle_infused_bone"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("suspiciously_glowing_skull", this,
				new ItemInstance[]
				{
					new ItemInstance(registry.Get("brittle_infused_bone"), 4, 1),
					new ItemInstance(registry.Get("brittle_enchanted_bone"), 16, 1),
					new ItemInstance(registry.Get("altar_dust"), 6, 1),
				},
				new ItemInstance[] { new ItemInstance(registry.Get("bs_suspiciously_glowing_skull"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("ancient_altar", this,
				new ItemInstance[] { new ItemInstance(registry.Get("altar_dust"), 8, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("item_ancient_altar_placeable"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("flask_healthpotion1", this,
				new ItemInstance[] { new ItemInstance(registry.Get("flask_empty"), 1, 1), new ItemInstance(registry.Get("slime_chunk"), 2, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("flask_healthpotion1"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("flask_magicpotion1", this,
				new ItemInstance[] 
				{
					new ItemInstance(registry.Get("flask_empty"), 2, 1), 
					new ItemInstance(registry.Get("glowdust"), 6, 1),
					new ItemInstance(registry.Get("item_azure_flower"), 1, 1)
				},
				new ItemInstance[] { new ItemInstance(registry.Get("flask_magicpotion1"), 2, 1) }));

			/*recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(registry.Get("wood"), 1, 1), new ItemInstance(registry.Get("glowdust"), 4, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("glow_node"), 1, 1) }));*/

			recipes.Add(new RecipeFuzzy("furnace_t1", this,
				new ItemInstance[] { new ItemInstance(registry.Get("item_stone"), 30, 1), new ItemInstance(registry.Get("glowdust"), 50, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("item_furnace_t1"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("anvil_iron", this,
				new ItemInstance[] { new ItemInstance(registry.Get("ingot_bronze"), 20, 1) },
				new ItemInstance[] { new ItemInstance(registry.Get("item_anvil_iron"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("pickaxe_head_iron", this,
				new ItemInstance[] { new ItemInstance(registry.Get("pickaxe_head_iron"), 1, 1), new ItemInstance(registry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemPickaxe.CreatePickaxe(new ItemInstance(registry.Get("pickaxe_head_iron"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy("sword_blade_iron", this,
				new ItemInstance[] { new ItemInstance(registry.Get("sword_blade_iron"), 1, 1), new ItemInstance(registry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemSword.CreateSword(new ItemInstance(registry.Get("sword_blade_iron"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy("pickaxe_head_tin", this,
				new ItemInstance[] { new ItemInstance(registry.Get("pickaxe_head_tin"), 1, 1), new ItemInstance(registry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemPickaxe.CreatePickaxe(new ItemInstance(registry.Get("pickaxe_head_tin"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy("sword_blade_tin", this,
				new ItemInstance[] { new ItemInstance(registry.Get("sword_blade_tin"), 1, 1), new ItemInstance(registry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemSword.CreateSword(new ItemInstance(registry.Get("sword_blade_tin"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy("pickaxe_head_copper", this,
				new ItemInstance[] { new ItemInstance(registry.Get("pickaxe_head_copper"), 1, 1), new ItemInstance(registry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemPickaxe.CreatePickaxe(new ItemInstance(registry.Get("pickaxe_head_copper"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy("sword_blade_copper", this,
				new ItemInstance[] { new ItemInstance(registry.Get("sword_blade_copper"), 1, 1), new ItemInstance(registry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemSword.CreateSword(new ItemInstance(registry.Get("sword_blade_copper"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy("pickaxe_head_bronze", this,
				new ItemInstance[] { new ItemInstance(registry.Get("pickaxe_head_bronze"), 1, 1), new ItemInstance(registry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemPickaxe.CreatePickaxe(new ItemInstance(registry.Get("pickaxe_head_bronze"), 1, 1)) }));

			recipes.Add(new RecipeFuzzy("sword_blade_bronze", this,
				new ItemInstance[] { new ItemInstance(registry.Get("sword_blade_bronze"), 1, 1), new ItemInstance(registry.Get("wood"), 3, 1), },
				new ItemInstance[] { ItemSword.CreateSword(new ItemInstance(registry.Get("sword_blade_bronze"), 1, 1)) }));
		}

		public string GetName()
		{
			return "Inventory";
		}

		public Texture2D GetTexture()
		{
			return Main.assetsManager.GetAsset<Texture2D>("ui_inventory");
		}

		public RectangleF GetSourceRect()
		{
			return new RectangleF(32, 64, 16, 16);
		}
	}
}
