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
		public CubeFurnace() : base("furnace_t1", new CubeFacingLayout(new RectangleF(144, 32, 16, 16), new RectangleF(160, 32, 16, 16), new RectangleF(160, 32, 16, 16)), Color.White, 6)
		{
			Main.Registry.RecipeRegistry.RegisterCatalyst(this);
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			Vector3 dir = player.Position - (position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2));
			Vector2 dirXZ = new Vector2(dir.X, dir.Z);

			MeshHelper.CubeFace face;

			if (dirXZ.Length() > MathF.Abs(dir.Y))
            {
				if (MathF.Abs(dirXZ.X) > MathF.Abs(dirXZ.Y))
                {
					//facing left or right
					if (dirXZ.X > 0)
						face = MeshHelper.CubeFace.RIGHT;
					else face = MeshHelper.CubeFace.LEFT;
                }
                else
                {
					if (dirXZ.Y > 0)
						face = MeshHelper.CubeFace.BACK;
					else face = MeshHelper.CubeFace.FRONT;
                }
            }
            else
            {
				if (dir.Y > 0)
					face = MeshHelper.CubeFace.UP;
				else face = MeshHelper.CubeFace.DOWN;
            }

			player.GetWorld().EntityManager.Add(new EntityFurnace(position, face));
		}

        public override RectangleF GetSourceRect(RenderPass pass, World world, CubePosition pos, MeshHelper.CubeFace face)
        {
			if (world != null)
			{
				var ent = world.EntityManager.GetEntityTrackingPosition(pos);
				if (ent.GetOrDefault(null) != null)
                {
					EntityFurnace furnace = ent.Get() as EntityFurnace;

					if (face == furnace.Facing)
						return new RectangleF(176, 32, 16, 16);
                }
			}

            return base.GetSourceRect(pass, world, pos, face);
        }

        public override CubeAnimation GetAnimation(MeshHelper.CubeFace face, RenderPass pass, World world, CubePosition pos)
        {
			if (world != null)
			{
				var ent = world.EntityManager.GetEntityTrackingPosition(pos);
				if (ent.GetOrDefault(null) != null)
				{
					EntityFurnace furnace = ent.Get() as EntityFurnace;

					if (face == furnace.Facing)
						return new CubeAnimation(0.125f, 3);
				}
			}

			return base.GetAnimation(face, pass, world, pos);
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

			itemSlots[0] = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
				recipe.Layout[0]);

			bounds.x += UIConstants.SIZE;

			ItemInstance item = new ItemInstance();
			if (recipe.Layout.Length > 1)
				item = recipe.Layout[1];

			itemSlots[1] = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
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

				itemSlots[2 + i] = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
					new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
					instance);

				bounds.x += UIConstants.SIZE;
			}
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
