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
	//Walks towards, then shoots at, the player.
    public class AIWalkerShooter<T> : IHitboxOwner where T : Entity
    {
        public enum State
        {
            Normal,
            Attack,
            AttackStun
        }

		public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
		public Vector3 Velocity;
        private readonly NoticeHandler<Player> noticeHandler;
        private readonly World world;
        private readonly T entity;
        private readonly ProjectileManager.ProjectileStats shotProjectileStats;
        private readonly ProjectileManager.ProjectileVisStats shotProjectileVisStats;
        private float idleTimer;
		private float idleMoveTimer;
		private int idleMovements;
		private Vector2 idleDirection;
		private Vector2 idleHome;

		private State state;

		private const float ATTACK_STUN_TIME = 1.65f;   //the amount of time the cultist waits in the attacking state after attacking before returning to the normal state
		private const float ATTACK_COOLDOWN_TIME = 2f;  //the amount of time before the cultist can enter the attacking state again
		private const float ATTACK_TIME = 0.25f;
		private float attackTimer;

		public float InvulnTimer;

		private bool onGround;
		private bool shouldJump;

		private int health;
		private int maxHealth;

		private Rectangle3D bounds;
		private int hitbox = -1;

		public AIWalkerShooter(World world, T entity, Rectangle3D hitboxBounds, NoticeHandler<Player> noticeHandler, int maxHealth, ProjectileManager.ProjectileStats shotProjectileStats, ProjectileManager.ProjectileVisStats shotProjectileVisStats)
        {
            this.noticeHandler = noticeHandler;
            this.world = world;
            this.entity = entity;

			this.health = maxHealth;
			this.maxHealth = maxHealth;

            this.shotProjectileStats = shotProjectileStats;
            this.shotProjectileVisStats = shotProjectileVisStats;

			this.bounds = hitboxBounds;
        }

		public void Update(double deltaTime)
		{
			InvulnTimer -= (float)deltaTime;

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(this, bounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, 4, 1f);
			else world.HitboxManager.Update(hitbox, bounds.Offset(entity.Position));

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);

			if (InvulnTimer <= 0 && onGround)
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
						Vector3 dir = noticeHandler.GetNoticedEntity().Position - entity.Position;
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
							Vector3 dir = (noticeHandler.GetNoticedEntity().Position - new Vector3(0, Cube.CUBE_SCALE, 0)) - entity.Position;

							world.ProjectileManager.Add(new ProjectileManager.Projectile(this, entity.Position + new Vector3(0, Cube.CUBE_SCALE, 0),
								Vector3.Normalize(dir) * Cube.CUBE_SCALE * 16,
								8, shotProjectileVisStats, shotProjectileStats),
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
						idleHome = new Vector2(entity.Position.X, entity.Position.Z);

						idleTimer = Main.random.NextFloat(4f, 12f);
						idleMoveTimer = Main.random.NextFloat(0.25f, 2f);
						idleMovements = Main.random.Next(2, 6);

						idleDirection = Main.random.NextAngle();
					}
					else
					{
						float distFromIdleHome = (new Vector2(entity.Position.X, entity.Position.Z) - idleHome).Length();

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

			entity.Position += Velocity * (float)deltaTime;

			onGround = false;
			shouldJump = false;
			UpdateCollision();

			if ((world.player.Position - entity.Position).Length() > 128 * Cube.CUBE_SCALE)
				world.EntityManager.Remove(entity);
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
						CubePosition pos = CubePosition.FromWorldSpace(entity.Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace); //CubePosition.FromWorldSpace(Position);

						if (world.GetChunkManager().IsInWorldBounds(pos) &&
							world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							Vector3 offset = new Vector3(0, Cube.CUBE_SCALE * 0.25f, 0);
							Vector3 checkPos = entity.Position + offset;

							if (CollisionHelper.CheckCollision(cubeBounds, checkPos, Cube.CUBE_SCALE * 0.25f, out Vector3 change))
							{
								entity.Position = (checkPos - offset) + change;

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

			if (onGround && state == State.Normal && InvulnTimer <= 0)
			{
				if (Velocity.Length() > Cube.CUBE_SCALE / 4f)
				{
					var ray = world.RaycastVector(entity.Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(Velocity.X, 0, Velocity.Z), Cube.CUBE_SCALE * 2,
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

			foreach (T otherEntity in world.EntityManager.GetAll<T>())
			{
				if (otherEntity != entity)
				{
					Vector2 distXZ = new Vector2(entity.Position.X, entity.Position.Z) - new Vector2(otherEntity.Position.X, otherEntity.Position.Z);

					if (distXZ.Length() < Cube.CUBE_SCALE)
					{
						Vector2 correctPos = new Vector2(otherEntity.Position.X, otherEntity.Position.Z) + Vector2.Normalize(distXZ) * Cube.CUBE_SCALE;

						entity.Position = new Vector3(correctPos.X, entity.Position.Y, correctPos.Y);
					}
				}
			}
		}

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
			if (InvulnTimer <= 0)
			{
				if (other.group == HitboxManager.Group.PLAYER_DEAL)
				{
					Vector3 direction = Vector3.Normalize(other.direction);

					Velocity = new Vector3(direction.X * 3.2f * Cube.CUBE_SCALE, 6.4f * Cube.CUBE_SCALE, direction.Z * 3.2f * Cube.CUBE_SCALE);

					health -= other.damage;

					if (health <= 0)
					{
						health = 0;
						world.EntityManager.Remove(entity);

						if (hitbox != -1)
							world.HitboxManager.Remove(hitbox);
					}

					InvulnTimer = 0.25f;
					attackTimer = 0;    //immediately attempt to attack?

					noticeHandler.OnTakeDamage(other.owner as Player);
				}
			}
		}

		public State GetState()
        {
			return state;
        }

		public int GetHealth()
        {
			return health;
        }
	}
}
