using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;

namespace ViMG.Entities
{
    public class AISlime : IHitboxOwner
    {
        private readonly World world;
        private readonly NoticeHandler<Player> noticeHandler;
		private readonly Entity entity;

		public Vector3 Velocity;
		private Vector3 maxVelocity = new Vector3(3.2f * Cube.CUBE_SCALE, 17 * Cube.CUBE_SCALE, 3.2f * Cube.CUBE_SCALE);
        private Rectangle3D bounds;
		private int touchHitbox = -1;

		private float invulnTimer;
		private float despawnTimer;
		private const float DESPAWN_TIME = 10f;

		private int health;
		private int maxHealth = 4;

		public int Health => health;
		public int MaxHealth => maxHealth;

		private Vector3 jumpDir;
		private int numJumps;
		private float jumpTimer;
		private float jumpTime;

		public float JumpTimer => jumpTimer;
		public float JumpTime => jumpTime;

		private float alive;
		public float Alive => alive;

		public RectangleF SourceRectangle;

		public AISlime(World world, Entity entity, NoticeHandler<Player> noticeHandler, Rectangle3D bounds, int maxHealth)
        {
            this.world = world;
			this.entity = entity;
            this.noticeHandler = noticeHandler;
            this.bounds = bounds;

			this.maxHealth = maxHealth;
			this.health = maxHealth;
        }

		public void Update(double deltaTime, bool onGround)
		{
			alive += (float)deltaTime;

			if (touchHitbox == -1)
				touchHitbox = world.HitboxManager.Add(this, bounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, 1, 1f, invulnTimer <= 0);
			else world.HitboxManager.Update(touchHitbox, bounds.Offset(entity.Position), invulnTimer <= 0);

			Vector3 actualMaxVel = maxVelocity;

			Velocity.Y += World.GRAVITY;

			if (invulnTimer <= 0)
			{
				if (onGround)
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

								if (world.IsNight())
								{
									//During the night time, jump away from the player, regardless of whether or not they're noticed
									jumpDir = entity.Position - world.player.Position;
									jumpDir.Normalize();
								}
								else
								{
									//During the day time, jump in random directions
									jumpDir = new Vector3(Main.random.NextFloat(-1, 1), 0, Main.random.NextFloat(-1, 1));
									jumpDir.Normalize();
								}
							}

							Velocity = new Vector3(jumpDir.X * 1.6f * Cube.CUBE_SCALE, maxVelocity.Y * 0.75f, jumpDir.Y * 1.6f * Cube.CUBE_SCALE);

							numJumps--;
						}
						else
						{
							Vector2 playerDir = Vector2.Normalize(new Vector2(noticeHandler.Target.Position.X, noticeHandler.Target.Position.Z) - new Vector2(entity.Position.X, entity.Position.Z));
							Velocity = new Vector3(playerDir.X * 1.6f * Cube.CUBE_SCALE, maxVelocity.Y * 0.75f, playerDir.Y * 1.6f * Cube.CUBE_SCALE);
						}

						onGround = false;
					}

					//At night, despawn 
					if (world.IsNight())
					{
						float distance = (entity.Position - world.player.Position).Length();

						if (distance > Cube.CUBE_SCALE * 24)
						{
							despawnTimer -= (float)deltaTime;

							if (!noticeHandler.Noticed && despawnTimer <= 0)
								world.EntityManager.Remove(entity);
						}
						else despawnTimer = DESPAWN_TIME;
					}
					else despawnTimer = DESPAWN_TIME;
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

			invulnTimer -= (float)deltaTime;

			noticeHandler.Update(deltaTime);

			//Kill self if too far away
			if ((world.player.Position - entity.Position).Length() > 128 * Cube.CUBE_SCALE)
				world.EntityManager.Remove(entity);
		}

		public void OnUnload()
        {
			if (touchHitbox != -1)
				world.HitboxManager.Remove(touchHitbox);
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
						world.EntityManager.Remove(entity);
					}

					invulnTimer = 0.25f;

					noticeHandler.OnTakeDamage(other.owner);
				}
			}
		}
	}
}
