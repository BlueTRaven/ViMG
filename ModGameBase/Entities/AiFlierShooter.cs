using Engine;
using Engine.Networking;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;
using static HexaGen.Runtime.MemoryPool;

namespace ModGameBase.Entities
{
    public class AiFlierShooter
    {
        public struct ShootConfig
        {
            public bool shootsBatch;
            public ProjectileManager.ProjectileStats stats;
            public ProjectileManager.ProjectileBatchStats batchStats;
            public int visStatsId;

            public void Shoot(ProjectileManager manager, IHitboxOwner owner, Vector3 position, Vector3 velocity)
            {
                if (!shootsBatch)
                {
                    manager.Add(new ProjectileManager.Projectile(owner, position, velocity, 8, visStatsId, stats));
                }
                else
                {
                    manager.AddBatch(owner, position, velocity, 8, batchStats, visStatsId, stats);
                }
            }
        }

        private const int VERSION = 0;

        public enum State
        {
            Normal,         //walking/idling/moving towards player/etc
            Attack,         //attacking player
            AttackStun,     //stun after attacking player
            Stun
        }

        private State state;

        public float InvulnTimer;

        public Vector3 Velocity;
        public float MaxVelocity = Cube.CUBE_SCALE * 6;

        public float Acceleration = Cube.CUBE_SCALE / 2f;
        public float TurnSpeed = MathHelper.ToRadians(5f);

        public bool CollidesWithWorld = true;

        public float MoveTowardsTargetDistance = Cube.CUBE_SCALE * 2f;
        public float AttackTargetDistance = Cube.CUBE_SCALE * 2f;

        public float AttackStunTime = 1.65f;    //the amount of time the ai waits in the attacking state after attacking before returning to the normal state
        public float AttackCooldownTime = 2f;   //the amount of time before the ai can enter the attacking state again
        public float AttackLockTime = 0.25f;    //the amount of time the ai spends in the attack state before it can begin moving again.
        private float attackTimer;

        private IdleStats idle;

        public float AttackTimer => attackTimer;

        public int Health;
        public int MaxHealth = 10;

        public int TouchDamage = 2;
        private Rectangle3D touchHitboxBounds;
        private int touchHitbox = -1;

        public float ShootSpeed = Cube.CUBE_SCALE * 16;
        private readonly ShootConfig shootConfig;

        public Vector3 Facing;

        private readonly BuffManager buffManager;
        private readonly NoticeHandler<Player> noticeHandler;
        private readonly World world;

        public AiFlierShooter(World world, Rectangle3D touchHitboxBounds, NoticeHandler<Player> noticeHandler, BuffManager buffManager, int maxHealth,
            ShootConfig shootConfig)
        {
            this.noticeHandler = noticeHandler;
            this.buffManager = buffManager;
            this.world = world;
            this.touchHitboxBounds = touchHitboxBounds;

            this.Health = maxHealth;
            this.MaxHealth = maxHealth;

            this.shootConfig = shootConfig;
        }

        public struct Funcs<T> : IHitboxOwner where T : Entity, IHasStats
        {
            public AiFlierShooter ai;
            public T entity;

            public void OnUnload()
            {
                if (ai.touchHitbox != -1)
                    entity.world.HitboxManager.Remove(ai.touchHitbox);
            }

            public void Update(double deltaTime)
            {
                ai.InvulnTimer -= (float)deltaTime;

                if (ai.touchHitbox == -1)
                    ai.touchHitbox = entity.world.HitboxManager.Add(this, ai.touchHitboxBounds.Offset(entity.Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, ai.TouchDamage, 1f, ai.InvulnTimer <= 0);
                else entity.world.HitboxManager.Update(ai.touchHitbox, ai.touchHitboxBounds.Offset(entity.Position).ToOBB(), ai.InvulnTimer <= 0);

                Vector3 actualMaxVel = new Vector3(ai.MaxVelocity);

                ai.buffManager.Update(deltaTime);
                ai.noticeHandler.Update(deltaTime);

                if (ai.noticeHandler.Noticed)
                {
                    Vector3 playerDir = ai.noticeHandler.GetNoticedEntity().Position - entity.Position;
                    float distance = playerDir.Length();
                    playerDir.Normalize();

                    if (ai.state == State.Normal)
                    {
                        if (distance > ai.MoveTowardsTargetDistance)
                        {
                            //this guy might have a bit more complicated of a velocity calculation since it uses all 3 axes
                            if (ai.Velocity.Length() > 0)
                            {
                                Vector3 velocityDir = Vector3.Normalize(ai.Velocity);
                                float velocityLen = ai.Velocity.Length();

                                //Instead of simply adding/moving in a given direction, we instead attempt to rotate our movement velocity.
                                //This leads to more interesting movement patterns - in general, strafing has more of an effect this way.
                                Vector3 cross = Vector3.Cross(velocityDir, playerDir);
                                Matrix mat = Matrix.CreateFromAxisAngle(cross, ai.TurnSpeed);

                                Vector3 rotated = Vector3.Normalize(Vector3.Transform(velocityDir, mat));

                                if (velocityLen + ai.Acceleration > ai.MaxVelocity)
                                    ai.Velocity = rotated * (velocityLen - ai.Acceleration);
                                else ai.Velocity = rotated * (velocityLen + ai.Acceleration);
                            }
                            else
                            {
                                //Initial first acceleration (we start with 0 velocity, and that results in NaNs, so we kinda have to seed it)
                                ai.Velocity += playerDir * ai.Acceleration;
                            }

                            ai.Facing = Vector3.Normalize(ai.Velocity);
                        }
                        else
                        {
                            //slow down very fast.
                            ai.Velocity *= 0.65f;

                            ai.Facing = Vector3.Normalize(ai.Velocity);

                            if (distance < ai.AttackTargetDistance)
                            {
                                ai.Facing = playerDir;

                                ai.attackTimer -= (float)deltaTime;

                                if (ai.attackTimer <= 0)
                                {
                                    ai.attackTimer = ai.AttackLockTime;
                                    ai.state = State.Attack;
                                }
                            }
                        }
                    }
                    else if (ai.state == State.Attack)
                    {
                        ai.Velocity *= 0.95f;

                        ai.attackTimer -= (float)deltaTime;

                        if (ai.attackTimer <= 0)
                        {
                            Vector3 dir = (ai.noticeHandler.GetNoticedEntity().Position - new Vector3(0, Cube.CUBE_SCALE, 0)) - entity.Position;

                            ai.shootConfig.Shoot(ai.world.ProjectileManager, this, entity.Position + new Vector3(0, Cube.CUBE_SCALE, 0), Vector3.Normalize(dir) * ai.ShootSpeed);
                            
                            ai.Facing = Vector3.Normalize(dir);

                            ai.state = State.AttackStun;
                            ai.attackTimer = ai.AttackStunTime;
                        }
                    }
                    else if (ai.state == State.AttackStun)
                    {
                        ai.Velocity *= 0.95f;

                        ai.attackTimer -= (float)deltaTime;

                        if (ai.attackTimer <= 0)
                        {
                            ai.state = State.Normal;
                            ai.attackTimer = ai.AttackCooldownTime;
                        }
                    }
                    else if (ai.state == State.Stun)
                    {
                        if (ai.InvulnTimer <= 0)
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

                /*if (ai.Velocity.Length() > ai.MaxVelocity)
                    ai.Velocity = Vector3.Normalize(ai.Velocity) * ai.MaxVelocity;*/

                entity.Position += ai.Velocity * (float)deltaTime;

                if (ai.CollidesWithWorld)
                    UpdateCollision();

                // TODO MULTIPLAYER REFACTOR
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

                    if (GlobalState.Registry.CubeRegistry.GetOrDefault(id, GlobalState.Registry.CubeRegistry.Air).Solid)
                    {
                        Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

                        Vector3 offset = new Vector3(0, Cube.CUBE_SCALE * 0.25f, 0);
                        Vector3 checkPos = entity.Position + offset;

                        if (CollisionHelper.CheckCollision(cubeBounds, checkPos, Cube.CUBE_SCALE * 0.25f, out Vector3 change))
                        {
                            entity.Position = (checkPos - offset) + change;

                            if (change.Y != 0)
                                ai.Velocity.Y = -ai.Velocity.Y * 0.5f;
                            else if (change.X != 0)
                                ai.Velocity.X = -ai.Velocity.X * 0.5f;
                            else if (change.Z != 0)
                                ai.Velocity.Z = -ai.Velocity.Z * 0.5f;
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
                            ai.Velocity = -ai.Velocity;
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
                    else if ((us.group & HitboxManager.Group.ENEMYHOSTILE_DEAL) == HitboxManager.Group.ENEMYHOSTILE_DEAL && other.group == HitboxManager.Group.PLAYER_TAKE &&
                        other.canInteract)
                    {
                        //Bounce off the player if we deal contact damage to them
                        ai.Velocity = -ai.Velocity;
                    }
                }
            }

            public void Hurt(int damage)
            {
                EntityHelper.TakeDamage(entity, damage);

                ai.InvulnTimer = 0.25f;

                //interrupt current attack
                if (ai.state == State.Attack || ai.state == State.AttackStun)
                    ai.state = State.Normal;

                ai.attackTimer = 0;    //immediately attempt to attack?
            }

            public State GetState()
            {
                return ai.state;
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
        }

        public void Get(out SyncedEntity state)
        {
            state = new SyncedEntity
            {
                health = Health,
                velocity = Velocity,
                position = Vector3.Zero,
                rotation = Quaternion.Identity,
                state = (int)this.state,
                timers = { [2] = attackTimer, [3] = InvulnTimer },
            };
        }
    }
}
