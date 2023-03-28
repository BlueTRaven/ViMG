using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public static class EntityHelper
    {
        public struct DirectionalSourceRect
        {
            public RectangleF front;
            public RectangleF back;
            public RectangleF sides;
            public RectangleF below;
            public RectangleF above;
        }

        public static RectangleF GetEntityDirectionalSourceRect(Vector3 facing, DirectionalSourceRect directionalSourceRect)
        {
            float facingDotCamera = Vector3.Dot(facing, Main.camera.ForwardYawOnly);

            RectangleF sourceRect = directionalSourceRect.front;

            if (facingDotCamera < -0.3f)
            {
                //back
                sourceRect = directionalSourceRect.back;
            }
            else if (facingDotCamera < 0.2f)
            {
                //sides
                sourceRect = directionalSourceRect.sides;

                float leftDot = facing.X * Main.camera.ForwardYawOnly.Z - facing.Z * Main.camera.ForwardYawOnly.X;

                if (leftDot < 0)
                {
                    //if facing to the left, needs to be flipped.
                    sourceRect.x += sourceRect.width;
                    sourceRect.width = -sourceRect.width;
                }
            }

            return sourceRect;
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
            float maxLen = maxVelocity.Length();

            //adding velocity would not put us over the maximum (or would decrease it)
            if ((velocity + addVelocity).Length() < maxLen)
            {
                velocity += addVelocity;
            }
            //adding velocity would decrease length
            else if ((velocity + addVelocity).Length() < velocity.Length())
            {
                velocity += addVelocity;
            }
            //if adding to the current velocity would put us from under to over the maximum
            else if (velocity.Length() < maxLen && (velocity + addVelocity).Length() >= maxLen)
            {
                //just set to the direction
                velocity = Vector3.Normalize(velocity + addVelocity) * maxLen;
            }
        }

        public static void CalculateKnockback(ref Vector3 velocity, HitboxManager.Hitbox other)
        {
            Vector3 direction = Vector3.Normalize(other.direction);
            //knockback shouldn't be allowed to hit enemies down
            if (direction.Y < 0)
                direction.Y = 1;

            Vector3 scaledKnockback = direction * new Vector3(Cube.CUBE_SCALE * 3.2f, Cube.CUBE_SCALE * 6.4f, Cube.CUBE_SCALE * 3.2f);
            scaledKnockback = Vector3.Normalize(scaledKnockback) * (scaledKnockback.Length() + other.knockback);
            velocity = scaledKnockback;
        }
    }
}
