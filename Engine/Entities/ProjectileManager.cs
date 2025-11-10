using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Buffs;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    public class ProjectileManager : IHitboxOwner
	{
		public struct ProjectileBatchStats
        {
			internal enum BatchingType
            {
				ManualSpacing,			//each projectile direction is spaced with a manually provided array of numbers.
				EvenSpacing,            //each projectile direction is spaced an even amount of degrees in local-space separately along pitch and yaw.
				RandomOffsetInRange,	//each projectile direction is randomly varied in local-space in a range saparately along pitch and yaw.
            }

			public Vector2 yawRandomOffsetRange;	//local-space yaw offset from original direction
			public Vector2 pitchRandomOffsetRange;  //local-space pitch offset from original direction

			public float spacingYaw;
			public float spacingPitch;

			public float[] manualSpacingYaw;
			public float[] manualSpacingPitch;

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

				manualSpacingYaw = null;
				manualSpacingPitch = null;

				batchingType = BatchingType.RandomOffsetInRange;
            }

			public ProjectileBatchStats(int num, float spacingYaw, float spacingPitch)
            {
				this.num = num;

                this.yawRandomOffsetRange = Vector2.Zero;
				this.pitchRandomOffsetRange = Vector2.Zero;

				this.spacingYaw = spacingYaw;
				this.spacingPitch = spacingPitch;

				manualSpacingYaw = null;
				manualSpacingPitch = null;

				batchingType = BatchingType.EvenSpacing;
			}

			/// <summary>
			/// Creates a batch using the manual spacing method. Note that the number of elements in the provided arrays must be identical to num.
			/// Remember pre-allocate and cache your arrays!
			/// </summary>
			/// <param name="num">The number of projectiles in the batch.</param>
			/// <param name="manualSpacingYaw">The array determining spacing in yaw. May be null (in which case 0 will be assumed).</param>
			/// <param name="manualSpacingPitch">The array determining spacing in pitch. May be null (in which case 0 will be assumed).</param>
			public ProjectileBatchStats(int num, float[] manualSpacingYaw, float[] manualSpacingPitch)
            {
				this.num = num;

				this.yawRandomOffsetRange = Vector2.Zero;
				this.pitchRandomOffsetRange = Vector2.Zero;

				this.spacingYaw = 0;
				this.spacingPitch = 0;

				this.manualSpacingYaw = manualSpacingYaw;
				this.manualSpacingPitch = manualSpacingPitch;

				batchingType = BatchingType.ManualSpacing;
			}
		}

		public struct ProjectileVisStats
		{
			public float scale;
			public RectangleF sourceRect;
			//Note: we're removing the option to add a texture in visual stats.
			//That means each projectile MUST use the projectile texture.
			//This is done for instancing's sake. If there is the possibility of having more than one texture, it kills the ability to instance
			//and makes things much more complicated to work with. So we're just going to disallow this.

			public bool hasLight;
			public Vector4 lightColor;
			public Vector2 lightExtents;

			public bool rollFollowsVelocity;

			public ProjectileVisStats(RectangleF sourceRect, float scale)
			{
				this.sourceRect = sourceRect;
				this.scale = scale;

				this.hasLight = false;
				this.lightColor = Vector4.Zero;
				this.lightExtents = Vector2.Zero;

				rollFollowsVelocity = false;
			}

			public ProjectileVisStats(RectangleF sourceRect, float scale, Vector4 lightColor, Vector2 lightExtents)
			{
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
			public float knockback;
			public float collisionRadius;
			public float size;
			public bool gravity;
            public float gravityScale;
			public bool dieOnCollision;
			public int pierce;
			public Buff.BuffInstance[] applyBuffs;
			public IProjectileEffects effects;

            public ProjectileStats(HitboxManager.Group group, int damage, float knockback, float collisionRadius, float size, int pierce = 1, bool gravity = false, float gravityScale = 1, bool dieOnCollision = true, Buff.BuffInstance[] applyBuffs = null, IProjectileEffects effects = null)
			{
				this.group = group;
				this.damage = damage;
				this.knockback = knockback;
				this.collisionRadius = collisionRadius;
				this.size = size;
				this.pierce = pierce;
				this.gravity = gravity;
				this.gravityScale = gravityScale;
				this.dieOnCollision = dieOnCollision;

				this.applyBuffs = applyBuffs ?? Array.Empty<Buff.BuffInstance>();
				this.effects = effects;
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

			public int inventorySlot;

			public int currentPierce;

			public int index;

			public Projectile(int index)
            {
				this.index = index;
				owner = null;
				position = Vector3.Zero;
				velocity = Vector3.Zero;
				timeLeft = 0;
				visStats = new ProjectileVisStats();
				stats = new ProjectileStats();

				bounds = new Rectangle3D();
				hitbox = -1;
				light = -1;
				inventorySlot = -1;

				currentPierce = -1;

				active = false;
			}

			public Projectile(IHitboxOwner owner, Vector3 position, Vector3 velocity, float timeLeft, ProjectileVisStats visStats, ProjectileStats stats, int inventorySlot = -1)
			{
				index = -1;
				this.owner = owner;
				this.position = position;
				this.velocity = velocity;
				this.timeLeft = timeLeft;
				this.visStats = visStats;
				this.stats = stats;

				bounds = new Rectangle3D();
				hitbox = -1;
				light = -1;

				this.inventorySlot = inventorySlot;
				currentPierce = stats.pierce;

				active = true;
			}
		}

		public const int PROJECTILES_MAX = 1024;
		private static VerySimpleMesh mesh;
		private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("projectiles");

		private Projectile[] projectiles = new Projectile[PROJECTILES_MAX];

		private World world;

		public ProjectileManager(World world)
		{
            this.world = world;
        }

		public void InitMeshes(GraphicsDevice device)
		{
            mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Center);
        }


		public void Update(double deltaTime)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            Span<CubePosition> positions = stackalloc CubePosition[3 * 3 * 3];
			Span<ushort> ids = stackalloc ushort[3 * 3 * 3];

			for (int i = 0; i < PROJECTILES_MAX; i++)
			{
				if (!projectiles[i].active)
					continue;

				if (projectiles[i].hitbox == -1)
				{
					projectiles[i].hitbox = world.HitboxManager.Add(projectiles[i].owner, projectiles[i].bounds.Offset(projectiles[i].position), 
						projectiles[i].velocity, projectiles[i].stats.group, projectiles[i].stats.damage, projectiles[i].stats.knockback, 
						applyBuffs: projectiles[i].stats.applyBuffs, manager: this, inventorySlot: projectiles[i].inventorySlot, data: i);
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
						projectiles[i].visStats.lightColor);
                }

				projectiles[i].timeLeft -= (float)deltaTime;

				if (projectiles[i].timeLeft <= 0)
				{
					Kill(i);
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
						projectiles[i].visStats.lightColor);

				int pi = 0;

				for (int x = -1; x <= 1; x++)
				{
					for (int y = -1; y <= 1; y++)
					{
						for (int z = -1; z <= 1; z++)
						{
							CubePosition pos = CubePosition.FromWorldSpace(projectiles[i].position) +
								new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);

							if (world.ChunkManager.IsInWorldBounds(pos))
							{
								positions[pi] = pos;
								pi++;
							}
						}
					}
				}

				world.ChunkManager.CubeView.GetIds(positions[..pi], ids[..pi]);

				for (int j = 0; j < 3 * 3 * 3; j++)
                {
					CubePosition pos = positions[j];
					ushort id = ids[j];

					if (Main.Registry.CubeRegistry.GetOrDefault(id, Main.Registry.CubeRegistry.Air).Solid)
                    {
						if (CollisionHelper.CheckCollision(CubePosition.BoundsWorldSpace(pos), projectiles[i].position,
															projectiles[i].stats.collisionRadius, out Vector3 change))
						{
							if (projectiles[i].stats.dieOnCollision && change.Length() > 0)
							{
								Kill(i);
							}
						}
					}
                }
			}
		}

		private void Kill(int index)
        {
			projectiles[index].stats.effects?.OnProjectileDeath(world, index);

			if (projectiles[index].hitbox != -1)
				world.HitboxManager.Remove(projectiles[index].hitbox);

			if (projectiles[index].light != -1)
				world.LightManager.Remove(projectiles[index].light);

			projectiles[index] = new Projectile(index);
		}

		public void Draw(GraphicsDevice device)
		{
			for (int i = 0; i < PROJECTILES_MAX; i++)
			{
				if (projectiles[i].active)
				{
					if (!projectiles[i].visStats.rollFollowsVelocity)
					{
						Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
							Matrix.CreateScale(projectiles[i].visStats.scale) *
							Matrix.CreateFromYawPitchRoll(-Main.camera.Rotation.Y, -Main.camera.Rotation.X, 0) *
							Matrix.CreateTranslation(projectiles[i].position), projectiles[i].visStats.sourceRect));
					}
					else
					{
                        Vector3 axis = projectiles[i].velocity;
                        axis.Normalize();

                        Matrix mat = Matrix.CreateConstrainedBillboard(projectiles[i].position, 
							Main.camera.Position, axis, -Main.camera.Forward, Vector3.Forward);

                        Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
                            Matrix.CreateScale(projectiles[i].visStats.scale) *
                            mat, projectiles[i].visStats.sourceRect));
                    }
				}
			}
		}

		public void AddBatch(IHitboxOwner owner, Vector3 position, Vector3 velocity, float timeLeft, 
			ProjectileBatchStats batchStats, ProjectileVisStats visStats, ProjectileStats stats, Rectangle3D bounds, int inventorySlot = -1)
        {
			for (int i = 0; i < batchStats.num; i++)
			{
				Projectile projectile = new Projectile(owner, position, velocity, timeLeft, visStats, stats, inventorySlot);
				Vector3 direction = Vector3.Normalize(projectile.velocity);
				float speed = projectile.velocity.Length();

				float yawOffset = 0;
				float pitchOffset = 0;

				if (batchStats.batchingType == ProjectileBatchStats.BatchingType.EvenSpacing)
                {
					yawOffset = batchStats.spacingYaw * i;
					pitchOffset = batchStats.spacingPitch * i;
                }
				else if (batchStats.batchingType == ProjectileBatchStats.BatchingType.RandomOffsetInRange)
                {
					yawOffset = Main.random.NextFloat(batchStats.yawRandomOffsetRange.X, batchStats.yawRandomOffsetRange.Y);
					pitchOffset = Main.random.NextFloat(batchStats.pitchRandomOffsetRange.X, batchStats.pitchRandomOffsetRange.Y);
				}
				else if (batchStats.batchingType == ProjectileBatchStats.BatchingType.ManualSpacing)
                {
					yawOffset = batchStats.manualSpacingYaw != null ? batchStats.manualSpacingYaw[i] : 0;
					pitchOffset = batchStats.manualSpacingPitch != null ? batchStats.manualSpacingPitch[i] : 0;
                }

				Matrix offset = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(yawOffset), MathHelper.ToRadians(pitchOffset), 0);

				projectile.velocity = Vector3.Transform(direction, offset) * speed;

				Add(projectile, bounds);
			}
        }

		public int Add(Projectile projectile, Rectangle3D bounds)
		{
			for (int i = 0; i < PROJECTILES_MAX; i++)
			{
				if (!projectiles[i].active)
				{
					projectiles[i] = projectile;
					projectiles[i].index = i;
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
			//the sources are not the same
			//i.e. player vs enemy or enemy vs player, but not player vs player or enemy vs enemy
			if (((int)us.group & HitboxManager.GROUP_SOURCE_MASK) != ((int)other.group & HitboxManager.GROUP_SOURCE_MASK) && 
				((int)other.group & HitboxManager.DAMAGE_TYPE_TAKE) > 0 && other.canInteract)
            {
				int index = us.data;
				if (projectiles[index].active)
				{
					projectiles[index].currentPierce--;

					if (projectiles[index].currentPierce <= 0)
					{
						Kill(index);
					}
				}
			}
		}
	}
}
