using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using BrUtility;
using Engine.Networking;
using Engine;
using ModGameBase.Entities;
using System.Data;

namespace ViMG.Entities
{
    public class AIWalkerMelee : ISyncBasicState
    {
        public const int VERSION = 1;

        public const int ATTACK_TIMER_INDEX = 2;
        public const int INVULN_TIMER_INDEX = 3;

        public enum State
		{
			Paused,
			Normal,
			Attack,
			AttackStun
		}

		public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
		public Vector3 Velocity;
		public Vector3 Facing;
		private readonly NoticeHandler<Player> noticeHandler;
		private readonly BuffManager buffManager;
		private IdleStats idle;

		private State state;

		private float attackTimer;

		public float AttackTimer => attackTimer;

		public float TakeDamageTimer;
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

		public AIWalkerMelee(Rectangle3D touchHitboxBounds, Rectangle3D attackHitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth)
		{
			this.noticeHandler = noticeHandler;
			this.buffManager = buffManager;

			this.Health = maxHealth;
			this.MaxHealth = maxHealth;

			this.touchHitboxBounds = touchHitboxBounds;
            this.attackHitboxBounds = attackHitboxBounds;
        }

		public struct Funcs<T> : IHitboxOwner where T : Entity, IHasStats
		{
			public AIWalkerMelee ai;
			public T entity;

			public void OnUnload()
			{
				if (ai.touchHitbox != -1)
					entity.world.HitboxManager.Remove(ai.touchHitbox);

				if (ai.attackHitbox != -1)
					entity.world.HitboxManager.Remove(ai.attackHitbox);
			}

			public void Update(double deltaTime)
			{
                ai.InvulnTimer -= (float)deltaTime;

				if (ai.touchHitbox == -1)
                    ai.touchHitbox = entity.world.HitboxManager.Add(this, ai.touchHitboxBounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, ai.TouchDamage, 1f, ai.InvulnTimer <= 0);
				else entity.world.HitboxManager.Update(ai.touchHitbox, ai.touchHitboxBounds.Offset(entity.Position).ToOBB(), ai.InvulnTimer <= 0);

				Vector3 actualMaxVel = ai.MaxVelocity;

                ai.Velocity.Y += World.GRAVITY;

                ai.noticeHandler.Update(deltaTime);
                ai.buffManager.Update(deltaTime);

                ai.isInRangeOfTarget = false;

				if (ai.InvulnTimer <= 0 && ai.onGround)
				{
                    ai.shouldJumpLockTimer -= (float)deltaTime;

					if (ai.shouldJump && ai.shouldJumpLockTimer <= 0)
					{
                        ai.Velocity.Y = Cube.CUBE_SCALE * 10;
                        ai.shouldJump = false;
					}

					if (ai.noticeHandler.Noticed)
					{
						ai.idle.idleMovements = 0;

						if (ai.state == State.Paused)
						{
                            //In paused state, slow down and attempt to transition into Normal state.
                            ai.Velocity.X *= 0.95f;
                            ai.Velocity.Z *= 0.95f;

                            ai.state = State.Normal;
						}
						else if (ai.state == State.Normal)
						{
							Vector3 dir = ai.noticeHandler.GetNoticedEntity().Position - entity.Position;
							dir.Normalize();
							dir *= ai.MoveSpeed;

							float distance = XZDistance(ai.noticeHandler.GetNoticedEntity().Position, entity.Position);

							if (distance > ai.MoveTowardsTargetDistance)
							{
								EntityHelper.AddCappedVelocityHorizontal(ref ai.Velocity, dir, actualMaxVel);
                                ai.Facing = Vector3.Normalize(dir);
                            }
                            else
							{
                                ai.Velocity.X *= 0.95f;
                                ai.Velocity.Z *= 0.95f;
							}

							if (distance < ai.AttackTargetDistance)
							{
                                ai.isInRangeOfTarget = true;

                                ai.attackTimer -= (float)deltaTime;

								if (ai.attackTimer <= 0)
								{
                                    ai.attackTimer = ai.AttackLockTime;
                                    ai.state = State.Attack;
								}
							}
						}
						else if (ai.state == State.Attack)
						{
                            ai.isInRangeOfTarget = true;

                            ai.Velocity.X *= 0.95f;
                            ai.Velocity.Z *= 0.95f;

                            ai.attackTimer -= (float)deltaTime;

							if (ai.attackTimer <= 0)
							{
								Vector3 dir = (ai.noticeHandler.GetNoticedEntity().Position - new Vector3(0, Cube.CUBE_SCALE, 0)) - entity.Position;
								if (ai.attackHitbox == -1)
                                    ai.attackHitbox = entity.world.HitboxManager.Add(this, ai.attackHitboxBounds.Offset(entity.Position + Vector3.Normalize(dir) * Cube.CUBE_SCALE * 1.5f),
										Vector3.Normalize(ai.Velocity), HitboxManager.Group.ENEMYHOSTILE_BOTH, ai.AttackDamage, 1);

                                ai.Facing = Vector3.Normalize(dir);

                                ai.state = State.AttackStun;
                                ai.attackTimer = ai.AttackStunTime;
							}
						}
						else if (ai.state == State.AttackStun)
						{
                            ai.isInRangeOfTarget = true;

							if (ai.attackTimer <= ai.AttackStunTime - ai.AttackHitboxTime)
							{
								if (ai.attackHitbox != -1)
								{
									entity.world.HitboxManager.Remove(ai.attackHitbox);
                                    ai.attackHitbox = -1;
								}
							}

                            ai.Velocity.X *= 0.5f;
                            ai.Velocity.Z *= 0.5f;

                            ai.attackTimer -= (float)deltaTime;

							if (ai.attackTimer <= 0)
							{
                                ai.state = State.Normal;
                                ai.attackTimer = ai.AttackCooldownTime;
							}
						}
					}
					else
					{
                        ai.state = State.Normal;
                        ai.attackTimer = ai.AttackCooldownTime;

						ai.idle.Update(entity.random, entity.Position, deltaTime);
                        
						if (ai.idle.idleTimer <= 0)
                        {
                            EntityHelper.AddCappedVelocityHorizontal(ref ai.Velocity, ai.idle.idleDirection, actualMaxVel);
                            ai.Facing = Vector3.Normalize(new Vector3(ai.idle.idleDirection.X, 0, ai.idle.idleDirection.Y));
                        }
                        else
                        {
                            ai.Velocity.X *= 0.85f;
                            ai.Velocity.Z *= 0.85f;
                        }
                    }
				}

				if (ai.Velocity.Y < -actualMaxVel.Y)
                    ai.Velocity.Y = -actualMaxVel.Y;

				entity.Position += ai.Velocity * (float)deltaTime;

                ai.onGround = false;
                ai.shouldJump = false;
				UpdateCollision();

                var ent = this.entity;
                if (entity.world.player.All(x => x == null || (x.Position - ent.Position).Length() > 128 * Cube.CUBE_SCALE))
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

				if (ai.onGround && ai.state == State.Normal && ai.InvulnTimer <= 0 && ai.shouldJumpLockTimer <= 0)
				{
					if (ai.Velocity.Length() > Cube.CUBE_SCALE / 4f)
					{
						var entity = this.entity;
						var ray = entity.world.RaycastVector(entity.Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(ai.Velocity.X, 0, ai.Velocity.Z), Cube.CUBE_SCALE * 1.15f,
							(Vector3 pos) =>
							{
								Cube cube = entity.world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);

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
				EntityHelper.TakeDamage(entity, damage);

                ai.shouldJumpLockTimer = 1f;
                ai.InvulnTimer = 0.25f;
				ai.TakeDamageTimer = 0.125f;

				//interrupt current attack
				if (ai.state == State.Attack || ai.state == State.AttackStun)
                    ai.state = State.Normal;

                ai.attackTimer = 0;    //immediately attempt to attack?
			}

			public State GetState()
			{
				return ai.state;
			}

			public void SetPaused()
			{
                ai.state = State.Paused;
			}

			private static float XZDistance(Vector3 otherPosition, Vector3 position)
			{
				return (new Vector2(otherPosition.X, otherPosition.Z) - new Vector2(position.X, position.Z)).Length();
			}
		}

		public void OnSave(List<byte> saveBytes)
		{
			SaveHelper.SaveInt32(saveBytes, VERSION);
			SaveHelper.SaveInt32(saveBytes, MaxHealth);
        }

		public void OnLoad(byte[] loadBytes, ref int index)
		{
			int version = SaveHelper.LoadInt32(loadBytes, ref index);

			MaxHealth = SaveHelper.LoadInt32(loadBytes, ref index);

			if (version == 0)
			{
				SaveHelper.LoadVector2(loadBytes, ref index);
				SaveHelper.LoadVector2(loadBytes, ref index);
			}
        }

		public void Get(out BasicState state)
        {
			state = new BasicState
			{
				health = Health,
				velocity = Velocity,
				position = Vector3.Zero,
				rotation = EngineMathHelper.DirectionYawOnlyToQuaternion(-Facing, Vector3.Up),
				state = (int)this.state,
				timers = { [ATTACK_TIMER_INDEX] = attackTimer, [INVULN_TIMER_INDEX] = InvulnTimer },
			};
        }

        public void Set(ref readonly BasicState state)
        {
			Health = state.health;
			Velocity = state.velocity;
			this.state = (State)state.state;
            attackTimer = state.timers[2];
            InvulnTimer = state.timers[3];
        }
    }
}
