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
    public class Cultist : Entity, IHitboxOwner
    {
		private enum State
        {
			Normal,
			Attack,
			AttackStun
        }

		private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

		private NoticeHandler<Player> noticeHandler;

		public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
		public Vector3 Velocity;

		private int health;
		private int maxHealth = 140;

		private bool onGround;
		private bool shouldJump;

		private Rectangle3D Bounds => new Rectangle3D(Position - new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
			new Vector3(Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 0.70f));
		private int hitbox = -1;

		private float invulnTimer;
		private float resurrectTimer;
		private float alive;

		private float idleTimer;
		private float idleMoveTimer;
		private int idleMovements;
		private Vector2 idleDirection;
		private Vector2 idleHome;

		private State state;

		private const float ATTACK_STUN_TIME = 1.65f;	//the amount of time the cultist waits in the attacking state after attacking before returning to the normal state
		private const float ATTACK_COOLDOWN_TIME = 2f;	//the amount of time before the cultist can enter the attacking state again
		private const float ATTACK_TIME = 0.25f;		
		private float attackTimer;

		public Cultist()
        {
        }

		public Cultist(Vector3 position)
        {
			this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			health = maxHealth;

			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
		}

        public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			alive += (float)deltaTime;

			invulnTimer -= (float)deltaTime;

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, 4, 1f);
			else world.HitboxManager.Update(hitbox, Bounds);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);

			if (invulnTimer <= 0 && onGround)
			{
				if (shouldJump)
				{
					Velocity.Y = Cube.CUBE_SCALE * 10;
					shouldJump = false;
				}

				if (noticeHandler.Noticed)
				{
					idleMovements = 0;

					if (state == State.Normal)
					{
						Vector3 dir = noticeHandler.GetNoticedEntity().Position - Position;
						float distance = dir.Length();
						dir = Vector3.Normalize(dir) * Cube.CUBE_SCALE;

						if (distance > Cube.CUBE_SCALE * 6f)
                        {
							Velocity.X += dir.X;
							Velocity.Z += dir.Z;
						}
						else
                        {
							Velocity.X *= 0.95f;
							Velocity.Z *= 0.95f;
                        }

						if (distance < Cube.CUBE_SCALE * 8)
                        {
							attackTimer -= (float)deltaTime;

							if (attackTimer <= 0)
                            {
								attackTimer = ATTACK_TIME;
								state = State.Attack;
                            }
                        }
					}
					else if (state == State.Attack)
                    {
						Velocity.X *= 0.95f;
						Velocity.Z *= 0.95f;

						attackTimer -= (float)deltaTime;

						if (attackTimer <= 0)
						{
							Vector3 dir = (noticeHandler.GetNoticedEntity().Position - new Vector3(0, Cube.CUBE_SCALE, 0)) - Position;

							ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(this,
									HitboxManager.Group.ENEMYHOSTILE_BOTH, 1, Cube.CUBE_SCALE / 4, Cube.CUBE_SCALE, false, true); ;
							ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"),
								new RectangleF(32, 0, 16, 16), Cube.CUBE_SCALE,
								Color.Red.ToVector4(), new Vector2(Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 4));

							world.ProjectileManager.Add(new ProjectileManager.Projectile(Position + new Vector3(0, Cube.CUBE_SCALE, 0),
								Vector3.Normalize(dir) * Cube.CUBE_SCALE * 16,
								8, visStats, stats),
								new Rectangle3D(-new Vector3(Cube.CUBE_SCALE / 4), new Vector3(Cube.CUBE_SCALE / 2)));

							state = State.AttackStun;
							attackTimer = ATTACK_STUN_TIME;
						}
					}
					else if (state == State.AttackStun)
                    {
						Velocity.X *= 0.5f;
						Velocity.Z *= 0.5f;

						attackTimer -= (float)deltaTime;

						if (attackTimer <= 0)
                        {
							state = State.Normal;
							attackTimer = ATTACK_COOLDOWN_TIME;
                        }
                    }
				}
				else
				{
					state = State.Normal;
					attackTimer = ATTACK_COOLDOWN_TIME;

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

			onGround = false;
			shouldJump = false;
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

						if (world.GetChunkManager().IsInWorldBounds(pos) &&
							world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
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
				}
			}

			if (onGround && state == State.Normal && invulnTimer <= 0)
			{
				if (Velocity.Length() > Cube.CUBE_SCALE / 4f)
				{
					var ray = world.RaycastVector(Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(Velocity.X, 0, Velocity.Z), Cube.CUBE_SCALE * 2,
						(Vector3 pos) =>
						{
							Cube cube = world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air);

							return cube.Collision != Cube.CollisionValue.None;
						});

					if (ray.hasHit)
					{
						shouldJump = true;
					}
				}
			}

			foreach (Cultist cultist in world.EntityManager.GetAll<Cultist>())
			{
				if (cultist != this)
				{
					Vector2 distXZ = new Vector2(Position.X, Position.Z) - new Vector2(cultist.Position.X, cultist.Position.Z);

					if (distXZ.Length() < Cube.CUBE_SCALE)
					{
						Vector2 correctPos = new Vector2(cultist.Position.X, cultist.Position.Z) + Vector2.Normalize(distXZ) * Cube.CUBE_SCALE;

						Position = new Vector3(correctPos.X, Position.Y, correctPos.Y);
					}
				}
			}
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh.VBO == null)
			{
				float pixelsPerCube = Cube.CUBE_SCALE / 16f;
				mesh = MeshHelper.MakeEnemyQuad(device, pixelsPerCube * 19, pixelsPerCube * 32);
			}

			//world.DrawWireframeUnscaled(device, Bounds, Color.Red);

			RectangleF sourceRect = new RectangleF(0, 0, 19, 32);

			if (state == State.Normal)
			{
				if (Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
				{
					float animP = (alive % 0.75f) / 0.75f;

					int frame = (int)(animP * 2f);

					sourceRect = new RectangleF(22 + frame * 22, 0, 19, 32);
				}
			}
			else if (state == State.Attack)
            {
				sourceRect = new RectangleF(65, 0, 19, 32);
			}

			Vector3 tintColor = invulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("cultist"),
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), sourceRect, tintColor));

			DrawHelper3D.DrawHealthbar(device, health, maxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
		}

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
			if (invulnTimer <= 0)
			{
				if (other.group == HitboxManager.Group.PLAYER_DEAL)
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
					attackTimer = 0;	//immediately attempt to attack?

					noticeHandler.OnTakeDamage(other.owner as Player);
				}
			}
		}
    }
}
