using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.UIs;

namespace ViMG.Entities
{
	[EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(2, 0)]
	public class EntityFurnace : Entity, ICubeTracker
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

		}

		public EntityFurnace(CubePosition position, MeshHelper.CubeFace facing)
		{
			this.TrackedPosition = position;
			MeshingDataInstance = new MeshingData()
			{
				facing = facing
			};

			Position = position.InWorldSpace();

			inventory = new Inventory(5);
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			Optional<Entity> tracker = world.EntityManager.GetEntityTrackingPosition(TrackedPosition);

			if (!tracker.HasValue() || tracker.Get() != this)
				world.EntityManager.Remove(this);

			world.ChunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(TrackedPosition));//, true);
		}

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			craftTimer -= (float)deltaTime;

			if (craftTimer <= 0 && light != -1)
            {
				world.LightManager.Remove(light);
				light = -1;
            }
        }

        public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
		{
			world.EntityManager.Remove(this);
		}

		public bool OnInteract(Player player)
		{
            Main.gameStateManager.GetCurrentGameState().PushMenu(new MenuFurnace(Main.gameStateManager, player, player.GetInventory(), inventory, this));

			return true;
		}

		public void OnCraft()
        {
			craftTimer = 3f;

			if (light == -1)
				light = world.LightManager.Add(Position, Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8, Color.OrangeRed.ToVector4());
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
			
			inventory = Inventory.Load(loadBytes, ref index);
		}

        public unsafe Buffer<byte> GetMeshingData(BufferPool bufferPool)
        {
            bufferPool.Take(1, out Buffer<MeshingData> md);
            md.Memory->facing = MeshingDataInstance.facing;

            return md.As<byte>();
        }
    }
}
