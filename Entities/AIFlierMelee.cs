using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class AIFlierMelee<T> : IHitboxOwner where T : Entity, IHasStats
    {
		public enum State
		{
			Normal,
			Attack,
			AttackStun,
		}

		private State state;

		public float InvulnTimer;

		public Vector3 Velocity;
		public float MaxVelocity = Cube.CUBE_SCALE * 6;

		public float Acceleration = Cube.CUBE_SCALE / 2f;

		public float MoveTowardsTargetDistance = Cube.CUBE_SCALE * 2f;
		public float AttackTargetDistance = Cube.CUBE_SCALE * 2f;

		public float AttackStunTime = 1.65f;    //the amount of time the ai waits in the attacking state after attacking before returning to the normal state
		public float AttackCooldownTime = 2f;   //the amount of time before the ai can enter the attacking state again
		public float AttackLockTime = 0.25f;    //the amount of time the ai spends in the attack state before it can begin moving again.
		public float AttackHitboxTime = 6f / 60f;
		private float attackTimer;

		public float AttackTimer => attackTimer;

		public int Health;
		public int MaxHealth = 10;

		public int TouchDamage = 2;
		private Rectangle3D touchHitboxBounds;
		private int touchHitbox = -1;

		public int AttackDamage = 4;
		private Rectangle3D attackHitboxBounds;
		private int attackHitbox = -1;

		private BuffManager buffManager;
		private NoticeHandler<Player> noticeHandler;
		private Entity entity;

		public AIFlierMelee(World world, T entity, Rectangle3D touchHitboxBounds, Rectangle3D attackHitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth)
		{
			this.noticeHandler = noticeHandler;
			this.buffManager = buffManager;
			this.entity = entity;
            this.touchHitboxBounds = touchHitboxBounds;
            this.attackHitboxBounds = attackHitboxBounds;

            this.Health = maxHealth;
			this.MaxHealth = maxHealth;
		}

		public void Update(double deltaTime)
		{
			InvulnTimer -= (float)deltaTime;

			if (touchHitbox == -1)
				touchHitbox = entity.world.HitboxManager.Add(this, touchHitboxBounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, TouchDamage, 1f, InvulnTimer <= 0);
			else entity.world.HitboxManager.Update(touchHitbox, touchHitboxBounds.Offset(entity.Position), InvulnTimer <= 0);

			buffManager.Update(deltaTime);
			noticeHandler.Update(deltaTime);

			if (noticeHandler.Noticed)
			{
				Vector3 playerDir = noticeHandler.GetNoticedEntity().Position - entity.Position;
				float distance = playerDir.Length();
				playerDir.Normalize();

				if (state == State.Normal)
				{
					if (distance > MoveTowardsTargetDistance)
					{
						if (Velocity.Length() > 0)
						{
							Vector3 velocityDir = Vector3.Normalize(Velocity);
							float velocityLen = Velocity.Length();

							Vector3 cross = Vector3.Cross(velocityDir, playerDir);
							Matrix mat = Matrix.CreateFromAxisAngle(cross, MathHelper.ToRadians(5));

							Vector3 rotated = Vector3.Normalize(Vector3.Transform(velocityDir, mat));
							Velocity = rotated * (velocityLen + Acceleration);
						}
						else
						{
							//Initial first acceleration (we start with 0 velocity, and that results in NaNs, so we kinda have to seed it)
							Velocity += playerDir * Acceleration;
						}
					}
					else
					{
						//slow down very fast.
						Velocity *= 0.65f;

						if (distance < AttackTargetDistance)
						{
							attackTimer -= (float)deltaTime;

							if (attackTimer <= 0)
							{
								attackTimer = AttackLockTime;
								state = State.Attack;
							}
						}
					}
				}
				else if (state == State.Attack)
				{
					Velocity *= 0.95f;

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
					if (attackTimer <= AttackStunTime - AttackHitboxTime)
					{
						if (attackHitbox != -1)
						{
							entity.world.HitboxManager.Remove(attackHitbox);
							attackHitbox = -1;
						}
					}

					Velocity *= 0.95f;

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
				//TODO wander behavior
			}

			if (Velocity.Length() > MaxVelocity)
				Velocity = Vector3.Normalize(Velocity) * MaxVelocity;

			entity.Position += Velocity * (float)deltaTime;

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
						CubePosition pos = CubePosition.FromWorldSpace(entity.Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace); //CubePosition.FromWorldSpace(entity.Position);

						if (entity.world.GetChunkManager().IsInWorldBounds(pos) &&
							entity.world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							Vector3 offset = new Vector3(0, Cube.CUBE_SCALE * 0.25f, 0);
							Vector3 checkPos = entity.Position + offset;

							if (CollisionHelper.CheckCollision(cubeBounds, checkPos, Cube.CUBE_SCALE * 0.25f, out Vector3 change))
							{
								entity.Position = (checkPos - offset) + change;

								if (change.Y != 0)
									Velocity.Y = -Velocity.Y * 0.5f;
								else if (change.X != 0)
									Velocity.X = -Velocity.X * 0.5f;
								else if (change.Z != 0)
									Velocity.Z = -Velocity.Z * 0.5f;
							}
						}
					}
				}
			}

			foreach (T otherEntity in entity.world.EntityManager.GetAll<T>())
			{
				if (otherEntity != entity)
				{
					//Vector2 distXZ = new Vector2(entity.Position.X, entity.Position.Z) - new Vector2(otherEntity.entity.Position.X, otherEntity.entity.Position.Z);
					Vector3 direction = entity.Position - otherEntity.Position;

					if (direction.Length() < Cube.CUBE_SCALE)
					{
						Vector3 correctPos = otherEntity.Position + Vector3.Normalize(direction) * Cube.CUBE_SCALE;

						entity.Position = correctPos;
						Velocity = -Velocity;
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

					Velocity = new Vector3(direction.X * Cube.CUBE_SCALE * other.knockback, direction.Y * Cube.CUBE_SCALE * other.knockback, direction.Z * Cube.CUBE_SCALE * other.knockback);

					Health -= other.damage;

					if (Health <= 0)
					{
						Health = 0;
						entity.world.EntityManager.Remove(entity);

						if (touchHitbox != -1)
							entity.world.HitboxManager.Remove(touchHitbox);
					}

					buffManager.AddBuffs(other.applyBuffs);

					InvulnTimer = 0.25f;
					attackTimer = 0;    //immediately attempt to attack?

					noticeHandler.OnTakeDamage(other.owner as Player);
				}
				else if ((us.group & HitboxManager.Group.ENEMYHOSTILE_DEAL) == HitboxManager.Group.ENEMYHOSTILE_DEAL && other.group == HitboxManager.Group.PLAYER_TAKE &&
					other.canInteract)
				{
					//Bounce off the player if we deal contact damage to them
					Velocity = -Velocity;
				}
			}
		}

		public State GetState()
        {
			return state;
        }
	}
}
