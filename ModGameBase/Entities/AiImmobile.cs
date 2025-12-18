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

namespace ViMG.Entities
{
    public class AiImmobile : ISyncBasicState
    {
        public const int VERSION = 0;

        public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
        public Vector3 Velocity;
        private readonly BuffManager buffManager;
        
        public float InvulnTimer;

        private bool onGround;
        
        public int TouchDamage = 2;
        public int AttackDamage = 4;

        public int Health;
        public int MaxHealth;

        private Rectangle3D touchHitboxBounds;
        private int touchHitbox = -1;

        public AiImmobile(Rectangle3D touchHitboxBounds, BuffManager buffManager, int maxHealth)
        {
            this.buffManager = buffManager;

            this.Health = maxHealth;
            this.MaxHealth = maxHealth;

            this.touchHitboxBounds = touchHitboxBounds;
        }

        public struct Funcs<T> : IHitboxOwner where T : Entity, IHasStats
        {
            public AiImmobile ai;
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
                else entity.world.HitboxManager.Update(ai.touchHitbox, ai.touchHitboxBounds.Offset(entity.Position), ai.InvulnTimer <= 0);

                Vector3 actualMaxVel = ai.MaxVelocity;

                ai.Velocity.Y += World.GRAVITY;

                ai.buffManager.Update(deltaTime);

                if (ai.InvulnTimer <= 0 && ai.onGround)
                {
                    ai.Velocity.X *= 0.85f;
                    ai.Velocity.Z *= 0.85f;
                }

                if (ai.Velocity.Y < -actualMaxVel.Y)
                    ai.Velocity.Y = -actualMaxVel.Y;

                entity.Position += ai.Velocity * (float)deltaTime;

                ai.onGround = false;
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
        }

        public void Get(out BasicState state)
        {
            state = new BasicState
            {
                health = Health,
                velocity = Velocity,
                position = Vector3.Zero,
                rotation = Quaternion.Identity,
                state = 0,
                timers = { [0] = InvulnTimer, },
            };
        }

        public void Set(ref readonly BasicState state)
        {
            Health = state.health;
            Velocity = state.velocity;
            InvulnTimer = state.timers[0];
        }
    }
}
