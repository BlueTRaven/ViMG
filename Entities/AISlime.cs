using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using BrUtility;
using BepuPhysics.CollisionDetection;

namespace ViMG.Entities
{
    public class AISlime<T> : IHitboxOwner where T : Entity, IHasStats
	{
		public float InvulnTimer;

		public float Acceleration = Cube.CUBE_SCALE / 2f;

		public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
		public Vector3 Velocity;
		private readonly NoticeHandler<Player> noticeHandler;
		private readonly BuffManager buffManager;
		private readonly T entity;
		
		private bool onGround;
		public bool OnGround => onGround;

		private Vector3 jumpDir;
		private int numJumps;
		private float jumpTimer;
		private float jumpTime;

		public float JumpTimer => jumpTimer;
		public float JumpTime => jumpTime;

		public bool ShouldJumpAwayFromPlayer;

		public int Health;
		public int MaxHealth = 10;

		public int TouchDamage = 2;
		private Rectangle3D touchHitboxBounds;
		private int touchHitbox = -1;

		public AISlime(T entity, Rectangle3D touchHitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth)
		{
			this.noticeHandler = noticeHandler;
			this.buffManager = buffManager;
			this.entity = entity;
			this.touchHitboxBounds = touchHitboxBounds;
			this.Health = maxHealth;
			this.MaxHealth = maxHealth;
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
				touchHitbox = entity.world.HitboxManager.Add(this, touchHitboxBounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, TouchDamage, 1f, InvulnTimer <= 0);
			else entity.world.HitboxManager.Update(touchHitbox, touchHitboxBounds.Offset(entity.Position), InvulnTimer <= 0);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);
			buffManager.Update(deltaTime);

			if (InvulnTimer <= 0 && onGround)
			{
				jumpTimer -= (float)deltaTime;

				if (jumpTimer <= 0)
				{
					jumpTime = Main.random.NextFloat(0.25f, 3);
					jumpTimer = jumpTime;

					if (!noticeHandler.Noticed)
					{
						if (numJumps == 0)
						{
							numJumps = Main.random.Next(1, 6);
							jumpTime = Main.random.NextFloat(2, 6);
							jumpTimer = jumpTime;

							if (ShouldJumpAwayFromPlayer)
							{
								jumpDir = entity.Position - entity.world.player.Position;
								jumpDir.Normalize();
							}
							else
							{
								//During the day time, jump in random directions
								jumpDir = new Vector3(Main.random.NextFloat(-1, 1), 0, Main.random.NextFloat(-1, 1));
								jumpDir.Normalize();
							}
						}

						Velocity = new Vector3(jumpDir.X * 1.6f * Cube.CUBE_SCALE, MaxVelocity.Y * 0.75f, jumpDir.Y * 1.6f * Cube.CUBE_SCALE);

						numJumps--;
					}
					else
					{
						Vector2 playerDir = Vector2.Normalize(new Vector2(noticeHandler.Target.Position.X, noticeHandler.Target.Position.Z) - new Vector2(entity.Position.X, entity.Position.Z));
						Velocity = new Vector3(playerDir.X * 1.6f * Cube.CUBE_SCALE, MaxVelocity.Y * 0.75f, playerDir.Y * 1.6f * Cube.CUBE_SCALE);
					}

					onGround = false;
				}
			}

			if (Velocity.Y < -actualMaxVel.Y)
				Velocity.Y = -actualMaxVel.Y;

			if (onGround)
			{
				Vector2 velocitySlowed = new Vector2(Velocity.X, Velocity.Z);
				if (velocitySlowed.Length() > 0)
				{
					velocitySlowed = Vector2.Normalize(velocitySlowed) * velocitySlowed.Length() * 0.85f;
				}

				Velocity = new Vector3(velocitySlowed.X, Velocity.Y, velocitySlowed.Y);
			}

			entity.Position += Velocity * (float)deltaTime;

			onGround = false;
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
					EntityHelper.CalculateKnockback(ref Velocity, other);

					Hurt(other.damage);

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
                entity.world.EntityManager.Remove(entity);

                if (touchHitbox != -1)
                    entity.world.HitboxManager.Remove(touchHitbox);
            }

            InvulnTimer = 0.25f;
        }
	}
}
