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
		private UI.ButtonConstructionParameters buttonParameters;

		public CubeFurnace() : base("furnace_t1", new CubeFacingLayout(new RectangleF(144, 32, 16, 16), new RectangleF(160, 32, 16, 16), new RectangleF(160, 32, 16, 16)), Color.White, 6)
		{
			Main.Registry.GetCurrentMod().Registry.RecipeRegistry.RegisterCatalyst(this);

			buttonParameters = new UI.ButtonConstructionParameters(new RectangleF(Vector2.Zero, 18 * 2, 18 * 2), 
				Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(92, 0, 18, 18), new RectangleF(110, 0, 18, 18), new RectangleF(110, 0, 18, 18));
        }

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			MeshHelper.CubeFace face = CubeHelper.GetFaceFromPlayerPos(player, position);

			player.GetWorld().EntityManager.Add(new EntityFurnace(position, face));
		}

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
			if (data != null && data.GetValid())
			{
				var meshingData = data.GetEntityMeshingData<EntityFurnace.MeshingData>(parameters.position);
				if (face == meshingData.facing)
					return new RectangleF(176, 32, 16, 16);
			}

			return base.GetSourceRect(pass, data, parameters, face);
        }

        public override CubeAnimation GetAnimation(RenderPass pass, CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
			if (data != null && data.GetValid())
			{
				var meshingData = data.GetEntityMeshingData<EntityFurnace.MeshingData>(parameters.position);
				if (face == meshingData.facing)
					return new CubeAnimation(0.125f, 3, 16);
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
			recipes.Add(new RecipeFuzzy("sand_to_flask", this,
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_sand"), 1, 1) },
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("flask_empty"), 1, 1) }));

			recipes.Add(new RecipeLayout("sand_to_glass", this,
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_sand"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("item_sand"), 1, 1) },
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("item_glass"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("iron_chunk_to_ingot",this,
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("iron_chunk"), 1, 1) },
							new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("tin_chunk_to_ingot", this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("tin_chunk"), 1, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("copper_chunk_to_ingot", this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("copper_chunk"), 1, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), 1, 1) }));

			recipes.Add(new RecipeFuzzy("tin_copper_chunk_to_bronze_ingot", this,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("tin_chunk"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("copper_chunk"), 2, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), 3, 1) }, 2));

			recipes.Add(new RecipeFuzzy("tin_copper_ingot_to_bronze_ingot", this,
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

			itemSlots[0] = UI.MakeItemSlot(UI.MakeButton(buttonParameters), recipe.Layout[0]);

			UI.StartParent(new Vector2(18f * 2f));

			ItemInstance item = new ItemInstance();
			if (recipe.Layout.Length > 1)
				item = recipe.Layout[1];

			itemSlots[1] = UI.MakeItemSlot(UI.MakeButton(buttonParameters), item);

			UI.EndParent();
			UI.StartParent(new Vector2(0, 18 * 4));

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

            UI.EndParent();
            UI.StartParent(new Vector2(0, 18 * 6));

			int xOff = 0;

			for (int i = 0; i < recipe.Outputs.Length; i++)
			{
				ItemInstance instance;
				if (i < recipe.Outputs.Length)
					instance = recipe.Outputs[i];
				else instance = new ItemInstance();

				itemSlots[2 + i] = UI.MakeItemSlot(UI.MakeButton(buttonParameters), instance);

				xOff += 18 * 2;

				UI.EndParent();
				UI.StartParent(new Vector2(xOff, 18 * 6));
			}

			UI.EndParent();
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
