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
    public class AIPassive<T> : IHitboxOwner where T : Entity, IHasStats
	{
		public enum State
		{
			Normal,
			Flee,
		}

		public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
		public Vector3 MaxVelocityFleeing = new Vector3(Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 4f);
		public Vector3 Velocity;
		public Vector3 Facing = new Vector3(1, 0, 0);
		private readonly NoticeHandler<Player> noticeHandler;
		private readonly BuffManager buffManager;
		private readonly T entity;
		private float idleTimer;
		private float idleMoveTimer;
		private int idleMovements;
		private Vector2 idleDirection;
		private Vector2 idleHome;

		private State state;

		private float fleeTimer;
		public float FleeTimer => fleeTimer;

		public float InvulnTimer;

		private bool onGround;
		private bool shouldJump;
		private float shouldJumpLockTimer;  //Sometimes we want to prevent the entity from jumping again.

		public int Health;
		public int MaxHealth;

		private Rectangle3D touchHitboxBounds;
		private int touchHitbox = -1;
		
		public AIPassive(T entity, Rectangle3D touchHitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth)
		{
			this.noticeHandler = noticeHandler;
			this.buffManager = buffManager;
			this.entity = entity;

			this.Health = maxHealth;
			this.MaxHealth = maxHealth;

			this.touchHitboxBounds = touchHitboxBounds;
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
				touchHitbox = entity.world.HitboxManager.Add(this, touchHitboxBounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_TAKE, 0, 1f, InvulnTimer <= 0);
			else entity.world.HitboxManager.Update(touchHitbox, touchHitboxBounds.Offset(entity.Position), InvulnTimer <= 0);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);
			buffManager.Update(deltaTime);

			if (InvulnTimer <= 0 && onGround)
			{
				shouldJumpLockTimer -= (float)deltaTime;

				if (shouldJump && shouldJumpLockTimer <= 0)
				{
					Velocity.Y = Cube.CUBE_SCALE * 10;
					shouldJump = false;
				}

				if (state == State.Normal)
				{
					idleTimer -= (float)deltaTime;

					if (idleTimer <= 0)
						idleMoveTimer -= (float)deltaTime;

					if (idleMovements == 0 && idleTimer <= 0 && idleMoveTimer <= 0)
					{
						idleHome = new Vector2(entity.Position.X, entity.Position.Z);

						idleTimer = Main.random.NextFloat(5f, 12f);
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

						Facing = Vector3.Normalize(Velocity);
					}
					else
					{
						Velocity.X *= 0.85f;
						Velocity.Z *= 0.85f;
					}
				}
                else
                {
					actualMaxVel = MaxVelocityFleeing;

					Vector3 dir = noticeHandler.Target.Position - entity.Position;
					dir.Normalize();

					Velocity.X -= dir.X;
					Velocity.Z -= dir.Z;

					fleeTimer -= (float)deltaTime;

					if (fleeTimer <= 0)
                    {
						state = State.Normal;
						idleMovements = 0;
						idleTimer = 0;
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

			entity.world.ChunkManager2.ThreadedView.GetIds(positions, ids, ThreadedCubeView.SafetyCheck.InWorldBounds);

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

			if (onGround && InvulnTimer <= 0 && shouldJumpLockTimer <= 0)
			{
				if (Velocity.Length() > Cube.CUBE_SCALE / 4f)
				{
					var ray = entity.world.RaycastVector(entity.Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(Velocity.X, 0, Velocity.Z), Cube.CUBE_SCALE * 2,
						(Vector3 pos) =>
						{
							Cube cube = entity.world.ChunkManager2.ThreadedView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air);

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
			if (entity is IHitboxOwner hitboxOwner)
				hitboxOwner.OnInteractWithOther(us, other);

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

					noticeHandler.OnTakeDamage(other.owner);

					if (state == State.Normal)
					{
						state = State.Flee;
						fleeTimer = 6f;
					}
				}
			}
		}

		public State GetState()
		{
			return state;
		}
	}
}
