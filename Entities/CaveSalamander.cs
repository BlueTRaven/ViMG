using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class CaveSalamander : Entity
    {
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

		public Vector3 MaxVelocity = new Vector3(1.6f * Cube.CUBE_SCALE);
		public Vector3 MaxVelocityFalling = new Vector3(1.6f * Cube.CUBE_SCALE, 17 * Cube.CUBE_SCALE, 1.6f * Cube.CUBE_SCALE);
		public Vector3 Velocity;
		private Vector3 moveDir;
		private bool onGround;
		private bool wasOnGround;

		private float alive;
		private float invulnTimer;

		private int hitbox;
		private NoticeHandler<Player> noticeHandler;

		private Vector3 gravityDir = new Vector3(0, 1, 0);

		private Vector3 target = Vector3.Zero;

		private float wanderTimer;

		public CaveSalamander()
		{
		}

		public CaveSalamander(Vector3 position)
        {
			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 12, false);

			this.Position = position;

			target = position + new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f),
				Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f),
				Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f));
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			alive += (float)deltaTime;

			Vector3 actualMaxVel = MaxVelocity;

			if (!onGround)
				actualMaxVel = MaxVelocityFalling;

			Velocity += gravityDir * World.GRAVITY;

			wanderTimer -= (float)deltaTime;
			invulnTimer -= (float)deltaTime;
			noticeHandler.Update(deltaTime);

			if (invulnTimer <= 0)
			{
				if (!noticeHandler.Noticed)
				{
					if (wanderTimer <= 0)
					{
						target = Position + new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f),
							Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f),
							Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f));

						wanderTimer = Main.random.NextFloat(0.65f, 2.5f);
					}
				}
				else target = world.player.Position;

				if (onGround)
				{
					Vector3 distance = (target - new Vector3(0, Cube.CUBE_SCALE, 0)) - Position;

					Vector3 dir = Vector3.Normalize(distance) * Cube.CUBE_SCALE;

					float stopDist = noticeHandler.Noticed ? Cube.CUBE_SCALE * 1.5f : Cube.CUBE_SCALE * 3.5f;

					if (distance.Length() > stopDist)
					{
						Vector3 tangent = Vector3.Normalize(Vector3.Cross(gravityDir, dir));

						Matrix mat = Matrix.CreateFromAxisAngle(gravityDir, MathHelper.ToRadians(-90));

						moveDir = Vector3.Transform(tangent, mat);
						Velocity += moveDir * (Cube.CUBE_SCALE / 4f);
					}
                    else
                    {
						Velocity *= 0.95f;
                    }

					if (Velocity.Length() > actualMaxVel.Length())
					{
						Velocity.Normalize();
						Velocity *= actualMaxVel.Length();
					}
				}
                else
                {
					Vector2 clampXY = new Vector2(actualMaxVel.X, actualMaxVel.Z);
					Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

					if (velXY.Length() > clampXY.Length())
					{
						velXY.Normalize();
						velXY *= clampXY.Length();
					}

					Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);

					if (Velocity.Y < -actualMaxVel.Y)
						Velocity.Y = -actualMaxVel.Y;
				}
			}

			Position += Velocity * (float)deltaTime;

			onGround = false;
			UpdateCollision();

			//If we were on ground last frame and not on this frame, then we're attempting to cross a corner (or other things, 
			//but this is the most likely scenario).
			if (!onGround && wasOnGround)
			{
				//Position += moveDir * (Cube.CUBE_SCALE / 8f);
				//Position += gravityDir * (Cube.CUBE_SCALE / 4f);
			}

			if (!onGround)
			{
				gravityDir = Vector3.Up;
			}

			wasOnGround = onGround;
		}

		private void UpdateCollision()
		{
			const int checkSize = 1;

			for (int x = -checkSize; x <= checkSize; x++)
			{
				for (int y = -checkSize; y <= checkSize; y++)
				{
					for (int z = -checkSize; z <= checkSize; z++)
					{
						CubePosition pos = CubePosition.FromWorldSpace(Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace); //CubePosition.FromWorldSpace(Position);

						if (world.GetChunkManager().IsInWorldBounds(pos) && 
							world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							/*if (CollisionHelper.TestStaticAABBAABB(new Rectangle3D(Position - new Vector3(Cube.CUBE_SCALE * 0.25f), new Vector3(Cube.CUBE_SCALE * .5f)), 
								cubeBounds, out CollisionHelper.Contact contact))
                            {
								Position += contact.Normal * contact.Penetration;

								float dot = Vector3.Dot(contact.Normal, gravityDir);

								if (dot < 0.25f)
                                {
									onGround = true;

									//float velDot = Vector3.Dot(Velocity, gravityDir);
									//Velocity += gravityDir * velDot;    //negate ground-facing axis?

									gravityDir = -contact.Normal;
								}

								if (dot >= 0.95f)
                                {
									onGround = true;

									//float velDot = Vector3.Dot(Velocity, gravityDir);
									//Velocity += gravityDir * velDot;    //negate ground-facing axis?
								}
                            }*/

							Vector3 offset = gravityDir * Cube.CUBE_SCALE * 0.25f;
							Vector3 checkPos = Position + offset;

							if (CollisionHelper.CheckCollision(cubeBounds, checkPos, Cube.CUBE_SCALE * 0.25f, out Vector3 change))
							{
								Vector3 changeDir = Vector3.Normalize(change);
								Position = (checkPos - offset) + change;

								float dot = Vector3.Dot(changeDir, gravityDir);

								if (dot < 0.25f)
								{
									onGround = true;

									float velDot = Vector3.Dot(Velocity, gravityDir);
									Velocity += gravityDir * velDot;    //negate ground-facing axis?

									gravityDir = -changeDir;
								}

								if (dot >= 0.95f)
								{
									onGround = true;

									//float velDot = Vector3.Dot(Velocity, gravityDir);
									//Velocity += gravityDir * velDot;    //negate ground-facing axis?
								}
							}
						}
					}
				}
			}
		}

		public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
                mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("salamander"),
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(Position), new RectangleF(0, 0, 16, 16)));

            DrawHelper3D.DrawHealthbar(device, 4, 4, Position);
        }
    }
}
