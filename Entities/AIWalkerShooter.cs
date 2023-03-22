using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;

namespace ViMG.Entities
{
	//Walks towards, then shoots at, the player.
    public class AIWalkerShooter<T> : IHitboxOwner where T : Entity, IHasStats
    {
        public enum State
        {
            Normal,
            Attack,
            AttackStun
        }

		public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
		public Vector3 Velocity;
		public Vector3 Facing;
        private readonly NoticeHandler<Player> noticeHandler;
		private readonly BuffManager buffManager;
        private readonly World world;
        private readonly T entity;
		private readonly bool projectileBatch;
		private readonly ProjectileManager.ProjectileBatchStats shotProjectileBatchStats;
        private readonly ProjectileManager.ProjectileStats shotProjectileStats;
        private readonly ProjectileManager.ProjectileVisStats shotProjectileVisStats;
        private float idleTimer;
		private float idleMoveTimer;
		private int idleMovements;
		private Vector2 idleDirection;
		private Vector2 idleHome;

		private State state;

		private float attackTimer;

		public float InvulnTimer;

		private bool onGround;
		private bool shouldJump;
		public float shouldJumpLockTimer;

		public int Health;
		public int MaxHealth;

		public float AttackStunTime = 1.65f;	//the amount of time the shooter waits in the attacking state after attacking before returning to the normal state
		public float AttackCooldownTime = 2f;	//the amount of time before the shooter can enter the attacking state again
		public float AttackLockTime = 0.25f;	//the amount of time the shooter spends in the attack state before it can begin moving again.

		public float ShootSpeed = Cube.CUBE_SCALE * 16;
		public float MoveSpeed = Cube.CUBE_SCALE;

		public float MoveTowardsTargetDistance = Cube.CUBE_SCALE * 6f;
		public float AttackTargetDistance = Cube.CUBE_SCALE * 8;

		private bool isInRangeOfTarget;
		public bool IsInRangeOfTarget => isInRangeOfTarget;

		private Rectangle3D bounds;
		private int touchHitbox = -1;

		public AIWalkerShooter(World world, T entity, Rectangle3D hitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth, 
			ProjectileManager.ProjectileStats shotProjectileStats, 
			ProjectileManager.ProjectileVisStats shotProjectileVisStats)
        {
            this.noticeHandler = noticeHandler;
            this.buffManager = buffManager;
            this.world = world;
            this.entity = entity;

			this.Health = maxHealth;
			this.MaxHealth = maxHealth;

            this.shotProjectileStats = shotProjectileStats;
            this.shotProjectileVisStats = shotProjectileVisStats;

			projectileBatch = false;
			this.bounds = hitboxBounds;
        }

		public AIWalkerShooter(World world, T entity, Rectangle3D hitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth,
			ProjectileManager.ProjectileBatchStats shotProjectileBatchStats,
			ProjectileManager.ProjectileStats shotProjectileStats,
			ProjectileManager.ProjectileVisStats shotProjectileVisStats)
		{
			this.noticeHandler = noticeHandler;
            this.buffManager = buffManager;
            this.world = world;
			this.entity = entity;

			this.Health = maxHealth;
			this.MaxHealth = maxHealth;

			projectileBatch = true;
			this.shotProjectileBatchStats = shotProjectileBatchStats;

			this.shotProjectileStats = shotProjectileStats;
			this.shotProjectileVisStats = shotProjectileVisStats;

			this.bounds = hitboxBounds;
		}

		public void OnUnload()
		{
			if (touchHitbox != -1)
				entity.world.HitboxManager.Remove(touchHitbox);
		}

		public void Update(double deltaTime)
		{
			InvulnTimer -= (float)deltaTime;

			if (touchHitbox == -1)
				touchHitbox = world.HitboxManager.Add(this, bounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, 4, 1f, InvulnTimer <= 0);
			else world.HitboxManager.Update(touchHitbox, bounds.Offset(entity.Position), InvulnTimer <= 0);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);
			buffManager.Update(deltaTime);

			isInRangeOfTarget = false;

			if (InvulnTimer <= 0 && onGround)
			{
                shouldJumpLockTimer -= (float)deltaTime;

                if (shouldJump && shouldJumpLockTimer <= 0)
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
						dir = Vector3.Normalize(dir) * MoveSpeed;

						if (distance > MoveTowardsTargetDistance)
						{
                            EntityHelper.AddCappedVelocityHorizontal(ref Velocity, dir, actualMaxVel);
							Facing = Vector3.Normalize(dir);
						}
						else
						{
							Velocity.X *= 0.95f;
							Velocity.Z *= 0.95f;
						}

						if (distance < AttackTargetDistance)
						{
							isInRangeOfTarget = true;

							attackTimer -= (float)deltaTime;

							if (attackTimer <= 0)
							{
								attackTimer = AttackLockTime;
								state = State.Attack;
							}
						}
					}
					else if (state == State.Attack)
					{
						isInRangeOfTarget = true;

						Velocity.X *= 0.95f;
						Velocity.Z *= 0.95f;

						attackTimer -= (float)deltaTime;

						if (attackTimer <= 0)
						{
							Vector3 dir = (noticeHandler.GetNoticedEntity().Position - new Vector3(0, Cube.CUBE_SCALE, 0)) - entity.Position;

							if (!projectileBatch)
							{
								world.ProjectileManager.Add(new ProjectileManager.Projectile(this, entity.Position + new Vector3(0, Cube.CUBE_SCALE, 0),
									Vector3.Normalize(dir) * ShootSpeed,
									8, shotProjectileVisStats, shotProjectileStats),
									new Rectangle3D(-new Vector3(Cube.CUBE_SCALE / 4), new Vector3(Cube.CUBE_SCALE / 2)));
							}
                            else
                            {
								world.ProjectileManager.AddBatch(this, entity.Position + new Vector3(0, Cube.CUBE_SCALE, 0), Vector3.Normalize(dir) * ShootSpeed, 8, 
									shotProjectileBatchStats, shotProjectileVisStats, shotProjectileStats,
									new Rectangle3D(-new Vector3(Cube.CUBE_SCALE / 4), new Vector3(Cube.CUBE_SCALE / 2)));
                            }

							Facing = Vector3.Normalize(dir);

							state = State.AttackStun;
							attackTimer = AttackStunTime;
						}
					}
					else if (state == State.AttackStun)
					{
						isInRangeOfTarget = true;

						Velocity.X *= 0.5f;
						Velocity.Z *= 0.5f;

						attackTimer -= (float)deltaTime;

						if (attackTimer <= 0)
						{
							state = State.Normal;
							attackTimer = AttackCooldownTime;
						}
					}
				}
				else
				{
					state = State.Normal;
					attackTimer = AttackCooldownTime;

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
                        EntityHelper.AddCappedVelocityHorizontal(ref Velocity, idleDirection, actualMaxVel);
                        Facing = Vector3.Normalize(new Vector3(idleDirection.X, 0, idleDirection.Y));
                    }
					else
					{
						Velocity.X *= 0.85f;
						Velocity.Z *= 0.85f;
					}
				}
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
						CubePosition pos = CubePosition.FromWorldSpace(entity.Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);

						positions[pi] = pos;
						pi++;
					}
				}
			}

			entity.world.ChunkManager.ThreadedView.GetIds(positions, ids, ThreadedCubeView.SafetyCheck.InWorldBounds);

			for (int i = 0; i < total; i++)
			{
				CubePosition pos = positions[i];
				ushort id = ids[i];

				if (Main.Registry.CubeRegistry.GetOrDefault(id, Main.Registry.CubeRegistry.Air).Solid)
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

			if (onGround && state == State.Normal && InvulnTimer <= 0)
			{
				if (Velocity.Length() > Cube.CUBE_SCALE / 4f)
				{
					var ray = world.RaycastVector(entity.Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(Velocity.X, 0, Velocity.Z), Cube.CUBE_SCALE * 2,
						(Vector3 pos) =>
						{
							Cube cube = world.ChunkManager.ThreadedView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air);

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
					EntityHelper.CalculateKnockback(ref Velocity, other);

					Hurt(other.damage);

					//Kind of hacky - but we can assume that we want to face the source of knockback,
					//and we can do that by just using the negative velocity.
                    Facing = Vector3.Normalize(-Velocity);

                    buffManager.AddBuffs(other.applyBuffs);

					noticeHandler.OnTakeDamage(other.owner);
				}
			}
		}

		public void Hurt(int damage)
		{
            Health -= damage;

            if (Health <= 0)
            {
                Health = 0;
                world.EntityManager.Remove(entity);

                if (touchHitbox != -1)
                    world.HitboxManager.Remove(touchHitbox);
            }

            shouldJumpLockTimer = 1f;
            InvulnTimer = 0.25f;

            //interrupt current attack
            if (state == State.Attack || state == State.AttackStun)
                state = State.Normal;

            attackTimer = 0;    //immediately attempt to attack?
        }

		public State GetState()
        {
			return state;
        }
	}
}
