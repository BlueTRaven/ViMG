using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(1, 0)]
	public class Tree : Entity, IMultiCubeTracker, ISyncBasicState
	{
		private int baseSize;
		private int size;

		public int Size => size;
		public int MaxSize => baseSize;

		private CubePosition[] trackedPositions;
        public IEnumerable<CubePosition> TrackedPositions => trackedPositions;

		private Rectangle3D bounds;

		public bool NeedsRerender = true;

		private static CubeTree cube;

		public Tree()
		{
		}

		public Tree(Vector3 position, int size, CubePosition basePosition)
		{
            AlwaysRender = true;

			this.Position = position;
			this.baseSize = size;
			this.size = baseSize;

            trackedPositions = new CubePosition[size];
			trackedPositions[0] = basePosition;

            bounds = new Rectangle3D(basePosition.InWorldSpace(), new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * (size + 4), Cube.CUBE_SCALE));
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            if (cube == null)
                cube = Main.Registry.CubeRegistry.Get("tree") as CubeTree;

            for (int i = 1; i < size; i++)
            {
                var posOffset = trackedPositions[0] + new CubePosition(0, i, 0);

                world.ChunkManager.CubeView.SetCube(posOffset, cube.Id);

                trackedPositions[i] = posOffset;
            }
        }

        public override void Update(double deltaTime)
		{
			base.Update(deltaTime);
		}

        public bool OnInteract(Player player)
        {
			return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager cm, Player? playerWhoInitiated, CubePosition position, ushort updatedId, double updatedTime)
        {
			if (updatedTime >= TimeInitialized && updatedId != cube.Id)
			{
				if (position == trackedPositions[0])
				{
					world.EntityManager.Kill(this);

                    for (int i = position.Y; i < position.Y + size; i++)
                    {
                        world.TryMineCube(playerWhoInitiated, new CubePosition(position.X, i, position.Z), 0, 0, true);
                    }

					size = 0;
                    return;
				}
				else
				{
					for (int i = position.Y + 1; i < trackedPositions[0].Y + size; i++)
					{
						world.TryMineCube(playerWhoInitiated, new CubePosition(position.X, i, position.Z), 0, 0, true);
					}

					size = position.Y - trackedPositions[0].Y;

					CubePosition[] oldTracked = trackedPositions;

					trackedPositions = new CubePosition[size];
					trackedPositions[0] = oldTracked[0];
                    for (int i = 1; i < size; i++)
                        trackedPositions[i] = trackedPositions[0] + new CubePosition(0, i, 0);

					world.EntityManager.UpdateTrackedPositions(this, oldTracked);

					NeedsRerender = true;
				}
			}
        }

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveInt32(saveBytes, baseSize);
			SaveHelper.SaveInt32(saveBytes, size);

			for (int i = 0; i < size; i++)
				SaveHelper.SaveCubePosition(saveBytes, trackedPositions[i]);
		}

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
			baseSize =  SaveHelper.LoadInt32(loadBytes, ref index);
			size = SaveHelper.LoadInt32(loadBytes, ref index);

			if (version == 0)
			{
				//version 0 had weirdly generated sizes. They would have one too many tree blocks for its size.
				size++;
				baseSize++;

				CubePosition basePosition = SaveHelper.LoadCubePosition(loadBytes, ref index);

				Position = basePosition.InWorldSpace() + new Vector3(Cube.CUBE_SCALE * 0.5f, 0, Cube.CUBE_SCALE * 0.5f);
				bounds = new Rectangle3D(basePosition.InWorldSpace(), new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * (size + (Cube.CUBE_SCALE / 5)), Cube.CUBE_SCALE));

                trackedPositions = new CubePosition[size];

                for (int i = 0; i < size; i++)
                    trackedPositions[i] = basePosition + new CubePosition(0, i, 0);
            }
			else
			{
				trackedPositions = new CubePosition[size];
				for (int i = 0; i < size; i++)
					trackedPositions[i] = SaveHelper.LoadCubePosition(loadBytes, ref index);

                Position = trackedPositions[0].InWorldSpace() + new Vector3(Cube.CUBE_SCALE * 0.5f, 0, Cube.CUBE_SCALE * 0.5f);
                bounds = new Rectangle3D(trackedPositions[0].InWorldSpace(), new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE * (size + (Cube.CUBE_SCALE / 5)), Cube.CUBE_SCALE));
            }
		}

        public void Get(out BasicState state)
        {
			state = new BasicState
			{
				position = Position,
				counters = { [0] = size, [1] = baseSize },
			};
        }

        public void Set(ref readonly BasicState state)
        {
			Position = state.position;
			size = state.counters[0];
			baseSize = state.counters[1];
        }
    }
}
