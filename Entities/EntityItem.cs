using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Entities
{
	public class EntityItem : Entity
	{
		public Vector3 Velocity;
		public Vector3 MaxVelocity = new Vector3(64, 340, 64);
		
		public readonly ItemInstance Item;

		private float sineTimer;

		private Rectangle3D bounds = new Rectangle3D(-new Vector3(10), new Vector3(20));
		public Rectangle3D Bounds => bounds.Offset(Position);

		private float noPickupTimer;

		public bool CanBePickedUp => noPickupTimer <= 0;

		public EntityItem(Vector3 position, ItemInstance item)
		{
			this.Position = position;
			this.Item = item;

			noPickupTimer = 2;

			sineTimer = Main.random.NextFloat(0, 4.5f);
		}

		public override void Update(double deltaTime)
		{
			noPickupTimer -= (float)deltaTime;

			Velocity.Y += World.GRAVITY;

			if (Velocity.Y < -MaxVelocity.Y)
				Velocity.Y = -MaxVelocity.Y;

			Velocity.X *= 0.95f;
			Velocity.Z *= 0.95f;

			//Velocity = MoveInCollisionGrid(Velocity);

			Position += Velocity * (float)deltaTime;

			UpdateCollision(deltaTime);
			//const float height = Cube.CUBE_SCALE / 2f;
			//UpdateCollision(deltaTime);
			/*var result = world.Raycast(Position, Position - new Vector3(0, height, 0), (Vector3 pos) =>
			{
				return world.IsInWorldBounds(pos) && world.GetRaw(pos) != 0;
			});

			if (result.hasHit)
			{
				Position.Y = result.hit.Y + height;

				//Vector3 pos = CubePosition.FromWorldSpace(result.hit).InWorldSpace(null);
				//Position.Y = pos.Y + Cube.CUBE_SCALE;
				Velocity.Y = 0;
				Velocity.X *= 0.45f;
				Velocity.Z *= 0.45f;
			}*/

			sineTimer += (float)deltaTime;
		}

		private void UpdateCollision(double deltaTime)
		{
			Rectangle3D ourBounds = Bounds;

			CubePosition ourBoundsNear = CubePosition.FromWorldSpace(ourBounds.Position);
			CubePosition ourBoundsFar = CubePosition.FromWorldSpace(ourBounds.FarPosition);

			for (int x = ourBoundsNear.X - 1; x <= ourBoundsFar.X; x++)
			{
				for (int y = ourBoundsFar.Y - 1; y <= ourBoundsFar.Y; y++)
				{
					for (int z = ourBoundsNear.Z - 1; z <= ourBoundsFar.Z; z++)
					{
						CubePosition pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace); //CubePosition.FromWorldSpace(Position);

						if (world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetRaw(pos) != 0)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							if (CollisionHelper.CheckCollision(cubeBounds, Position, 8, out Vector3 change))
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
					}
				}
			}

			//if (!anyCol)
				//Position += Velocity * (float)deltaTime;
		}

		public void MoveTowards(Vector3 position)
		{
			Vector3 dir = position - Position;
			dir.Normalize();
			dir *= 4;

			Velocity.X += dir.X;
			Velocity.Y += dir.Y * 4;
			Velocity.Z += dir.Z;
		}

		public override void Draw(GraphicsDevice device)
		{
			const float bobTime = 2f;
			const float spinTime = 4.5f;

			float bobPercent = (sineTimer % bobTime) / bobTime;
			bobPercent = (float)Math.Sin(MathHelper.Pi * 2 * bobPercent);

			float spinPercent = (sineTimer % spinTime) / spinTime;

			//device.RasterizerState = Main.noCullRS;

			Item.item.Draw(device, Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
				Matrix.CreateScale(0.5f) *
				Matrix.CreateRotationY(MathHelper.ToRadians(360 * spinPercent)) *
				Matrix.CreateTranslation(new Vector3(0, 5 * bobPercent, 0)) *
				//Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
				Matrix.CreateTranslation(Position));
		}
	}
}
