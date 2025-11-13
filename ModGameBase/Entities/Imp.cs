using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using ViMG.Rendering;

namespace ViMG.Entities
{
    public class Imp : Entity, IHitboxOwner
    {
		private static VerySimpleMesh mesh;

		private bool onGround;
		private bool shouldJump;

		private NoticeHandler<Player> noticeHandlerDay;
		private NoticeHandler<Player> noticeHandlerNight;

		public Vector3 MaxVelocity = new Vector3(3.2f * Cube.CUBE_SCALE, 17 * Cube.CUBE_SCALE, 3.2f * Cube.CUBE_SCALE);
		public Vector3 Velocity;

		private int health;
		private int maxHealth = 8;

		private float invulnTimer;
		private float alive;
		private float fireTimer;
		private const float FIRE_TIME = 2.25f;

		private float idleTimer;
		private float idleMoveTimer;
		private int idleMovements;
		private Vector2 idleDirection;
		private Vector2 idleHome;

		private Rectangle3D Bounds => new Rectangle3D(Position - new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
			new Vector3(Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.70f));
		private int hitbox = -1;
		private int light = -1;
		private bool isShadowmapped;

		public Imp()
        {
        }

        public Imp(Vector3 position)
        {
            this.Position = position;
        }

		public override void Initialize(World world)
		{
			base.Initialize(world);

			noticeHandlerNight = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
			noticeHandlerDay = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6, false, noticeFalloffTime: 5);

			health = maxHealth;
		}

		public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			alive += (float)deltaTime;

			NoticeHandler<Player> realNoticeHandler = noticeHandlerNight;

			if (!world.IsNight())
				realNoticeHandler = noticeHandlerDay;

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, 1, 1f, invulnTimer <= 0);
			else world.HitboxManager.Update(hitbox, Bounds, invulnTimer <= 0);

			float p0 = (alive % 0.65f) / 0.65f;
			//float p1 = ((alive + 0.3f) % 0.45f) / 0.45f;
			float s0 = MathF.Sin(MathF.PI * 2 * p0) * Cube.CUBE_SCALE * 1.25f;
			//float s1 = MathF.Sin(MathF.PI * 2 * p1) * Cube.CUBE_SCALE * 1.25f;

			BoundingSphere sphere = new BoundingSphere(Position, Cube.CUBE_SCALE * 8);

			LightHelper.UpdateLight(world.LightManager, new LightHelper.LightInfo(Position + new Vector3(Cube.CUBE_SCALE / 2f),
				Cube.CUBE_SCALE * 4f + s0, Cube.CUBE_SCALE * 8f, Color.OrangeRed.ToVector4()), Velocity.Length() > 0.0005f ? LightHelper.LightUpdateType.UpdateDirty : LightHelper.LightUpdateType.DontUpdate,
				sphere, ref light, ref isShadowmapped, true);

			/*if (light == -1)
				light = world.LightManager.Add(Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), Cube.CUBE_SCALE * 4 + s0, Cube.CUBE_SCALE * 8 + s1, Color.OrangeRed);
			else world.LightManager.Update(light, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), Cube.CUBE_SCALE * 4 + s0, Cube.CUBE_SCALE * 8 + s1, Color.OrangeRed);*/

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			realNoticeHandler.Update(deltaTime);

			invulnTimer -= (float)deltaTime;

			if (invulnTimer <= 0 && onGround)
			{
				if (shouldJump)
				{
					Velocity.Y = Cube.CUBE_SCALE * 10;
					shouldJump = false;
				}

				if (realNoticeHandler.Noticed)
				{
					Vector3 distance = (realNoticeHandler.GetNoticedEntity().Position - new Vector3(0, Cube.CUBE_SCALE, 0)) - Position;

					fireTimer -= (float)deltaTime;

					idleMovements = 0;

					Vector3 dir = Vector3.Normalize(distance) * Cube.CUBE_SCALE;

					if (distance.Length() > Cube.CUBE_SCALE * 9)
					{
						if (fireTimer <= 1)
							fireTimer = 1;
						
						Velocity.X += dir.X;
						Velocity.Z += dir.Z;
					}
					else if (distance.Length() < Cube.CUBE_SCALE * 4)
                    {
						if (fireTimer <= 1)
							fireTimer = 1;

						//Too close, travel away from the target
						Velocity.X -= dir.X;
						Velocity.Z -= dir.Z;
					}
					else
                    {
						if (fireTimer <= 0)
                        {
							ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(
								HitboxManager.Group.ENEMYHOSTILE_BOTH, 1, 1f, Cube.CUBE_SCALE / 4, Cube.CUBE_SCALE);
							ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(
								new RectangleF(32, 0, 16, 16), Cube.CUBE_SCALE, 
								Color.Red.ToVector4(), new Vector2(Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 4));

							world.ProjectileManager.Add(new ProjectileManager.Projectile(this, Position + new Vector3(0, Cube.CUBE_SCALE, 0), 
								Vector3.Normalize(distance) * Cube.CUBE_SCALE * 16, 
								8, visStats, stats), 
								new Rectangle3D(-new Vector3(Cube.CUBE_SCALE / 4), new Vector3(Cube.CUBE_SCALE / 2)));

							fireTimer = FIRE_TIME;
                        }

						Velocity.X *= 0.85f;
						Velocity.Z *= 0.85f;
                    }
				}
				else
				{
					idleTimer -= (float)deltaTime;

					if (idleTimer <= 0)
						idleMoveTimer -= (float)deltaTime;

					if (idleMovements == 0 && idleTimer <= 0 && idleMoveTimer <= 0)
					{
						idleHome = new Vector2(Position.X, Position.Z);

						idleTimer = Main.random.NextFloat(4f, 12f);
						idleMoveTimer = Main.random.NextFloat(0.25f, 2f);
						idleMovements = Main.random.Next(2, 6);

						idleDirection = Main.random.NextAngle();
					}
					else
					{
						float distFromIdleHome = (new Vector2(Position.X, Position.Z) - idleHome).Length();

						if (distFromIdleHome > Cube.CUBES_PER_UNIT * 16)
							idleDirection = -idleDirection;

						if (idleTimer <= 0 && idleMoveTimer <= 0)
						{
							idleMovements--;
							idleDirection = Main.random.NextAngle();
							idleMoveTimer = Main.random.NextFloat(0.25f, 2f);
						}
					}

					if (idleTimer <= 0)
					{
						Velocity.X += idleDirection.X;
						Velocity.Z += idleDirection.Y;
					}
					else
					{
						Velocity.X *= 0.85f;
						Velocity.Z *= 0.85f;
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
			}

			if (Velocity.Y < -actualMaxVel.Y)
				Velocity.Y = -actualMaxVel.Y;

			Position += Velocity * (float)deltaTime;

			shouldJump = false;
			onGround = false;
			UpdateCollision();

            if (world.player.All(x => x == null || (x.Position - Position).Length() > 128 * Cube.CUBE_SCALE))
                world.EntityManager.Remove(this);
		}

		public override void OnUnload()
		{
			base.OnUnload();

			if (light != -1)
			{
				if (!isShadowmapped)
					world.LightManager.Remove(light);
				else world.LightManager.RemoveShadowmapped(light);
			}

			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);
        }

        private void UpdateCollision()
		{
			const int checkSize = 1;

			int total = (int)Math.Pow(checkSize * 2 + 1, 3);
			int pi = 0;
			Span<CubePosition> positions = stackalloc CubePosition[total];
			Span<ushort> ids = stackalloc ushort[total];

			for (int x = -checkSize; x <= checkSize; x++)
			{
				for (int y = -checkSize; y <= checkSize; y++)
				{
					for (int z = -checkSize; z <= checkSize; z++)
					{
						CubePosition pos = CubePosition.FromWorldSpace(Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);

						if (world.ChunkManager.IsInWorldBounds(pos))
						{
							positions[pi] = pos;
							pi++;
						}
					}
				}
			}

			world.ChunkManager.CubeView.GetIds(positions[..pi], ids[..pi]);

			for (int i = 0; i < total; i++)
			{
				CubePosition pos = positions[i];
				ushort id = ids[i];

				if (Main.Registry.CubeRegistry.GetOrDefault(id, Main.Registry.CubeRegistry.Air).Solid)
				{
					Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

					Vector3 offset = new Vector3(0, Cube.CUBE_SCALE * 0.25f, 0);
					Vector3 checkPos = Position + offset;

					if (CollisionHelper.CheckCollision(cubeBounds, checkPos, Cube.CUBE_SCALE * 0.25f, out Vector3 change))
					{
						Position = (checkPos - offset) + change;

						if (change.Y > 0)
						{
							Velocity.Y = 0;
							onGround = true;
						}
						else if (change.Y < 0)
							Velocity.Y = 0;
						else if (change.X != 0)
							Velocity.X = 0;
						else if (change.Z != 0)
							Velocity.Z = 0;
					}
				}
			}

			if (onGround)
			{
				var ray = world.RaycastVector(Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(Velocity.X, 0, Velocity.Z), Cube.CUBE_SCALE * 2,
					(Vector3 pos) =>
					{
						Cube cube = world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air);

						return cube.Collision != Cube.CollisionValue.None;
					});

				if (ray.hasHit)
				{
					shouldJump = true;
				}
			}

			foreach (Imp imp in world.EntityManager.GetAll<Imp>())
			{
				if (imp != this)
				{
					Vector2 distXZ = new Vector2(Position.X, Position.Z) - new Vector2(imp.Position.X, imp.Position.Z);

					if (distXZ.Length() < Cube.CUBE_SCALE)
					{
						Vector2 correctPos = new Vector2(imp.Position.X, imp.Position.Z) + Vector2.Normalize(distXZ) * Cube.CUBE_SCALE;

						Position = new Vector3(correctPos.X, Position.Y, correctPos.Y);
						//skeleton.Position -= new Vector3(distXZ.X, 0, distXZ.Y);
					}
				}
			}
		}

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
		{
			if (invulnTimer <= 0)
			{
				if (other.group == HitboxManager.Group.PLAYER_DEAL)
				{
					NoticeHandler<Player> realNoticeHandler = noticeHandlerNight;

					if (!world.IsNight())
						realNoticeHandler = noticeHandlerDay;

					Vector3 direction = Vector3.Normalize(other.direction);

					Velocity = new Vector3(direction.X * 3.2f * Cube.CUBE_SCALE, 6.4f * Cube.CUBE_SCALE, direction.Z * 3.2f * Cube.CUBE_SCALE);

					health -= other.damage;

					if (health <= 0)
					{
						health = 0;
						world.EntityManager.Remove(this);
					}

					invulnTimer = 0.25f;

					realNoticeHandler.OnTakeDamage(other.owner);
				}
			}
		}

		//public override void Draw(GraphicsDevice device, Effect effect)
		//{
		//	base.Draw(device, effect);

		//	/*if (mesh.VBO == null)
		//		mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);
		//		//MakeMesh(device);

		//	RectangleF sourceRect = new RectangleF(0, 16, 16, 16);

		//	Vector3 tintColor = invulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

		//	Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("imp"),
		//		DrawHelper.BlackPixel, Main.assetsManager.GetAsset<Texture2D>("imp_emissive"), mesh.VBO, mesh.IBO,
		//		Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
		//		Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
		//		Matrix.CreateTranslation(Position), sourceRect, tintColor));*/

		//	if (health < maxHealth)
		//		DrawHelper3D.DrawHealthbar(device, health, maxHealth, Position);
		//}
	}
}
