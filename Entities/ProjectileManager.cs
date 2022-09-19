using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Entities
{
	public class ProjectileManager : IHitboxOwner
	{
		public struct ProjectileVisStats
		{
			public float scale;
			public RectangleF sourceRect;
			public Texture2D texture;

			public bool hasLight;
			public Vector4 lightColor;
			public Vector2 lightExtents;

			public ProjectileVisStats(Texture2D texture, RectangleF sourceRect, float scale)
			{
				this.texture = texture;
				this.sourceRect = sourceRect;
				this.scale = scale;

				this.hasLight = false;
				this.lightColor = Vector4.Zero;
				this.lightExtents = Vector2.Zero;
			}

			public ProjectileVisStats(Texture2D texture, RectangleF sourceRect, float scale, Vector4 lightColor, Vector2 lightExtents)
			{
				this.texture = texture;
				this.sourceRect = sourceRect;
				this.scale = scale;

				this.hasLight = true;
				this.lightColor = lightColor;
				this.lightExtents = lightExtents;
			}
		}

		public struct ProjectileStats
		{
			public IHitboxOwner owner;
			public int group;
			public int damage;
			public float collisionRadius;
			public float size;
			public bool gravity;
			public bool dieOnCollision;

			public ProjectileStats(IHitboxOwner owner, int group, int damage, float collisionRadius, float size, bool gravity, bool dieOnCollision)
			{
				this.owner = owner;
				this.group = group;
				this.damage = damage;
				this.collisionRadius = collisionRadius;
				this.size = size;
				this.gravity = gravity;
				this.dieOnCollision = dieOnCollision;
			}
		}

		public struct Projectile
		{
			public Vector3 position;
			public Vector3 velocity;
			public float timeLeft;

			public ProjectileVisStats visStats;
			public ProjectileStats stats;

			public readonly bool active;

			public Rectangle3D bounds;
			public int hitbox;
			public int light;

			public Projectile(Vector3 position, Vector3 velocity, float timeLeft, ProjectileVisStats visStats, ProjectileStats stats)
			{
				this.position = position;
				this.velocity = velocity;
				this.timeLeft = timeLeft;
				this.visStats = visStats;
				this.stats = stats;

				active = true;

				bounds = new Rectangle3D();
				hitbox = -1;
				light = -1;
			}
		}

		private Projectile[] projectiles = new Projectile[1024];

		private SimpleMesh<VertexCube, int> mesh;

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

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, -1)));

			mesh = new SimpleMesh<VertexCube, int>(device, vertices, indices);
		}

		public void Update(World world, double deltaTime)
		{
			for (int i = 0; i < 1024; i++)
			{
				if (!projectiles[i].active)
					continue;

				if (projectiles[i].hitbox == -1)
				{
					projectiles[i].hitbox = world.HitboxManager.Add(projectiles[i].stats.owner, projectiles[i].bounds.Offset(projectiles[i].position), projectiles[i].velocity, projectiles[i].stats.group, projectiles[i].stats.damage, 1f);
				}

				if (projectiles[i].visStats.hasLight && projectiles[i].light == -1)
                {
					projectiles[i].light = world.LightManager.Add(projectiles[i].position, 
						projectiles[i].visStats.lightExtents.X, 
						projectiles[i].visStats.lightExtents.Y, 
						new Color(projectiles[i].visStats.lightColor));
                }

				projectiles[i].timeLeft -= (float)deltaTime;

				if (projectiles[i].timeLeft <= 0)
				{
					Kill(world, i);
				}

				if (projectiles[i].stats.gravity)
				{
					projectiles[i].velocity.Y += World.GRAVITY;

					if (projectiles[i].velocity.Y < -340)
						projectiles[i].velocity.Y = -340;
				}

				projectiles[i].position += projectiles[i].velocity * (float)deltaTime;

				if (projectiles[i].hitbox != -1)
					world.HitboxManager.Update(projectiles[i].hitbox, projectiles[i].bounds.Offset(projectiles[i].position));
				
				if (projectiles[i].light != -1)
					world.LightManager.Update(projectiles[i].light, projectiles[i].position, 
						projectiles[i].visStats.lightExtents.X, projectiles[i].visStats.lightExtents.Y, 
						new Color(projectiles[i].visStats.lightColor));

				for (int x = -1; x <= 1; x++)
				{
					for (int y = -1; y <= 1; y++)
					{
						for (int z = -1; z <= 1; z++)
						{
							CubePosition pos = CubePosition.FromWorldSpace(projectiles[i].position) + 
								new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);

							if (world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetRaw(pos) != 0)
							{
								if (CollisionHelper.CheckCollision(CubePosition.BoundsWorldSpace(pos), projectiles[i].position, 
									projectiles[i].stats.collisionRadius, out Vector3 change))
								{
									if (projectiles[i].stats.dieOnCollision && change.Length() > 0)
									{
										Kill(world, i);
									}	
								}
							}
						}
					}
				}
			}
		}

		private void Kill(World world, int index)
        {
			if (projectiles[index].hitbox != -1)
				world.HitboxManager.Remove(projectiles[index].hitbox);

			if (projectiles[index].light != -1)
				world.LightManager.Remove(projectiles[index].light);

			projectiles[index] = new Projectile();
		}

		public void Draw(GraphicsDevice device, Effect effect)
		{
			for (int i = 0; i < 1024; i++)
			{
				if (projectiles[i].active)
				{
					Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(projectiles[i].visStats.texture,
						DrawHelper.BlackPixel, projectiles[i].visStats.hasLight ? DrawHelper.WhitePixel : DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
						Matrix.CreateScale(projectiles[i].visStats.scale) *
						Matrix.CreateRotationX(-Main.camera.Rotation.X) *
						Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
						Matrix.CreateRotationZ(-Main.camera.Rotation.Z) *
						Matrix.CreateTranslation(projectiles[i].position), projectiles[i].visStats.sourceRect));
				}
			}
		}

		public int Add(Projectile projectile, Rectangle3D bounds)
		{
			for (int i = 0; i < 1024; i++)
			{
				if (!projectiles[i].active)
				{
					projectiles[i] = projectile;

					projectiles[i].bounds = bounds;
					return i;
				}
			}

			return -1;
		}

		public ref Projectile Get(int index)
		{
			return ref projectiles[index];
		}

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
		{
		}
	}
}
