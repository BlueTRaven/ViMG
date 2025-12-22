using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(1, 0)]
    public class Sapling : Entity, ICubeTracker, ISyncBasicState
    {
		private static VerySimpleMesh mesh;

		private float startTime;
		private float toGrowTime;

        public CubePosition TrackedPosition => CubePosition.FromWorldSpace(Position);
		private bool grown;

		public Sapling()
        {
            DoesSync = false;
            DoesMajorSync = false;
        }

        public Sapling(CubePosition position)
        {
            DoesSync = false;
            DoesMajorSync = false;

            if (position.Y == 0)
				throw new Exception();

			this.Position = position.InWorldSpace();
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			startTime = world.GetTime();
            //any amount of time between three minutes and an hour, in intervals of a minute.
            toGrowTime = world.GetTime() + Main.random.Next(3, 60) * 60;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			if (world.GetTime() > toGrowTime && !grown)
            {
                world.EntityManager.Kill(this);   

				int num = Main.random.Next(3, 12);

				Tree tree = new Tree(TrackedPosition.InWorldSpace() + new Vector3(Cube.CUBE_SCALE * 0.5f, 0, Cube.CUBE_SCALE * 0.5f),
					num, TrackedPosition);
                //tree must be delayed otherwise we'll have both the tree and sapling occupying the same space
				world.EntityManager.Add(tree, true);

				grown = true;
			}
        }

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
		{
			world.EntityManager.Kill(this);
		}

  //      public override void Draw(GraphicsDevice device, Effect effect)
  //      {
  //          base.Draw(device, effect);

		//	if (mesh.IBO == null)
		//		MakeMesh(device);

		//	Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(
		//		StaticMaterials.Cubes, mesh,
		//		Matrix.CreateTranslation(Position + new Vector3(Cube.CUBE_SCALE / 2f, 0, Cube.CUBE_SCALE / 2f)), GetSourceRect()));
		//}

		public RectangleF GetSourceRect()
        {
			float percent = (world.GetTime() - startTime) / (toGrowTime - startTime);
			int i = (int)(3f * percent);

			return new RectangleF(16 * i, 80, 16, 16);
        }

        private void MakeMesh(GraphicsDevice device)
        {
            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

			DrawHelper3D.MakeXMeshRaw(vertices, indices, Vector3.Zero, Vector3.One, new RectangleF(0, 0, 1, 1));

            mesh = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));
			//mesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
		}

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
            SaveHelper.SaveFloat32(saveBytes, toGrowTime);
            SaveHelper.SaveFloat32(saveBytes, startTime);

            if (Position.Y == 0)
                throw new Exception();
        }

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
            Position = SaveHelper.LoadCubePosition(loadBytes, ref index).InWorldSpace();

            toGrowTime = SaveHelper.LoadFloat32(loadBytes, ref index);
            if (version == 0)
            {
                _ = SaveHelper.LoadFloat32(loadBytes, ref index);
                startTime = 0;
            }
            else
            {
                startTime = SaveHelper.LoadFloat32(loadBytes, ref index);
            }

            if (Position.Y == 0)
                throw new Exception();
        }

        public void Get(out BasicState state)
        {
            state = new BasicState
            {
                position = Position,
                timers = { [0] = toGrowTime, [1] = startTime },
            };
        }

        public void Set(ref readonly BasicState state)
        {
            Position = state.position;
            toGrowTime = state.timers[0];
            startTime = state.timers[1];
        }
    }
}
