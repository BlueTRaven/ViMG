using BepuUtilities.Memory;
using Engine.Items;
using Engine.Networking;
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
	public class EntityFurnace : Entity, ICubeTracker, IHasInventory, ISyncBasicState
	{
        private static MenuHelper.IWhiteList?[] whitelists = [null, null, new MenuHelper.WhiteListOneName("glowdust"), null, null];

        public struct MeshingData
		{
            public MeshHelper.CubeFace facing;
        }

		public CubePosition TrackedPosition 
		{
			get;
			private set; 
		}

		private InventoryManager.InventoryReference inventory;
		public MeshingData MeshingDataInstance;

		private float craftTimer;

		public EntityFurnace()
		{
        }

		public EntityFurnace(CubePosition position, MeshHelper.CubeFace facing)
		{
            this.TrackedPosition = position;
			MeshingDataInstance = new MeshingData()
			{
				facing = facing
			};

			Position = position.InWorldSpace();
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

            world.InventoryManager.GetOrAdd(ref inventory, new Inventory.InventoryConfig(5, whitelists, null));

            Optional<Entity> tracker = world.EntityManager.GetEntityTrackingPosition(TrackedPosition);

            if (tracker.HasValue())
                world.EntityManager.Kill(this);

            world.ChunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(TrackedPosition));//, true);
		}

        public override void OnUnload()
        {
            base.OnUnload();

            world.InventoryManager.Unload(inventory);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            var inventory = world.InventoryManager.Get(this.inventory);
            inventory.ProcessEventsServer(this);

			craftTimer -= (float)deltaTime;

            if (craftTimer > 0)
            {
                world.LightManager2.Add(new Engine.Common.LightManager2.LightConfig
                {
                    position = Position, 
                    min = Cube.CUBE_SCALE * 4,
                    max = Cube.CUBE_SCALE * 8, 
                    color = Color.OrangeRed.ToVector4(),
                });
            }
        }

        public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
		{
			world.EntityManager.Kill(this);
		}

		public bool OnInteract(Player player)
		{
			return false;
		}

        private bool CraftItem(Player? activatingPlayer, Recipe recipe)
        {
			bool activated = false;

            var inventory = world.InventoryManager.Get(this.inventory);
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
                    var playerInventory = world.InventoryManager.Get(activatingPlayer.inventory);
                    playerInventory.Add(recipe.Outputs[i]);
                }
            }

            // remove fuel
            inventory.Remove(2, 1);

			return activated;
        }

        public void OnCraft()
        {
			craftTimer = 3f;
        }

        public Recipe FindRecipe()
        {
            var inventory = world.InventoryManager.Get(this.inventory);

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
            
            var inventory = world.InventoryManager.Get(this.inventory);
            inventory.Save(saveBytes);
		}

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;

			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			Position = TrackedPosition.InWorldSpace();

            //if (version == 2)
            MeshingDataInstance.facing = (MeshHelper.CubeFace)SaveHelper.LoadInt32(loadBytes, ref index);

            var inventory = world.InventoryManager.GetOrAdd(ref this.inventory, new Inventory.InventoryConfig(5, whitelists, null));
            inventory.Load(loadBytes, ref index);
		}

        public unsafe Buffer<byte> GetMeshingData(BufferPool bufferPool)
        {
            bufferPool.Take(1, out Buffer<MeshingData> md);
            md.Memory->facing = MeshingDataInstance.facing;

            return md.As<byte>();
        }

        public bool InventoryAction(int activatingPlayer, int action)
        {
            var currentRecipe = FindRecipe();

            var inventory = world.InventoryManager.Get(this.inventory);
            var player = world.player[activatingPlayer];
            if (currentRecipe != null && inventory.Get(2).num > 0)
                return CraftItem(player, currentRecipe);
            return false;
        }

        public void Get(out BasicState state)
        {
            state = new BasicState
            {
                position = Position,
                state = (int)MeshingDataInstance.facing,

                timers = { [0] = craftTimer},
                counters = { [0] = inventory.id, [1] = inventory.generation },
            };
        }

        public void Set(ref readonly BasicState state)
        {
            throw new NotImplementedException();
        }
    }
}
