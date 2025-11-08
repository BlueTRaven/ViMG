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
		
		public readonly ItemInstance ItemInstance;

		private Rectangle3D bounds = new Rectangle3D(-new Vector3(Cube.CUBE_SCALE / 2f), new Vector3(Cube.CUBE_SCALE / 2f));
		public Rectangle3D Bounds => bounds.Offset(Position);

		private float noPickupTimer;

		public bool CanBePickedUp => noPickupTimer <= 0;

		private Box box;
		private TypedIndex physicsShapeIndex;
		public BodyHandle physicsHandle;

		public EntityItem(Vector3 position, Vector3 initialVelocity, ItemInstance item)
		{
			this.Position = position;
            InitialVelocity = initialVelocity;
            this.ItemInstance = item;

			noPickupTimer = 1;
		}

        public override void Initialize(World world)
        {
            base.Initialize(world);

			if (ItemInstance.item is ItemCube)
				box = new Box(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE / 2f);
			else box = new Box(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE / 8f);

			const float scale = MathF.PI;
			Vector3 initialAngular = new Vector3(Main.random.NextFloat(-scale, scale), Main.random.NextFloat(-scale, scale),
				Main.random.NextFloat(-scale, scale));

			physicsShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(box);
			physicsHandle = world.PhysicsInfo.Simulation.Bodies.Add(
				BodyDescription.CreateDynamic(new RigidPose(Position.ToNumerics()), new BodyVelocity(InitialVelocity.ToNumerics(), initialAngular.ToNumerics()), 
				box.ComputeInertia(1), physicsShapeIndex, 0.001f));

			world.PhysicsInfo.Properties[physicsHandle] = new PhysicsProperties(new SubgroupCollisionFilter(FilterGroups.GROUP_ITEM));
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

            if (world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(Position)).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid)
                velocity.Y -= PhysicsInfo.SIM_GRAVITY * (float)deltaTime * 4f;

            world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = velocity.ToNumerics();

			Vector3 origin = new Vector3(Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 16f);


			if (ItemInstance.item is ItemCube)
				origin.Z = Cube.CUBE_SCALE / 4f;

			Position = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position + origin;
		}

		public void MoveTowards(Vector3 position)
		{
			world.PhysicsInfo.Simulation.Awakener.AwakenBody(physicsHandle);

			Vector3 dir = position - Position;
			dir.Normalize();
			dir *= Cube.CUBE_SCALE / 4f;

			Vector3 velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear;

			velocity.X += dir.X;
			velocity.Y += dir.Y * 4;
			velocity.Z += dir.Z;

			world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = velocity.ToNumerics();
		}

		//public override void Draw(GraphicsDevice device, Effect effect)
		//{
		//	Vector3 origin = new Vector3(Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 16f);

		//	if (Item.item is ItemCube)
		//		origin.Z = Cube.CUBE_SCALE / 4f;

		//	var reference = world.PhysicsInfo.Simulation.Bodies[physicsHandle];
		//	Item.item.DrawInWorld(device, world, Item,
		//		Matrix.CreateTranslation(-origin) *
		//		Matrix.CreateFromQuaternion(new Quaternion(reference.Pose.Orientation.X, reference.Pose.Orientation.Y, reference.Pose.Orientation.Z, reference.Pose.Orientation.W)) *
		//		Matrix.CreateTranslation(reference.Pose.Position)
		//		);
		//}
	}
}
