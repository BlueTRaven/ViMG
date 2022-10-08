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
		public struct ProjectileBatchStats
        {
			internal enum BatchingType
            {
				EvenSpacing,            //each projectile direction is spaced an even amount of degrees in local-space separately along pitch and yaw.
				RandomOffsetInRange,	//each projectile direction is randomly varied in local-space in a range saparately along pitch and yaw.
            }

			public Vector2 yawRandomOffsetRange;	//local-space yaw offset from original direction
			public Vector2 pitchRandomOffsetRange;  //local-space pitch offset from original direction

			public float spacingYaw;
			public float spacingPitch;

			public int num;

			internal BatchingType batchingType;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="num"></param>
			/// <param name="yawRange">yaw variance (in degrees) in local-space</param>
			/// <param name="pitchRange">pitch variance (in degrees) in local-space</param>
			public ProjectileBatchStats(int num, Vector2 yawRange, Vector2 pitchRange)
            {
				this.num = num;

                this.yawRandomOffsetRange = yawRange;
				this.pitchRandomOffsetRange = pitchRange;

				spacingYaw = 0;
				spacingPitch = 0;

				batchingType = BatchingType.RandomOffsetInRange;
            }

			public ProjectileBatchStats(int num, float spacingYaw, float spacingPitch)
            {
				this.num = num;

                this.yawRandomOffsetRange = Vector2.Zero;
				this.pitchRandomOffsetRange = Vector2.Zero;

				this.spacingYaw = spacingYaw;
				this.spacingPitch = spacingPitch;

				batchingType = BatchingType.EvenSpacing;
			}
		}

		public struct ProjectileVisStats
		{
			public float scale;
			public RectangleF sourceRect;
			public Texture2D texture;

			public bool hasLight;
			public Vector4 lightColor;
			public Vector2 lightExtents;

			public bool rollFollowsVelocity;

			public ProjectileVisStats(Texture2D texture, RectangleF sourceRect, float scale)
			{
				this.texture = texture;
				this.sourceRect = sourceRect;
				this.scale = scale;

				this.hasLight = false;
				this.lightColor = Vector4.Zero;
				this.lightExtents = Vector2.Zero;

				rollFollowsVelocity = false;
			}

			public ProjectileVisStats(Texture2D texture, RectangleF sourceRect, float scale, Vector4 lightColor, Vector2 lightExtents)
			{
				this.texture = texture;
				this.sourceRect = sourceRect;
				this.scale = scale;

				this.hasLight = true;
				this.lightColor = lightColor;
				this.lightExtents = lightExtents;

				rollFollowsVelocity = false;
			}
		}

		public struct ProjectileStats
		{
			public HitboxManager.Group group;
			public int damage;
			public float collisionRadius;
			public float size;
			public bool gravity;
            public float gravityScale;
			public bool dieOnCollision;

            public ProjectileStats(HitboxManager.Group group, int damage, float collisionRadius, float size, bool gravity = false, float gravityScale = 1, bool dieOnCollision = true)
			{
				this.group = group;
				this.damage = damage;
				this.collisionRadius = collisionRadius;
				this.size = size;
				this.gravity = gravity;
				this.gravityScale = gravityScale;
				this.dieOnCollision = dieOnCollision;
			}
		}

		public struct Projectile
		{
			public IHitboxOwner owner;

			public Vector3 position;
			public Vector3 velocity;
			public float timeLeft;

			public ProjectileVisStats visStats;
			public ProjectileStats stats;

			public readonly bool active;

			public Rectangle3D bounds;
			public int hitbox;
			public int light;

			public Projectile(IHitboxOwner owner, Vector3 position, Vector3 velocity, float timeLeft, ProjectileVisStats visStats, ProjectileStats stats)
			{
				this.owner = owner;
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
					projectiles[i].hitbox = world.HitboxManager.Add(projectiles[i].owner, projectiles[i].bounds.Offset(projectiles[i].position), projectiles[i].velocity, projectiles[i].stats.group, projectiles[i].stats.damage, 1f);
				}
                else
                {
					if (world.HitboxManager.Get(projectiles[i].hitbox).owner != projectiles[i].owner)
						throw new Exception("???????");
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
					projectiles[i].velocity.Y += World.GRAVITY * projectiles[i].stats.gravityScale;

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

							if (world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid)
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
					//float roll = Vector3.Dot(-Main.camera.Up, Vector3.Normalize(projectiles[i].velocity)) + MathHelper.ToRadians(180);
					float roll = 0;

					Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(projectiles[i].visStats.texture,
						DrawHelper.BlackPixel, projectiles[i].visStats.hasLight ? DrawHelper.WhitePixel : DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
						Matrix.CreateScale(projectiles[i].visStats.scale) *
						Matrix.CreateFromYawPitchRoll(-Main.camera.Rotation.Y, -Main.camera.Rotation.X, roll) *
						//Matrix.CreateRotationZ(roll) *
						Matrix.CreateTranslation(projectiles[i].position), projectiles[i].visStats.sourceRect));
				}
			}
		}

		public void AddBatch(IHitboxOwner owner, Vector3 position, Vector3 velocity, float timeLeft, 
			ProjectileBatchStats batchStats, ProjectileVisStats visStats, ProjectileStats stats, Rectangle3D bounds)
        {
			for (int i = 0; i < batchStats.num; i++)
			{
				Projectile projectile = new Projectile(owner, position, velocity, timeLeft, visStats, stats);
				Vector3 direction = Vector3.Normalize(projectile.velocity);
				float speed = projectile.velocity.Length();

				float yawOffset;
				float pitchOffset;

				if (batchStats.batchingType == ProjectileBatchStats.BatchingType.EvenSpacing)
                {
					yawOffset = batchStats.spacingYaw * i;
					pitchOffset = batchStats.spacingPitch * i;
                }
				else
                {
					yawOffset = Main.random.NextFloat(batchStats.yawRandomOffsetRange.X, batchStats.yawRandomOffsetRange.Y);
					pitchOffset = Main.random.NextFloat(batchStats.pitchRandomOffsetRange.X, batchStats.pitchRandomOffsetRange.Y);
				}

				Matrix offset = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(yawOffset), MathHelper.ToRadians(pitchOffset), 0);

				projectile.velocity = Vector3.Transform(direction, offset) * speed;

				Add(projectile, bounds);
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
