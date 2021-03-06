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

			bool anyCol = false;

			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					for (int z = -1; z <= 1; z++)
					{
						CubePosition pos = new CubePosition((int)(Position.X / 20) + x, (int)(Position.Y / 20) + y, (int)(Position.Z / 20) + z);

						if (world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetRaw(pos) != 0)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							if (ourBounds.Intersects(cubeBounds))
							{
								/*float xb = Math.Min(ourBounds.Left - cubeBounds.Right, cubeBounds.Left - ourBounds.Right);

								float yb = Math.Min(ourBounds.Top - cubeBounds.Bottom, cubeBounds.Top - ourBounds.Bottom);

								float zb = Math.Min(ourBounds.Back - cubeBounds.Front, cubeBounds.Back - ourBounds.Front );

								float vx = Math.Abs(Velocity.X);
								float vy = Math.Abs(Velocity.Y);
								float vz = Math.Abs(Velocity.Z);

								if (xb > yb && xb > zb && vx > vy && vx > vz)
								{
									Position.X += xb;
								}
								else if (yb > xb && yb > zb && vy > vx && vy > vz)
								{
									Position.Y += yb;
								}
								else if (zb > xb && zb > yb && vz > vx && vz > vy)
								{
									Position.Z += zb;
								}

								ourBounds = Bounds;*/

								Position.Y = cubeBounds.Top + bounds.Size.Y / 2f;
								Velocity.Y = 0;

								anyCol = true;
							}
						}
					}
				}
			}

			//if (!anyCol)
				//Position += Velocity * (float)deltaTime;
		}

		//TODO implement
		public Vector3 MoveInCollisionGrid(Vector3 delta)
		{
			const float epsilon = 1E-5f;

			Vector3 d = delta;
			Vector3 position = Position;
			Vector3 start = Position;
			Rectangle3D startBounds = Bounds;
			bool hadCollision = false;
			while (d.X != 0 || d.Y != 0 || d.Z != 0)
			{
				if (d.X != 0)
				{
					float inc = MathF.Abs(d.X) < 1 ? d.X : MathF.Sign(d.X);
					position.X += inc;
					if (Check(startBounds, CubePosition.FromWorldSpace(position)))
					{
						hadCollision = true;
						float x;
						if (d.X > 0)
						{
							x = MathF.Ceiling(startBounds.FarPosition.X) - 1 - startBounds.Size.X - epsilon;
						}
						else
						{
							x = MathF.Floor(startBounds.Position.X) + 1 + epsilon;
						}
						d.X = 0;
						position.X = x;
					}
					else
					{
						d.X -= inc;
					}
				}
				if (d.Y != 0)
				{
					float inc = MathF.Abs(d.Y) < 1 ? d.Y : MathF.Sign(d.Y);
					position.Y += inc;
					if (Check(startBounds, CubePosition.FromWorldSpace(position)))
					{
						hadCollision = true;
						float y;
						if (d.Y > 0)
						{
							y = MathF.Ceiling(startBounds.FarPosition.Y) - 1 - startBounds.Size.Y - epsilon;
						}
						else
						{
							y = MathF.Floor(startBounds.Position.Y) + 1 + epsilon;
						}
						d.Y = 0;
						position.Y = y;
					}
					else
					{
						d.Y -= inc;
					}
				}
				if (d.Z != 0)
				{
					float inc = MathF.Abs(d.Z) < 1 ? d.Z : MathF.Sign(d.Z);
					position.Z += inc;
					if (Check(startBounds, CubePosition.FromWorldSpace(position)))
					{
						hadCollision = true;
						float z;
						if (d.Z > 0)
						{
							z = MathF.Ceiling(startBounds.FarPosition.Z) - 1 - startBounds.Size.Z;
						}
						else
						{
							z = MathF.Floor(startBounds.Position.Z) + 1;
						}
						d.Z = 0;
						position.Z = z;
					}
					else
					{
						d.Z -= inc;
					}
				}
			}
			if (hadCollision)
			{
				return position - start;
			}
			else
			{
				return delta;
			}
		}

		private bool Check(Rectangle3D entBounds, CubePosition position)
		{
			if (world.GetChunkManager().GetCubeInstance(position).cubeId > 0)
			{
				Rectangle3D bounds = CubePosition.BoundsWorldSpace(position);

				return entBounds.Intersects(bounds);
			}

			return false;
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
				Matrix.CreateScale(0.25f) *
				Matrix.CreateRotationY(MathHelper.ToRadians(360 * spinPercent)) *
				Matrix.CreateTranslation(new Vector3(0, 5 * bobPercent, 0)) *
				//Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
				Matrix.CreateTranslation(Position));
		}
	}
}
