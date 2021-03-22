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
	public class CubeFurnace : Cube, IRecipeCatalyst
	{
		public CubeFurnace() : base("furnace_t1", new CubeFacingLayout(new RectangleF(144, 0, 16, 16), new RectangleF(160, 0, 16, 16), new RectangleF(160, 0, 16, 16)), Color.White, 6)
		{
			Main.Registry.RecipeRegistry.RegisterCatalyst(this);
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			player.GetWorld().EntityManager.Add(new EntityFurnace(position));
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("item_furnace_t1"), 1, 1));
		}

		public void RegisterRecipes(List<Recipe> recipes)
		{
			recipes.Add(new RecipeFuzzy(this,
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_sand"), 1, 1) },
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("flask_empty"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("iron_chunk"), 1, 1) },
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("iron_ingot"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("tin_chunk"), 1, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("tin_ingot"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("copper_chunk"), 1, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("copper_ingot"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("tin_chunk"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("copper_chunk"), 2, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bronze_ingot"), 3, 1) }, 2));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("tin_ingot"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("copper_ingot"), 2, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("bronze_ingot"), 3, 1) }));
		}

		public void DoRecipeUI(out Size size, Recipe recipe, float textureSize, float textureScale)
		{
			RectangleF bounds = new RectangleF(Vector2.Zero, textureSize, textureSize);

			UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								recipe.Layout[0]);

			bounds.x += textureSize;

			ItemInstance item = new ItemInstance();
			if (recipe.Layout.Length > 1)
				item = recipe.Layout[1];

			UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
									item);

			bounds.x -= textureSize;

			bounds.y += textureSize * 1.25f;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += textureSize * 1.25f;

			for (int i = 0; i < recipe.Outputs.Length; i++)
			{
				UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
									recipe.Outputs[i]);

				bounds.x += textureSize;
			}

			size = new Size(textureSize * 2, bounds.y + bounds.height);
		}
	}
}
