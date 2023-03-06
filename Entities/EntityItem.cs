using BepuPhysics;
using BepuPhysics.Collidables;
using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;
using ViMG.Physics;

namespace ViMG.Entities
{
	public class EntityItem : Entity
	{
		//public Vector3 Velocity;
		public readonly Vector3 InitialVelocity;
		public Vector3 MaxVelocity = new Vector3(10, 15, 10) * Cube.CUBE_SCALE;
		
		public readonly ItemInstance Item;

		private float sineTimer;

		private Rectangle3D bounds = new Rectangle3D(-new Vector3(Cube.CUBE_SCALE / 2f), new Vector3(Cube.CUBE_SCALE / 2f));
		public Rectangle3D Bounds => bounds.Offset(Position);

		private float noPickupTimer;

		public bool CanBePickedUp => noPickupTimer <= 0;

		private Box box;
		private TypedIndex physicsShapeIndex;
		private BodyHandle physicsHandle;

		public EntityItem(Vector3 position, Vector3 initialVelocity, ItemInstance item)
		{
			this.Position = position;
            InitialVelocity = initialVelocity;
            this.Item = item;

			noPickupTimer = 1;

			sineTimer = Main.random.NextFloat(0, 4.5f);
		}

        public override void Initialize(World world)
        {
            base.Initialize(world);

			if (Item.item is ItemCube)
				box = new Box(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE / 2f);
			else box = new Box(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE / 8f);

			const float scale = MathF.PI;
			Vector3 initialAngular = new Vector3(Main.random.NextFloat(-scale, scale), Main.random.NextFloat(-scale, scale),
				Main.random.NextFloat(-scale, scale));

			physicsShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(box);
			physicsHandle = world.PhysicsInfo.Simulation.Bodies.Add(
				BodyDescription.CreateDynamic(new RigidPose(Position.ToNumerics()), new BodyVelocity(InitialVelocity.ToNumerics(), initialAngular.ToNumerics()), 
				box.ComputeInertia(1), physicsShapeIndex, 0.001f));

			world.PhysicsInfo.Properties[physicsHandle].Filter.GroupId = FilterGroups.GROUP_ITEM;
		}

        public override void OnUnload()
        {
            base.OnUnload();

			world.PhysicsInfo.Simulation.Shapes.Remove(physicsShapeIndex);
			world.PhysicsInfo.Simulation.Bodies.Remove(physicsHandle);
        }

        public override void Update(double deltaTime)
		{
			noPickupTimer -= (float)deltaTime;

			Vector3 velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear;

			velocity.X = Math.Clamp(velocity.X, -MaxVelocity.X, MaxVelocity.X);
			velocity.Z = Math.Clamp(velocity.Z, -MaxVelocity.Z, MaxVelocity.Z);

			world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = velocity.ToNumerics();

			//UpdateCollision(deltaTime);

			sineTimer += (float)deltaTime;

			Position = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position;
		}

		/*private void UpdateCollision(double deltaTime)
		{
			//if (world.ChunkManager2.GetCube(CubePosition.FromWorldSpace(Position + Velocity * (float)deltaTime)).GetOrDefault(Main.Registry.CubeRegistry.Air) == Main.Registry.CubeRegistry.Air)
			{
				Position += Velocity * (float)deltaTime;
			}

			Rectangle3D ourBounds = Bounds;

			CubePosition ourBoundsNear = CubePosition.FromWorldSpace(ourBounds.Position);
			CubePosition ourBoundsFar = CubePosition.FromWorldSpace(ourBounds.FarPosition);

			int pi = 0;
			Span<CubePosition> positions = stackalloc CubePosition[3 * 3 * 3];
			Span<ushort> ids = stackalloc ushort[3 * 3 * 3];

			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					for (int z = -1; z <= 1; z++)
					{
						positions[pi] = CubePosition.FromWorldSpace(Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);
						pi++;
					}
				}
			}

			world.ChunkManager.ThreadedView.GetIds(positions, ids, ThreadedCubeView.SafetyCheck.InWorldBounds);

			for (int i = 0; i < 3 * 3 * 3; i++)
			{
				CubePosition pos = positions[i];

				if (world.ChunkManager.IsInWorldBounds(pos) && Main.Registry.CubeRegistry.GetOrDefault(ids[i], Main.Registry.CubeRegistry.Air).Solid)
				{
					Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

					if (CollisionHelper.CheckCollision(cubeBounds, Position, 8f / 20f * Cube.CUBE_SCALE, out Vector3 change))
					{
						Position += change;

						if (change.Y != 0)
							Velocity.Y = 0;
						else if (change.X != 0)
							Velocity.X = 0;
						else if (change.Z != 0)
							Velocity.Z = 0;
					}
				}
				//if (!anyCol)
				//Position += Velocity * (float)deltaTime;
			}
		}*/

		public void MoveTowards(Vector3 position)
		{
			Vector3 dir = position - Position;
			dir.Normalize();
			dir *= Cube.CUBE_SCALE / 4f;

			Vector3 velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear;

			velocity.X += dir.X;
			velocity.Y += dir.Y * 4;
			velocity.Z += dir.Z;

			world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = velocity.ToNumerics();
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			var reference = world.PhysicsInfo.Simulation.Bodies[physicsHandle];
			Item.item.DrawInWorld(device, world, Item,
				Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 4f)) *
				Matrix.CreateFromQuaternion(new Quaternion(reference.Pose.Orientation.X, reference.Pose.Orientation.Y, reference.Pose.Orientation.Z, reference.Pose.Orientation.W)) *
				Matrix.CreateTranslation(reference.Pose.Position)
				);
		}
	}
}
