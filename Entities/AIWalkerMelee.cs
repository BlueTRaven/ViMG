using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using BrUtility;

namespace ViMG.Entities
{
    public class AIWalkerMelee<T> : IHitboxOwner where T : Entity, IHasStats
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
		private readonly BuffManager buffManager;
		private readonly T entity;
		private float idleTimer;
		private float idleMoveTimer;
		private int idleMovements;
		private Vector2 idleDirection;
		private Vector2 idleHome;

		private State state;

		private float attackTimer;

		public float AttackTimer => attackTimer;

		public float InvulnTimer;

		private bool onGround;
		private bool shouldJump;
		private float shouldJumpLockTimer;	//Sometimes we want to prevent the entity from jumping again.

		public int TouchDamage = 2;
		public int AttackDamage = 4;

		public int Health;
		public int MaxHealth;

		public float AttackStunTime = 1.65f;    //the amount of time the ai waits in the attacking state after attacking before returning to the normal state
		public float AttackCooldownTime = 2f;   //the amount of time before the ai can enter the attacking state again
		public float AttackLockTime = 0.25f;    //the amount of time the ai spends in the attack state before it can begin moving again.
		public float AttackHitboxTime = 6f / 60f;

		public float ShootSpeed = Cube.CUBE_SCALE * 16;
		public float MoveSpeed = Cube.CUBE_SCALE;

		public float MoveTowardsTargetDistance = Cube.CUBE_SCALE * 1.75f;
		public float AttackTargetDistance = Cube.CUBE_SCALE * 1.75f;

		private bool isInRangeOfTarget;
		public bool IsInRangeOfTarget => isInRangeOfTarget;

		private Rectangle3D touchHitboxBounds;
		private int touchHitbox = -1;

		private Rectangle3D attackHitboxBounds;
		private int attackHitbox = -1;

		public AIWalkerMelee(T entity, Rectangle3D touchHitboxBounds, Rectangle3D attackHitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth)
		{
			this.noticeHandler = noticeHandler;
			this.buffManager = buffManager;
			this.entity = entity;

			this.Health = maxHealth;
			this.MaxHealth = maxHealth;

			this.touchHitboxBounds = touchHitboxBounds;
            this.attackHitboxBounds = attackHitboxBounds;
        }

		public void OnUnload()
		{
			if (touchHitbox != -1)
				entity.world.HitboxManager.Remove(touchHitbox);

			if (attackHitbox != -1)
				entity.world.HitboxManager.Remove(attackHitbox);
		}

		public void Update(double deltaTime)
		{
			InvulnTimer -= (float)deltaTime;

			if (touchHitbox == -1)
				touchHitbox = entity.world.HitboxManager.Add(this, touchHitboxBounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, TouchDamage, 1f, InvulnTimer <= 0);
			else entity.world.HitboxManager.Update(touchHitbox, touchHitboxBounds.Offset(entity.Position), InvulnTimer <= 0);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);
			buffManager.Update(deltaTime);

			isInRangeOfTarget = false;

			if (InvulnTimer <= 0 && onGround)
			{
				shouldJumpLockTimer -= (float)deltaTime;

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
						dir.Normalize();
						dir *= MoveSpeed;

						float distance = XZDistance(noticeHandler.GetNoticedEntity().Position, entity.Position);

						if (distance > MoveTowardsTargetDistance)
						{
							Velocity.X += dir.X;
							Velocity.Z += dir.Z;
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
							if (attackHitbox == -1)
								attackHitbox = entity.world.HitboxManager.Add(this, attackHitboxBounds.Offset(entity.Position + Vector3.Normalize(dir) * Cube.CUBE_SCALE * 1.5f),
									Vector3.Normalize(Velocity), HitboxManager.Group.ENEMYHOSTILE_BOTH, AttackDamage, 1);

							state = State.AttackStun;
							attackTimer = AttackStunTime;
						}
					}
					else if (state == State.AttackStun)
					{
						isInRangeOfTarget = true;

						if (attackTimer <= AttackStunTime - AttackHitboxTime)
                        {
							if (attackHitbox != -1)
                            {
								entity.world.HitboxManager.Remove(attackHitbox);
								attackHitbox = -1;
                            }
                        }

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

			if ((entity.world.player.Position - entity.Position).Length() > 128 * Cube.CUBE_SCALE)
				entity.world.EntityManager.Remove(entity);
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

						if (entity.world.GetChunkManager().IsInWorldBounds(pos) &&
							entity.world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
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

			if (onGround && state == State.Normal && InvulnTimer <= 0 && shouldJumpLockTimer > 0)
			{
				if (Velocity.Length() > Cube.CUBE_SCALE / 4f)
				{
					var ray = entity.world.RaycastVector(entity.Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(Velocity.X, 0, Velocity.Z), Cube.CUBE_SCALE * 2,
						(Vector3 pos) =>
						{
							Cube cube = entity.world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air);

							return cube.Collision != Cube.CollisionValue.None;
						});

					if (ray.hasHit)
					{
						shouldJump = true;
					}
				}
			}

			foreach (T otherEntity in entity.world.EntityManager.GetAll<T>())
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

					Velocity = direction * Cube.CUBE_SCALE * 3f * other.knockback;

					Health -= other.damage;

					if (Health <= 0)
					{
						Health = 0;
						entity.world.EntityManager.Remove(entity);

						if (touchHitbox != -1)
							entity.world.HitboxManager.Remove(touchHitbox);
					}

					shouldJumpLockTimer = 1f;
					buffManager.AddBuffs(other.applyBuffs);

					InvulnTimer = 0.25f;
					attackTimer = 0;    //immediately attempt to attack?

					noticeHandler.OnTakeDamage(other.owner);
				}
			}
		}

		public State GetState()
		{
			return state;
		}

		private static float XZDistance(Vector3 otherPosition, Vector3 position)
		{
			return (new Vector2(otherPosition.X, otherPosition.Z) - new Vector2(position.X, position.Z)).Length();
		}
	}
}
