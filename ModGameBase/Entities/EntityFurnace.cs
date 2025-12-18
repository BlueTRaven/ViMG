using BepuUtilities.Memory;
using Engine.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Recipes;
using ViMG.UIs;

namespace ViMG.Entities
{
	[EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(2, 0)]
	public class EntityFurnace : Entity, ICubeTracker, IHasInventory
	{
		public struct MeshingData
		{
            public MeshHelper.CubeFace facing;
        }

		public CubePosition TrackedPosition 
		{
			get;
			private set; 
		}

		private Inventory inventory;
		public MeshingData MeshingDataInstance;

		private float craftTimer;
		private int light = -1;

		public EntityFurnace()
		{
            DoesSync = false;

            MenuHelper.IWhiteList?[] whitelists = [null, null, new MenuHelper.WhiteListOneName("glowdust"), null, null];
            inventory = new Inventory(0, 5, whitelists);
        }

		public EntityFurnace(CubePosition position, MeshHelper.CubeFace facing)
		{
            DoesSync = false;

            this.TrackedPosition = position;
			MeshingDataInstance = new MeshingData()
			{
				facing = facing
			};

			Position = position.InWorldSpace();

			MenuHelper.IWhiteList?[] whitelists = [null, null, new MenuHelper.WhiteListOneName("glowdust"), null, null];
			inventory = new Inventory(0, 5, whitelists);
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			Optional<Entity> tracker = world.EntityManager.GetEntityTrackingPosition(TrackedPosition);

            if (tracker.HasValue())
                world.EntityManager.Kill(this);

            world.ChunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(TrackedPosition));//, true);
		}

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            inventory.ProcessActions(this);

			craftTimer -= (float)deltaTime;

			if (craftTimer <= 0 && light != -1)
            {
				world.LightManager.Remove(light);
				light = -1;
            }
        }

        public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
		{
			world.EntityManager.Kill(this);
		}

		public bool OnInteract(Player player)
		{
			if (player.IsLocalPlayer)
				Main.gameStateManager.GetCurrentGameState().PushMenu(new MenuFurnace<EntityFurnace>(Main.gameStateManager, player, this, player.GetInventory(), player.GetHeldInventory(), inventory, this));

			return true;
		}

        private bool CraftItem(Player? activatingPlayer, Recipe recipe)
        {
			bool activated = false;

            if (recipe.Matches(inventory))
            {
                for (int i = 0; i < recipe.Layout.Length; i++)
                {
                    if (recipe.Layout[i].valid)
                    {
                        int numLeft = recipe.Layout[i].num;

                        inventory.FindExact(recipe.Layout[i], 2, out int index);

                        int overflow = inventory.Get(i).num - numLeft;
                        inventory.Remove(index, numLeft);

						activated = true;
                        OnCraft();

                        if (overflow < 0)
                            numLeft -= Math.Abs(overflow);
                        else numLeft -= numLeft;
                    }
                }

                for (int i = 0; i < recipe.Outputs.Length; i++)
                {
                    activatingPlayer.GetInventory().Add(recipe.Outputs[i]);
                }
            }

            // remove fuel
            inventory.Remove(2, 1);

			return activated;
        }

        public void OnCraft()
        {
			craftTimer = 3f;

			if (light == -1)
				light = world.LightManager.Add(Position, Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8, Color.OrangeRed.ToVector4());
        }

        public Recipe FindRecipe()
        {
            var recipes = Main.Registry.RecipeRegistry.GetRecipesByCatalyst(Main.Registry.CubeRegistry.Get("furnace_t1") as CubeFurnace);

            Recipe foundRecipe = null;

            for (int i = 0; i < recipes.Count; i++)
            {
                Recipe recipe = recipes[i];

                if (recipe.Matches(inventory))
                {
                    if (foundRecipe == null || recipe.Weight > foundRecipe.Weight)
                        foundRecipe = recipe;
                }
            }

            return foundRecipe;
        }

        public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			SaveHelper.SaveInt32(saveBytes, (int)MeshingDataInstance.facing);
			inventory.Save(saveBytes);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;

			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			Position = TrackedPosition.InWorldSpace();

            //if (version == 2)
            MeshingDataInstance.facing = (MeshHelper.CubeFace)SaveHelper.LoadInt32(loadBytes, ref index);
			
			inventory.Load(loadBytes, ref index);
		}

        public unsafe Buffer<byte> GetMeshingData(BufferPool bufferPool)
        {
            bufferPool.Take(1, out Buffer<MeshingData> md);
            md.Memory->facing = MeshingDataInstance.facing;

            return md.As<byte>();
        }

        public Inventory GetInventory(int id)
        {
			return inventory;
        }

        public bool InventoryAction(Player? activatingPlayer, int action)
        {
            var currentRecipe = FindRecipe();

            if (currentRecipe != null && inventory.Get(2).num > 0)
                return CraftItem(activatingPlayer, currentRecipe);
            return false;
        }
    }
}
