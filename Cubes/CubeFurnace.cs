using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.Items;
using ViMG.Recipes;
using ViMG.UIs;

namespace ViMG.Cubes
{
	public class CubeFurnace : Cube, IRecipeCatalyst
	{
		public CubeFurnace() : base("furnace_t1", new CubeFacingLayout(new RectangleF(144, 32, 16, 16), new RectangleF(160, 32, 16, 16), new RectangleF(160, 32, 16, 16)), Color.White, 6)
		{
			Main.Registry.RecipeRegistry.RegisterCatalyst(this);
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			MeshHelper.CubeFace face = CubeHelper.GetFaceFromPlayerPos(player, position);

			player.GetWorld().EntityManager.Add(new EntityFurnace(position, face));
		}

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
			if (data != null && data.GetValid())
			{
				var meshingData = data.GetEntityMeshingData(parameters.position);
				if (meshingData != null)
                {
					EntityFurnace.MeshingData castedMeshingData = (EntityFurnace.MeshingData)meshingData;
					if (face == castedMeshingData.facing)
						return new RectangleF(176, 32, 16, 16);
                }
			}

			return base.GetSourceRect(pass, data, parameters, face);
        }

        public override CubeAnimation GetAnimation(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            if (data != null && data.GetValid())
            {
                var meshingData = data.GetEntityMeshingData(parameters.position);
                if (meshingData != null)
                {
                    EntityFurnace.MeshingData castedMeshingData = (EntityFurnace.MeshingData)meshingData;
                    if (face == castedMeshingData.facing)
                        return new CubeAnimation(0.125f, 3, 16);
				}
			}

			return base.GetAnimation(pass, data, parameters, face);
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

			recipes.Add(new RecipeLayout(this,
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_sand"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("item_sand"), 1, 1) },
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_glass"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("iron_chunk"), 1, 1) },
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("tin_chunk"), 1, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("copper_chunk"), 1, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 1, 1) }));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("tin_chunk"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("copper_chunk"), 2, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1) }, 2));

			recipes.Add(new RecipeFuzzy(this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 2, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1) }));
		}

		public Size GetSize()
		{
			return new Size(UIConstants.SIZE * 2f, UIConstants.SIZE * 3f);
		}

		public void DoRecipeUI2(UI.ItemSlot[] itemSlots, Recipe recipe)
		{
			RectangleF bounds = new RectangleF(Vector2.Zero, UIConstants.SIZE, UIConstants.SIZE);

			itemSlots[0] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				recipe.Layout[0]);

			bounds.x += UIConstants.SIZE;

			ItemInstance item = new ItemInstance();
			if (recipe.Layout.Length > 1)
				item = recipe.Layout[1];

			itemSlots[1] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
				item);

			bounds.x -= UIConstants.SIZE;

			bounds.y += UIConstants.SIZE;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += UIConstants.SIZE;

			for (int i = 0; i < recipe.Outputs.Length; i++)
			{
				ItemInstance instance;
				if (i < recipe.Outputs.Length)
					instance = recipe.Outputs[i];
				else instance = new ItemInstance();

				itemSlots[2 + i] = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
					new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
					instance);

				bounds.x += UIConstants.SIZE;
			}
		}

		public void DoRecipeUI(out Size size, Recipe recipe, float textureSize, float textureScale)
		{
			RectangleF bounds = new RectangleF(Vector2.Zero, textureSize, textureSize);

			UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
								recipe.Layout[0]);

			bounds.x += textureSize;

			ItemInstance item = new ItemInstance();
			if (recipe.Layout.Length > 1)
				item = recipe.Layout[1];

			UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
									item);

			bounds.x -= textureSize;

			bounds.y += textureSize * 1.25f;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += textureSize * 1.25f;

			for (int i = 0; i < recipe.Outputs.Length; i++)
			{
				UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
									recipe.Outputs[i]);

				bounds.x += textureSize;
			}

			size = new Size(textureSize * 2, bounds.y + bounds.height);
		}

		public string GetName()
        {
			return "Furnace";
        }

		public Texture2D GetTexture()
		{
			return Main.assetsManager.GetAsset<Texture2D>("ui_inventory");
		}

		public RectangleF GetSourceRect()
		{
			return new RectangleF(64, 80, 16, 16);
		}
	}
}
