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
using Engine.Networking;
using Engine;

namespace ViMG.Entities
{
	public class AISlime : ISyncedEntity
	{
		public const int VERSION = 0;

		public float InvulnTimer;

		public float Acceleration = Cube.CUBE_SCALE / 2f;

		public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
		public Vector3 Velocity;
		private readonly NoticeHandler<Player> noticeHandler;
		private readonly BuffManager buffManager;
		//private readonly T entity;

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

		public AISlime(Rectangle3D touchHitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth)
		{
			this.noticeHandler = noticeHandler;
			this.buffManager = buffManager;
			//this.entity = entity;
			this.touchHitboxBounds = touchHitboxBounds;
			this.Health = maxHealth;
			this.MaxHealth = maxHealth;
		}

		public struct Funcs<T> : IHitboxOwner where T : Entity, IHasStats
		{
			public T entity;
			public AISlime ai;

			public void OnUnload()
			{
				if (ai != null && ai.touchHitbox != -1)
					entity.world.HitboxManager.Remove(ai.touchHitbox);
			}
			public void Update(double deltaTime)
			{
				var randNum = entity.random.Next();

				ai.InvulnTimer -= (float)deltaTime;

				if (ai.touchHitbox == -1)
                    ai.touchHitbox = entity.world.HitboxManager.Add(this, ai.touchHitboxBounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, ai.TouchDamage, 1f, ai.InvulnTimer <= 0);
				else entity.world.HitboxManager.Update(ai.touchHitbox, ai.touchHitboxBounds.Offset(entity.Position).ToOBB(), ai.InvulnTimer <= 0);

				Vector3 actualMaxVel = ai.MaxVelocity;

                ai.Velocity.Y += World.GRAVITY;

                ai.noticeHandler.Update(deltaTime);
                ai.buffManager.Update(deltaTime);

				if (ai.InvulnTimer <= 0 && ai.onGround)
				{
                    ai.jumpTimer -= (float)deltaTime;

					if (ai.jumpTimer <= 0)
					{
                        ai.jumpTime = entity.random.NextFloat(0.25f, 3);
                        ai.jumpTimer = ai.jumpTime;

						if (!ai.noticeHandler.Noticed)
						{
							if (ai.numJumps == 0)
							{
                                ai.numJumps = entity.random.Next(1, 6);
                                ai.jumpTime = entity.random.NextFloat(2, 6);
                                ai.jumpTimer = ai.jumpTime;

								if (ai.ShouldJumpAwayFromPlayer)
								{
                                    ai.jumpDir = entity.Position - (entity.world.GetClosestPlayer(entity.Position)?.Position ?? Vector3.Zero);
                                    ai.jumpDir.Normalize();
								}
								else
								{
                                    //During the day time, jump in random directions
                                    ai.jumpDir = new Vector3(entity.random.NextFloat(-1, 1), 0, entity.random.NextFloat(-1, 1));
                                    ai.jumpDir.Normalize();
								}
							}

                            ai.Velocity = new Vector3(ai.jumpDir.X * 1.6f * Cube.CUBE_SCALE, ai.MaxVelocity.Y * 0.75f, ai.jumpDir.Y * 1.6f * Cube.CUBE_SCALE);

                            ai.numJumps--;
						}
						else
						{
							Vector2 playerDir = Vector2.Normalize(new Vector2(ai.noticeHandler.Target.Position.X, ai.noticeHandler.Target.Position.Z) - new Vector2(entity.Position.X, entity.Position.Z));
                            ai.Velocity = new Vector3(playerDir.X * 1.6f * Cube.CUBE_SCALE, ai.MaxVelocity.Y * 0.75f, playerDir.Y * 1.6f * Cube.CUBE_SCALE);
						}

                        ai.onGround = false;
					}
				}

				if (ai.Velocity.Y < -actualMaxVel.Y)
                    ai.Velocity.Y = -actualMaxVel.Y;

				if (ai.onGround)
				{
					Vector2 velocitySlowed = new Vector2(ai.Velocity.X, ai.Velocity.Z);
					if (velocitySlowed.Length() > 0)
					{
						velocitySlowed = Vector2.Normalize(velocitySlowed) * velocitySlowed.Length() * 0.85f;
					}

                    ai.Velocity = new Vector3(velocitySlowed.X, ai.Velocity.Y, velocitySlowed.Y);
				}

				entity.Position += ai.Velocity * (float)deltaTime;

                ai.onGround = false;
				UpdateCollision();

				EntityHelper.UnloadIfDistanceFromPlayers(entity);
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

							if (entity.world.ChunkManager.IsInWorldBounds(pos))
							{
								positions[pi] = pos;
								pi++;
							}
						}
					}
				}

				entity.world.ChunkManager.CubeView.GetIds(positions[..pi], ids[..pi]);

				for (int i = 0; i < total; i++)
				{
					CubePosition pos = positions[i];
					ushort id = ids[i];

					if (GlobalState.Registry.CubeRegistry.GetOrDefault(id, GlobalState.Registry.CubeRegistry.Air).Solid)
					{
						Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

						Vector3 offset = new Vector3(0, Cube.CUBE_SCALE * 0.25f, 0);
						Vector3 checkPos = entity.Position + offset;

						if (CollisionHelper.CheckCollision(cubeBounds, checkPos, Cube.CUBE_SCALE * 0.25f, out Vector3 change))
						{
							entity.Position = (checkPos - offset) + change;

							if (change.Y > 0)
							{
                                ai.Velocity.Y = 0;
                                ai.onGround = true;
							}
							else if (change.Y < 0)
                                ai.Velocity.Y = 0;
							else if (change.X != 0)
                                ai.Velocity.X = 0;
							else if (change.Z != 0)
                                ai.Velocity.Z = 0;
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
				if (ai.InvulnTimer <= 0)
				{
					if (other.group == HitboxManager.Group.PLAYER_DEAL)
					{
						EntityHelper.CalculateKnockback(ref ai.Velocity, other);

						Hurt(other.damage);

                        ai.buffManager.AddBuffs(other.applyBuffs);

                        ai.noticeHandler.OnTakeDamage(other.owner);
					}
				}
			}

			public void Hurt(int damage)
			{
                ai.Health -= damage;

				if (ai.Health <= 0)
				{
                    ai.Health = 0;
					entity.world.EntityManager.Kill(entity);

					if (ai.touchHitbox != -1)
						entity.world.HitboxManager.Remove(ai.touchHitbox);
				}

                ai.InvulnTimer = 0.25f;
			}
        }

        public void OnSave(List<byte> saveBytes)
        {
			SaveHelper.SaveInt32(saveBytes, VERSION);
            SaveHelper.SaveInt32(saveBytes, MaxHealth);
            SaveHelper.SaveFloat32(saveBytes, JumpTimer);
            SaveHelper.SaveFloat32(saveBytes, JumpTime);
            SaveHelper.SaveVector3(saveBytes, jumpDir);
        }

        public void OnLoad(byte[] loadBytes, ref int index)
        {
			int version = SaveHelper.LoadInt32(loadBytes, ref index);

            MaxHealth = SaveHelper.LoadInt32(loadBytes, ref index);
            jumpTimer = SaveHelper.LoadFloat32(loadBytes, ref index);
            jumpTime = SaveHelper.LoadFloat32(loadBytes, ref index);
            jumpDir = SaveHelper.LoadVector3(loadBytes, ref index);
        }

        public void GetSyncedEntity(out SyncedEntity state)
        {
			state = new SyncedEntity
			{
				velocity = Velocity,
				health = Health,
				state = 0,
				timers = { [0] = JumpTimer, [1] = JumpTime, [2] = InvulnTimer },
				counters = { [0] = numJumps, [1] = noticeHandler.Noticed ? 1 : 0},
			};
        }
    }
}
