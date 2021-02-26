using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Entities
{
	public class ProjectileManager
	{
		public struct Projectile
		{
			public Vector3 position;
			public Vector3 velocity;
			public float timeLeft;
			
			public float scale;
			public Texture2D texture;

			public int damage;
			public bool dieOnCollision;

			public readonly bool active;

			public Projectile(Vector3 position, Vector3 velocity, float timeLeft, float scale, Texture2D texture, int damage, bool dieOnCollision)
			{
				this.position = position;
				this.velocity = velocity;
				this.timeLeft = timeLeft;
				this.scale = scale;
				this.texture = texture;
				this.damage = damage;
				this.dieOnCollision = dieOnCollision;

				active = true;
			}
		}

		private Projectile[] projectiles = new Projectile[1024];

		private SimpleMesh<VertexPositionColorTextureNormal, int> mesh;

		public ProjectileManager(GraphicsDevice device)
		{
			Vector3 min = new Vector3(-0.5f, -0.5f, 0);
			Vector3 max = new Vector3(0.5f, 0.5f, 0);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			Vector2 atx = new Vector2(0, 1);
			Vector2 btx = new Vector2(1, 1);
			Vector2 ctx = new Vector2(1, 0);
			Vector2 dtx = new Vector2(0, 0);

			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
			List<int> indices = new List<int>();

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, -1)));

			mesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices);
		}

		public void Update(World world, double deltaTime)
		{
			for (int i = 0; i < 1024; i++)
			{
				if (!projectiles[i].active)
					continue;

				projectiles[i].timeLeft -= (float)deltaTime;

				if (projectiles[i].timeLeft <= 0)
					projectiles[i] = new Projectile();

				var result = world.Raycast(projectiles[i].position, projectiles[i].position + projectiles[i].velocity, (Vector3 pos) =>
				{
					return world.GetRaw(pos) != 0;
				});

				if (!result.hasHit)
					projectiles[i].position += projectiles[i].velocity;
				else
				{
					if (projectiles[i].dieOnCollision)
						projectiles[i] = new Projectile();
				}
			}
		}

		public void Draw(GraphicsDevice device, Effect effect)
		{
			for (int i = 0; i < 1024; i++)
			{
				if (projectiles[i].active)
				{
					mesh.Draw(device, effect,
						Matrix.CreateScale(projectiles[i].scale) *
						Matrix.CreateRotationX(-Main.camera.Rotation.X) *
						Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
						Matrix.CreateRotationZ(-Main.camera.Rotation.Z) *
						Matrix.CreateTranslation(projectiles[i].position), projectiles[i].texture);
				}
			}
		}

		public int Add(Projectile projectile)
		{
			for (int i = 0; i < 1024; i++)
			{
				if (!projectiles[i].active)
				{
					projectiles[i] = projectile;

					return i;
				}
			}

			return -1;
		}

		public ref Projectile Get(int index)
		{
			return ref projectiles[index];
		}
	}
}
