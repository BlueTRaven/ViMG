using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using Microsoft.Xna.Framework.Graphics;

namespace ViMG.Entities
{
    public class Imp : Entity, IHitboxOwner
    {
		private static SimpleMesh<VertexPositionColorTextureNormal, int> mesh;

		private bool onGround;

		private NoticeHandler<Player> noticeHandler;

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

			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);

			health = maxHealth;
		}

		public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			alive += (float)deltaTime;

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, Slime.GROUP_ENEMYHOSTILE_SOURCE, 1, 1f);
			else world.HitboxManager.Update(hitbox, Bounds);

			float p0 = (alive % 0.65f) / 0.65f;
			float p1 = ((alive + 0.3f) % 0.45f) / 0.45f;
			float s0 = MathF.Sin(MathF.PI * 2 * p0) * Cube.CUBE_SCALE * 1.25f;
			float s1 = MathF.Sin(MathF.PI * 2 * p0) * Cube.CUBE_SCALE * 1.25f;

			if (light == -1)
				light = world.LightManager.Add(Position, Cube.CUBE_SCALE * 4 + s0, Cube.CUBE_SCALE * 8 + s1, Color.OrangeRed);
			else world.LightManager.Update(light, Position, Cube.CUBE_SCALE * 4 + s0, Cube.CUBE_SCALE * 8 + s1, Color.OrangeRed);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);

			invulnTimer -= (float)deltaTime;

			if (invulnTimer <= 0 && onGround)
			{
				if (noticeHandler.Noticed)
				{
					Vector3 distance = (noticeHandler.GetNoticedEntity().Position - new Vector3(0, Cube.CUBE_SCALE, 0)) - Position;

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
							ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(this, 
								Slime.GROUP_ENEMYHOSTILE_SOURCE, 1, Cube.CUBE_SCALE / 4, Cube.CUBE_SCALE, false, true); ;
							ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"), 
								new RectangleF(32, 0, 16, 16), Cube.CUBE_SCALE, 
								Color.Red.ToVector4(), new Vector2(Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 4));

							world.ProjectileManager.Add(new ProjectileManager.Projectile(Position + new Vector3(0, Cube.CUBE_SCALE, 0), 
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

			//if (!onGround) Velocity = new Vector3(Velocity.X * 0.95f, Velocity.Y, Velocity.Z * 0.95f);

			Position += Velocity * (float)deltaTime;

			onGround = false;
			UpdateCollision();

			if ((world.player.Position - Position).Length() > 128 * Cube.CUBE_SCALE)
				world.EntityManager.Remove(this);
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

						if (world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							Vector3 checkPos = Position + new Vector3(0, 8, 0);
							if (CollisionHelper.CheckCollision(cubeBounds, checkPos, 8, out Vector3 change))
							{
								Position = (checkPos - new Vector3(0, 8, 0)) + change;

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
				if (other.group == Player.GROUP_PLAYER_DEAL_SOURCE)
				{
					Vector3 direction = Vector3.Normalize(other.direction);

					Velocity = new Vector3(direction.X * 3.2f * Cube.CUBE_SCALE, 6.4f * Cube.CUBE_SCALE, direction.Z * 3.2f * Cube.CUBE_SCALE);

					health -= other.damage;

					if (health <= 0)
					{
						health = 0;
						world.EntityManager.Remove(this);
					}

					invulnTimer = 0.25f;

					noticeHandler.OnTakeDamage(other.owner as Player);
				}
			}
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh == null)
				MakeMesh(device);

			RectangleF sourceRect = new RectangleF(0, 16, 16, 16);

			Vector3 tintColor = invulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("imp"),
				DrawHelper.BlackPixel, Main.assetsManager.GetAsset<Texture2D>("imp_emissive"), mesh.VBO, mesh.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), sourceRect, tintColor));

			DrawHelper3D.DrawHealthbar(device, health, maxHealth, Position);
		}

		private static void MakeMesh(GraphicsDevice device)
		{
			Vector3 min = -new Vector3(Cube.CUBE_SCALE / 2f, 0, 0);
			Vector3 max = new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE, 0);

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

			mesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("imp"));
		}
	}
}
