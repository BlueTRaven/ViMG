using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(0, 0)]
	public class GlowNode : Entity, ICubeTracker, ISyncBasicState
	{
		private float radius;
		private float fade;
		private Vector4 color;

		public CubePosition TrackedPosition { get; private set; }

		public GlowNode()
        {

        }

		public GlowNode(CubePosition position, float radius, float fade, Vector4 color)
		{
			TrackedPosition = position;
			this.Position = position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f);
			this.radius = radius;
			this.fade = fade;
			this.color = color;
		}

        public override void Initialize(World world)
        {
            base.Initialize(world);

			DestroyOnDisabled = false;	//Don't destroy glow node upon becoming inactive. Otherwise we orphan the cube.
        }

        public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			BoundingSphere sphere = new BoundingSphere(Position, radius);

			world.LightManager2.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
			{
				position = Position,
                min = radius - fade,
                max = radius,
                color = color,
            });
		}

		public override void OnUnload()
		{
			base.OnUnload();
		}

		public bool OnInteract(Player player)
		{
			//world.MineCube(TrackedPosition, true);
			//TODO this had killtrackedentities false?
			world.ChunkManager.CubeView.SetCube(TrackedPosition, 0);
			world.EntityManager.Kill(this);
			
			List<ItemInstance> items = new List<ItemInstance>();
			Main.Registry.CubeRegistry.Get("glow_node").GetDrops(items);

			foreach (ItemInstance item in items)
			{
				EntityItem ent = new EntityItem(Position,
					new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 1.6f, 
						Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)),
					item);
				world.EntityManager.Add(ent);
			}

			return true;
		}

		public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
		{
			//world.ChunkManager2.GetChunk(TrackedPosition).GetData().SetCube(TrackedPosition, 0, killTrackedEntities: false);
			world.EntityManager.Kill(this);
		}

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			SaveHelper.SaveVector4(saveBytes, color);
			SaveHelper.SaveFloat32(saveBytes, radius);
			SaveHelper.SaveFloat32(saveBytes, fade);
        }
        
		public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			color = SaveHelper.LoadVector4(loadBytes, ref index);
			radius = SaveHelper.LoadFloat32(loadBytes, ref index);
			fade = SaveHelper.LoadFloat32(loadBytes, ref index);

			this.Position = TrackedPosition.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f);
		}

        public void Get(out BasicState state)
        {
			state = new BasicState
			{
				position = Position,
				velocity = new Vector3(color.X, color.Y, color.Z),
				timers = { [0] = radius, [1] = fade, [2] = color.W, },
			};
        }

        public void Set(ref readonly BasicState state)
        {
            throw new NotImplementedException();
        }
    }
}
