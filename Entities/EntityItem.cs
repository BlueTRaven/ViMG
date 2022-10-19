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
		public Vector3 MaxVelocity = new Vector3(10, 15, 10) * Cube.CUBE_SCALE;
		
		public readonly ItemInstance Item;

		private float sineTimer;

		private Rectangle3D bounds = new Rectangle3D(-new Vector3(Cube.CUBE_SCALE / 2f), new Vector3(Cube.CUBE_SCALE / 2f));
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

			Velocity.X *= 0.85f;
			Velocity.Z *= 0.85f;

			UpdateCollision(deltaTime);

			sineTimer += (float)deltaTime;
		}

		private void UpdateCollision(double deltaTime)
		{
			//if (world.GetChunkManager().GetCube(CubePosition.FromWorldSpace(Position + Velocity * (float)deltaTime)).GetOrDefault(Main.Registry.CubeRegistry.Air) == Main.Registry.CubeRegistry.Air)
			{
				Position += Velocity * (float)deltaTime;
			}

			Rectangle3D ourBounds = Bounds;

			CubePosition ourBoundsNear = CubePosition.FromWorldSpace(ourBounds.Position);
			CubePosition ourBoundsFar = CubePosition.FromWorldSpace(ourBounds.FarPosition);

			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					for (int z = -1; z <= 1; z++)
					{
						CubePosition pos = CubePosition.FromWorldSpace(Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace); //CubePosition.FromWorldSpace(Position);

						if (world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetRaw(pos) != 0)
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
					}
				}
				//if (!anyCol)
				//Position += Velocity * (float)deltaTime;
			}
		}

		public void MoveTowards(Vector3 position)
		{
			Vector3 dir = position - Position;
			dir.Normalize();
			dir *= Cube.CUBE_SCALE / 4f;

			Velocity.X += dir.X;
			Velocity.Y += dir.Y * 4;
			Velocity.Z += dir.Z;
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			const float bobTime = 2f;
			const float spinTime = 4.5f;

			float bobPercent = (sineTimer % bobTime) / bobTime;
			bobPercent = (float)Math.Sin(MathHelper.Pi * 2 * bobPercent);

			float spinPercent = (sineTimer % spinTime) / spinTime;

			//device.RasterizerState = Main.noCullRS;

			/*float widthScale = 1;
			float heightScale = 1;

			if (Item.item.SourceRect.width > Item.item.SourceRect.height)
            {
				widthScale = Item.item.SourceRect.width / Item.item.SourceRect.height;
            }
			else if (Item.item.SourceRect.height > Item.item.SourceRect.width)
            {
				heightScale = Item.item.SourceRect.height / Item.item.SourceRect.width;
            }

			Vector3 correctedScale = new Vector3(widthScale, heightScale, 1);*/

			Item.item.DrawInWorld(device, world, Item, Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 4f)) *
				Matrix.CreateRotationY(MathHelper.ToRadians(360 * spinPercent)) *
				Matrix.CreateTranslation(new Vector3(0, (Cube.CUBE_SCALE / 4f) * bobPercent, 0)) *
				//Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
				Matrix.CreateTranslation(Position));
		}
	}
}
