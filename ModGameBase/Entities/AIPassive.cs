using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using BrUtility;
using SharpDX.MediaFoundation;

namespace ViMG.Entities
{
	public class AIPassive
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

		public AIPassive(Rectangle3D touchHitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth)
		{
			this.noticeHandler = noticeHandler;
			this.buffManager = buffManager;

			this.Health = maxHealth;
			this.MaxHealth = maxHealth;

			this.touchHitboxBounds = touchHitboxBounds;
		}

		public struct Funcs<T> : IHitboxOwner where T : Entity, IHasStats
		{
			public T entity;
			public AIPassive ai;

			public void OnUnload()
			{
				if (ai.touchHitbox != -1)
					entity.world.HitboxManager.Remove(ai.touchHitbox);
			}

			public void Update(double deltaTime)
			{
				ai.InvulnTimer -= (float)deltaTime;

				if (ai.touchHitbox == -1)
					ai.touchHitbox = entity.world.HitboxManager.Add(this, ai.touchHitboxBounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_TAKE, 0, 1f, ai.InvulnTimer <= 0);
				else entity.world.HitboxManager.Update(ai.touchHitbox, ai.touchHitboxBounds.Offset(entity.Position), ai.InvulnTimer <= 0);

				Vector3 actualMaxVel = ai.MaxVelocity;

				ai.Velocity.Y += World.GRAVITY;

				ai.noticeHandler.Update(deltaTime);
                ai.buffManager.Update(deltaTime);

				if (ai.InvulnTimer <= 0 && ai.onGround)
				{
                    ai.shouldJumpLockTimer -= (float)deltaTime;

					if (ai.shouldJump && ai.shouldJumpLockTimer <= 0)
					{
						ai.Velocity.Y = Cube.CUBE_SCALE * 10;
                        ai.shouldJump = false;
					}

					if (ai.state == State.Normal)
					{
                        ai.idleTimer -= (float)deltaTime;

						if (ai.idleTimer <= 0)
                            ai.idleMoveTimer -= (float)deltaTime;

						if (ai.idleMovements == 0 && ai.idleTimer <= 0 && ai.idleMoveTimer <= 0)
						{
                            ai.idleHome = new Vector2(entity.Position.X, entity.Position.Z);

                            ai.idleTimer = entity.random.NextFloat(5f, 12f);
                            ai.idleMoveTimer = entity.random.NextFloat(0.25f, 2f);
                            ai.idleMovements = entity.random.Next(2, 6);

                            ai.idleDirection = entity.random.NextAngle();
						}
						else
						{
							float distFromIdleHome = (new Vector2(entity.Position.X, entity.Position.Z) - ai.idleHome).Length();

							if (distFromIdleHome > Cube.CUBES_PER_UNIT * 16)
                                ai.idleDirection = -ai.idleDirection;

							if (ai.idleTimer <= 0 && ai.idleMoveTimer <= 0)
							{
                                ai.idleMovements--;
                                ai.idleDirection = entity.random.NextAngle();
                                ai.idleMoveTimer = entity.random.NextFloat(0.25f, 2f);
							}
						}

						if (ai.idleTimer <= 0)
						{
							EntityHelper.AddCappedVelocityHorizontal(ref ai.Velocity, ai.idleDirection, actualMaxVel);

                            ai.Facing = Vector3.Normalize(ai.Velocity);
						}
						else
						{
							ai.Velocity.X *= 0.85f;
							ai.Velocity.Z *= 0.85f;
						}
					}
					else
					{
						actualMaxVel = ai.MaxVelocityFleeing;

						Vector3 dir = ai.noticeHandler.Target.Position - entity.Position;
						dir.Normalize();

						EntityHelper.AddCappedVelocityHorizontal(ref ai.Velocity, dir, actualMaxVel);

                        ai.fleeTimer -= (float)deltaTime;

						if (ai.fleeTimer <= 0)
						{
                            ai.state = State.Normal;
                            ai.idleMovements = 0;
                            ai.idleTimer = 0;
						}
					}
				}

				if (ai.Velocity.Y < -actualMaxVel.Y)
					ai.Velocity.Y = -actualMaxVel.Y;

				entity.Position += ai.Velocity * (float)deltaTime;

                ai.onGround = false;
                ai.shouldJump = false;
				UpdateCollision();

                if (entity.world.DistanceFromPlayer(entity.Position) > 128 * Cube.CUBE_SCALE)
                    entity.world.EntityManager.Kill(entity);
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

				if (ai.onGround && ai.InvulnTimer <= 0 && ai.shouldJumpLockTimer <= 0)
				{
					if (ai.Velocity.Length() > Cube.CUBE_SCALE / 4f)
					{
						var entity = this.entity;
						var ray = entity.world.RaycastVector(entity.Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(ai.Velocity.X, 0, ai.Velocity.Z), Cube.CUBE_SCALE * 2,
							(Vector3 pos) =>
							{
								Cube cube = entity.world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air);

								return cube.Collision != Cube.CollisionValue.None;
							});

						if (ray.hasHit)
						{
                            ai.shouldJump = true;
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

				if (ai.InvulnTimer <= 0)
				{
					if (other.group == HitboxManager.Group.PLAYER_DEAL)
					{
						EntityHelper.CalculateKnockback(ref ai.Velocity, other);

                        ai.Health -= other.damage;

						if (ai.Health <= 0)
						{
                            ai.Health = 0;
							entity.world.EntityManager.Kill(entity);

							if (ai.touchHitbox != -1)
								entity.world.HitboxManager.Remove(ai.touchHitbox);
						}

                        ai.shouldJumpLockTimer = 1f;
                        ai.buffManager.AddBuffs(other.applyBuffs);

						ai.InvulnTimer = 0.25f;

                        ai.noticeHandler.OnTakeDamage(other.owner);

						if (ai.state == State.Normal)
						{
                            ai.state = State.Flee;
                            ai.fleeTimer = 6f;
						}
					}
				}
			}

			public State GetState()
			{
				return ai.state;
			}
		}
	}
}
