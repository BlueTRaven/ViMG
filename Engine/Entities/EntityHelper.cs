using BrUtility;
using Engine.Common;
using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public static class EntityHelper
    {
        public static bool DieIfDaytime(Entity entity)
        {
            if (entity.world.IsDay())
            {
                entity.world.EntityManager.Kill(entity);
                return true;
            }
            return false;
        }

        public static bool UnloadIfDaytime(Entity entity)
        {
            if (entity.world.IsDay())
            {
                entity.world.EntityManager.Unload(entity);
                return true;
            }
            return false;
        }

        public struct DamageTimeOfDayConfig
        {
            public float MinTime;
            public float MaxTime;
            public int DamageAmt;
            public float DamageTime;
            // TODO: debuff
        }

        // NOTE: time is in time-of-day, meaning 0 to 0.5 = day, 0.5 to 1 = night
        // TODO: correctly handle wrapping
        public static bool TakeDamageIfTimeOfDay<T>(T entity, DamageTimeOfDayConfig config, ref float timer, double deltaTime) where T : Entity, IHasStats
        {
            if (timer > 0)
                timer -= (float)deltaTime;

            float normTime = entity.world.GetNormalizedTime();

            if (normTime > config.MinTime && normTime < config.MaxTime)
            {
                if (timer <= 0)
                {
                    TakeDamage(entity, config.DamageAmt);
                    timer += config.DamageTime;
                    return true;
                }
            }
            return false;
        }

        public static void TakeDamage<T>(T entity, int damage) where T : Entity, IHasStats
        {
            var stats = entity.GetStats();
            stats.HP -= damage;

            if (stats.HP <= 0)
            {
                stats.HP = 0;
                entity.world.EntityManager.Kill(entity);
            }
            
            entity.SetStats(stats);
        }

        public struct DirectionalSourceRect
        {
            public RectangleF front;
            public RectangleF back;
            public RectangleF sideLeft;
            public RectangleF sideRight;
            public RectangleF below;
            public RectangleF above;
        }

        public enum DirectionalSide
        {
            Front,
            Back,
            Left,
            Right,
            Top,
            Bottom,
        }

        public static DirectionalSide GetEntityDirectionalSide(Engine.Common.Camera camera, Vector3 facing, DirectionalSourceRect directionalSourceRect)
        {
            Vector2 facingXZ = Vector2.Normalize(facing.XZ());
            Vector2 forwardXZ = Vector2.Normalize(camera.Forward.XZ());

            float ang = float.Acos(Vector2.Dot(facingXZ, forwardXZ));

            DirectionalSide side = DirectionalSide.Front;

            if (ang > MathHelper.ToRadians(180 - 45))
            {
                //back
                side = DirectionalSide.Back;
            }
            else if (ang > MathHelper.ToRadians(45))
            {
                //sides
                side = DirectionalSide.Right;

                float leftDot = Vector2.Dot(facing.XZ(), camera.Right.XZ());

                if (leftDot < 0)
                {
                    side = DirectionalSide.Left;
                }
            }

            return side;
        }

        public static RectangleF GetEntityDirectionalSourceRect(DirectionalSide side, DirectionalSourceRect directionalSourceRect)
        {
            RectangleF sourceRect = directionalSourceRect.front;

            if (side == DirectionalSide.Back)
            {
                //back
                sourceRect = directionalSourceRect.back;
            }
            else if (side == DirectionalSide.Left || side == DirectionalSide.Right)
            {
                //sides
                sourceRect = directionalSourceRect.sideRight;

                if (side == DirectionalSide.Left)
                {
                    if (directionalSourceRect.sideLeft == RectangleF.Empty)
                    {
                        //if facing to the left and we do not have a source rect for the left available, needs to be flipped.
                        sourceRect.x += sourceRect.width;
                        sourceRect.width = -sourceRect.width;
                    }
                    else
                    {
                        sourceRect = directionalSourceRect.sideLeft;
                    }
                }
            }
            else
            {
                // TODO top and bottom
            }

            return sourceRect;
        }

        public static RectangleF GetEntityDirectionalSourceRect(Engine.Common.Camera camera, Vector3 facing, DirectionalSourceRect directionalSourceRect)
        {
            var side = GetEntityDirectionalSide(camera, facing, directionalSourceRect);
            return GetEntityDirectionalSourceRect(side, directionalSourceRect);
        }

        //adds velocity if it would not put the velocity over the velocity cap.
        public static void AddCappedVelocityHorizontal(ref Vector3 velocity, in Vector3 addVelocity, in Vector3 maxVelocity)
        {
            Vector2 velXZ = velocity.XZ();
            float maxVelXZ = maxVelocity.XZ().Length();

            if ((velocity + addVelocity).XZ().Length() > maxVelXZ)
            {
                //not above max velocity; set velocity to max velocity.
                velXZ = Vector2.Normalize((velocity + addVelocity).XZ()) * maxVelXZ;
                velocity = new Vector3(velXZ.X, velocity.Y, velXZ.Y);
            }
            else if (velXZ.Length() > maxVelXZ)
            {
                //already above max velocity
                //in this scenario just subtract some velocity.
                //Note that addVelocity can be in the direction we want to go in (which would add velocity)
                //so we just subtract the length from velocity.
                Vector2 xz = velXZ;
                xz -= Vector2.Normalize(xz) * addVelocity.XZ().Length();
                velocity = new Vector3(xz.X, velocity.Y, xz.Y);
            }
            else
            {
                velocity += addVelocity;
            }

            /*Vector2 currentxz = velocity.XZ();
            Vector2 addingxz = addVelocity.XZ();

            float maxLen = maxVelocity.XZ().Length();

            //adding velocity would not put us over the maximum (or would decrease it)
            if ((currentxz + addingxz).Length() < maxLen)
            {
                velocity.X += addVelocity.X;
                velocity.Z += addVelocity.Z;
            }
            //adding velocity would decrease length
            else if ((currentxz + addingxz).Length() < currentxz.Length())
            {
                velocity.X += addVelocity.X;
                velocity.Z += addVelocity.Z;
            }
            //if adding to the current velocity would put us from under to over the maximum
            else if (currentxz.Length() < maxLen && (currentxz + addingxz).Length() >= maxLen)
            {
                //just set to the direction
                currentxz = Vector2.Normalize(currentxz + addingxz) * maxLen;

                velocity.X = currentxz.X;
                velocity.Z = currentxz.Y;
            }*/
        }

        public static void AddCappedVelocityHorizontal(ref Vector3 velocity, in Vector2 addVelocity, in Vector3 maxVelocity)
        {
            Vector2 velXZ = velocity.XZ();
            float maxVelXZ = maxVelocity.XZ().Length();

            if ((velXZ + addVelocity).Length() > maxVelXZ)
            {
                //not above max velocity; set velocity to max velocity.
                velXZ = Vector2.Normalize(velXZ + addVelocity) * maxVelXZ;
                velocity = new Vector3(velXZ.X, velocity.Y, velXZ.Y);
            }
            else if (velXZ.Length() > maxVelXZ)
            {
                //already above max velocity
                //in this scenario just subtract some velocity.
                Vector2 xz = velXZ;
                xz -= addVelocity;
                velocity = new Vector3(xz.X, velocity.Y, xz.Y);
            }
            else
            {
                velocity += new Vector3(addVelocity.X, 0, addVelocity.Y);
            }
        }

        public static void AddCappedVelocity(ref Vector3 velocity, in Vector3 addVelocity, in Vector3 maxVelocity)
        {
            float maxVelLen = maxVelocity.Length();
            if (velocity.Length() < maxVelLen && (velocity + addVelocity).Length() > maxVelLen)
            {
                //not above max velocity; set velocity to max velocity.
                velocity = (velocity + addVelocity) * maxVelLen;
            }
            else if (velocity.Length() > maxVelLen)
            {
                //already above max velocity
                //in this scenario just subtract some velocity.
                //Note that addVelocity can be in the direction we want to go in (which would add velocity)
                //so we just subtract the length from velocity.
                velocity -= Vector3.Normalize(velocity) * addVelocity.Length();
            }
            else
            {
                velocity += addVelocity;
            }
        }

        public static void CalculateKnockback(ref Vector3 velocity, HitboxManager.Hitbox other, float kbMod = 1)
        {
            Vector3 direction = Vector3.Normalize(other.direction);
            //knockback shouldn't be allowed to hit enemies down
            if (direction.Y < 0)
                direction.Y = 1;

            Vector3 scaledKnockback = direction * new Vector3(Cube.CUBE_SCALE * 3.2f, Cube.CUBE_SCALE * 6.4f, Cube.CUBE_SCALE * 3.2f);
            scaledKnockback = Vector3.Normalize(scaledKnockback) * (scaledKnockback.Length() + other.knockback * kbMod);
            velocity = scaledKnockback;
        }
    }
}
