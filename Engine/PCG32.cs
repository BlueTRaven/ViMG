using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    // WARNING: ChatGPT generated this! 
    public struct PCG32
    {
        private ulong state;
        private ulong inc;

        public PCG32(ulong seed, ulong sequence = 1)
        {
            state = 0;
            inc = (sequence << 1) | 1;
            NextUInt();
            state += seed;
            NextUInt();
        }

        public void Reseed(ulong seed, ulong sequence = 1)
        {
            state = 0;
            inc = (sequence << 1) | 1;
            NextUInt();
            state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong oldState = state;
            state = oldState * 6364136223846793005UL + inc;

            uint xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rot = (int)(oldState >> 59);

            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }

        public int NextInt(int maxExclusive)
        {
            return (int)(NextUInt() % (uint)maxExclusive);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            return NextInt(maxExclusive - minInclusive) + minInclusive;
        }

        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1.0f / (1u << 24));
        }

        public int NextSign()
        {
            return NextCoinFlip() ? -1 : 1;
        }

        public Vector2 NextAngle()
        {
            return Vector2.Transform(new Vector2(-1, 0), Matrix.CreateRotationZ(MathHelper.ToRadians(NextFloat(0, 360))));
        }

        public Vector2 NextPointInside(Rectangle rectangle)
        {
            float x = NextFloat(rectangle.X, rectangle.X + rectangle.Width);
            float y = NextFloat(rectangle.Y, rectangle.Y + rectangle.Height);

            return new Vector2(x, y);
        }


        public float NextFloat(float minimum, float maximum)
        {
            return NextFloat() * (maximum - minimum) + minimum;
        }

        public bool NextCoinFlip()
        {   //non inclusive, so either 0 or 1
            return NextInt(2) == 0;
        }

        public Vector2 NextInside(Rectangle rectangle)
        {
            return new Vector2(NextFloat(rectangle.X, rectangle.X + rectangle.Width), NextFloat(rectangle.Y, rectangle.Y + rectangle.Height));
        }

        public Vector2 NextInside(in Rectangle rectangle)
        {
            return new Vector2(NextFloat(rectangle.X, rectangle.X + rectangle.Width), NextFloat(rectangle.Y, rectangle.Y + rectangle.Height));
        }

        public Vector2 NextOnEdge(Rectangle rectangle)
        {
            int side = NextInt(4);

            switch (side)
            {
                case 0: //top side
                    return new Vector2(NextInt(rectangle.X, rectangle.X + rectangle.Width), rectangle.Y);
                case 1: //right side
                    return new Vector2(rectangle.X + rectangle.Width, NextInt(rectangle.Y, rectangle.Y + rectangle.Height));
                case 2: //bottom side
                    return new Vector2(NextInt(rectangle.X, rectangle.X + rectangle.Width), rectangle.Y + rectangle.Height);
                case 3: //left side
                    return new Vector2(rectangle.X, NextInt(rectangle.Y, rectangle.Y + rectangle.Height));
            }

            return Vector2.Zero;
        }

        public Vector2 NextInside(RectangleF rectangle)
        {
            return NextInside(rectangle.ToRectangle());
        }

        public Vector2 NextInside(in RectangleF rectangle)
        {
            return NextInside(rectangle);
        }

        public Vector2 NextOnEdge(RectangleF rectangle)
        {
            return NextOnEdge(rectangle.ToRectangle());
        }
    }
}
