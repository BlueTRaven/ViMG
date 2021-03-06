using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG
{
	public class Slime
	{
		public const int GROUP_ENEMYHOSTILE_SOURCE = 1;
		private World world;
		public Vector3 Position;
		public Vector3 Velocity;

		public Vector3 MaxVelocity = new Vector3(64, 340, 64);

		private SimpleMesh<VertexPositionColor, int> meshDebugCube;
		private SimpleMesh<VertexPositionColorTextureNormal, int> mesh;
		private SimpleMesh<VertexPositionColorTextureNormal, int> meshHealthbar;

		private bool onGround;

		private Rectangle3D Bounds => new Rectangle3D(Position - new Vector3(Cube.CUBE_SCALE * 0.35f, Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE * 0.35f), 
			new Vector3(Cube.CUBE_SCALE * 0.70f));
		private int hitbox = -1;

		private float invulnTimer;

		private int health;
		private int maxHealth = 4;

		public bool Dead;

		public Slime(World world, Vector3 position)
		{
			this.world = world;
			this.Position = position;

			health = maxHealth;
		}

		public void Update(double deltaTime)
		{
			if (Dead)
				return;

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(Bounds, Vector3.Zero, GROUP_ENEMYHOSTILE_SOURCE, 1, 1f);
			else world.HitboxManager.Update(hitbox, Bounds);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			if (invulnTimer <= 0)
			{
				if (onGround)
				{
					if (Main.random.Next(0, 64) == 0)
					{
						Vector2 playerDir = Vector2.Normalize(new Vector2(world.player.Position.X, world.player.Position.Z) - new Vector2(Position.X, Position.Z));
						Velocity = new Vector3(playerDir.X * 32, MaxVelocity.Y * 0.75f, playerDir.Y * 32);
						onGround = false;
					}
				}

				Vector2 clampXY = new Vector2(actualMaxVel.X, actualMaxVel.Z);
				Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

				if (velXY.Length() > clampXY.Length())
				{
					velXY.Normalize();
					velXY *= clampXY.Length();
				}

				Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);

				UpdateDamage(deltaTime);
			}

			if (Velocity.Y < -actualMaxVel.Y)
				Velocity.Y = -actualMaxVel.Y;

			if (onGround)
			{
				Vector2 velocitySlowed = new Vector2(Velocity.X, Velocity.Z);
				if (velocitySlowed.Length() > 0)
				{
					velocitySlowed = Vector2.Normalize(velocitySlowed) * velocitySlowed.Length() * 0.85f;
				}

				Velocity = new Vector3(velocitySlowed.X, Velocity.Y, velocitySlowed.Y);
			}

			Position += Velocity * (float)deltaTime;

			onGround = false;
			UpdateCollision();

			invulnTimer -= (float)deltaTime;
		}

		private Vector2[] offsetsDown = new Vector2[4]
		{
			new Vector2(-0.325f) * Cube.CUBE_SCALE,
			new Vector2(-0.325f, 0.325f) * Cube.CUBE_SCALE,
			new Vector2(0.325f, -0.325f) * Cube.CUBE_SCALE,
			new Vector2(0.325f) * Cube.CUBE_SCALE
		};

		private Vector3[] directions = new Vector3[4]
		{
			new Vector3(-1, 0, 0),
			new Vector3(1, 0, 0),
			new Vector3(0, 0, -1),
			new Vector3(0, 0, 1)
		};

		private void UpdateDamage(double deltaTime)
		{
			DenseHitboxArray.Hitbox[] hitboxes = world.HitboxManager.GetAll();
			for (int i = 0; i < world.HitboxManager.Capacity; i++)
			{
				ref DenseHitboxArray.Hitbox hitbox = ref hitboxes[i];

				if (hitbox.active)
				{
					if (hitbox.group == Player.GROUP_PLAYER_SOURCE)
					{
						if (hitbox.bounds.Intersects(Bounds))
						{
							Vector3 direction = Vector3.Normalize(hitbox.direction);

							Velocity = new Vector3(direction.X * 64, 128, direction.Z * 64);

							health -= hitbox.damage;

							if (health <= 0)
							{
								Kill();
							}

							invulnTimer = 0.25f;
							break;	//break because another hitbox shouldn't be able to hit us anyway...
						}
					}
				}
			}
		}

		private void UpdateCollision()
		{
			const float height = Cube.CUBE_SCALE;
			for (int i = 0; i < 4; i++)
			{
				Vector3 startPos = new Vector3(Position.X + offsetsDown[i].X, Position.Y - height, Position.Z + offsetsDown[i].Y);
				Vector3 dir = new Vector3(0, height, 0);
				var resultDown = world.RaycastVector(startPos, dir, height, (Vector3 pos) =>
				{
					return world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetRaw(pos) != 0;
				});

				if (resultDown.hasHit)
				{
					CubePosition pos = CubePosition.FromWorldSpace(resultDown.hit);

					Position.Y = pos.Y * Cube.CUBE_SCALE + Cube.CUBE_SCALE + height;
					Velocity.Y = 0;
					onGround = true;
				}
			}

			const float sideWidth = 0.45f;

			for (int i = 0; i < 4; i++)
			{
				Vector3 startPos = new Vector3(Position.X, Position.Y - height + Cube.CUBE_SCALE * 0.5f, Position.Z);
				ref Vector3 dir = ref directions[i];
				var resultSideBot = world.RaycastVector(startPos, dir, Cube.CUBE_SCALE * sideWidth, (Vector3 pos) =>
				{
					return world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetRaw(pos) != 0;
				});

				if (resultSideBot.hasHit)
				{
					CubePosition pos = CubePosition.FromWorldSpace(resultSideBot.hit);

					Vector3 offset = resultSideBot.hit - directions[i] * Cube.CUBE_SCALE * sideWidth;

					Position = new Vector3(offset.X, Position.Y, offset.Z);
				}
			}
		}

		public void Kill()
		{
			EntityItem ent = new EntityItem(Position, new Items.ItemInstance(Main.Registry.ItemRegistry.Get("slime_chunk"), 1, 1));
			ent.Velocity = new Vector3(Main.random.NextFloat(-100, 100), 128, Main.random.NextFloat(-100, 100));
			world.EntityManager.Add(ent);
			world.HitboxManager.Remove(hitbox);
			hitbox = -1;
			Dead = true;
		}

		public void Draw(GraphicsDevice device)
		{
			if (Dead)
				return;

			if (mesh == null)
				MakeMeshes(device);

			mesh.Draw(device, Main.CubeEffect, 
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position));

			meshHealthbar.Draw(device, Main.CubeEffect, 
				Matrix.CreateScale(new Vector3((float)health / (float)maxHealth, 1, 1)) *
				Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 1.5f, 0)) *
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position));

			/*if (invulnTimer > 0)
				Main.BasicEffect.DiffuseColor = Color.Red.ToVector3();

			meshQuad.Draw(device, Main.BasicEffect, 
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) * 
				Matrix.CreateTranslation(Position));

			if (invulnTimer > 0)
				Main.BasicEffect.DiffuseColor = Color.White.ToVector3();*/
		}

		private void MakeMeshes(GraphicsDevice device)
		{
			meshDebugCube = MeshHelper.MakeCubeVertexPositionColor(device, -new Vector3(Cube.CUBE_SCALE / 2f, 0, Cube.CUBE_SCALE / 2f), 
				new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);

			Vector3 min = -new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE, 0);
			Vector3 max = new Vector3(Cube.CUBE_SCALE / 2f, 0, 0);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
			List<int> indices = new List<int>();

			Vector2 atx = new Vector2(0, 1);
			Vector2 btx = new Vector2(1, 1);
			Vector2 ctx = new Vector2(1, 0);
			Vector2 dtx = new Vector2(0, 0);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, -1)));

			mesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("slime"));

			min = Vector3.Zero;
			max = new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE / 4, Cube.CUBE_SCALE);

			a = new Vector3(max.X, min.Y, max.Z);
			b = new Vector3(min.X, min.Y, max.Z);
			c = new Vector3(min.X, max.Y, max.Z);
			d = new Vector3(max.X, max.Y, max.Z);

			vertices = new List<VertexPositionColorTextureNormal>();
			indices = new List<int>();

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(a, Color.Red, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(b, Color.Red, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.Red, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.Red, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(b, Color.Red, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(a, Color.Red, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.Red, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.Red, ctx, new Vector3(0, 0, -1)));

			meshHealthbar = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, DrawHelper.WhitePixel);
		}
	}
}
