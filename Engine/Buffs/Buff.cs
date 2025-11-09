using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Buffs
{
    public abstract class Buff : IRegisterable
    {
        public struct BuffInstance
        {
            public Buff buff;

            public float duration;
            public float tickInterval;

            public int stack;

            public bool valid;

            public BuffInstance(Buff buff, float duration = -1)
            {
                this.buff = buff;
                this.duration = duration <= -1 ? buff.durationMax : duration;

                stack = 1;
                tickInterval = buff.tickIntervalMax;

                valid = true;
            }

            public BuffInstance(Buff buff, float duration, int stack) : this(buff, duration)
            {
                this.stack = stack;
            }
        }

        public string Name;
        public string Description;

        protected readonly float durationMax;
        protected readonly float tickIntervalMax;
        public readonly Texture2D texture;
        public readonly RectangleF sourceRect;
        public HashSet<string> Tags = new HashSet<string>();

        public string Identifier { get; private set; }

        public Buff(string identifier, float durationMax, float tickIntervalMax, Texture2D texture = null, RectangleF? sourceRect = null)
        {
            this.Identifier = identifier;
            this.durationMax = durationMax;
            this.tickIntervalMax = tickIntervalMax;
            this.texture = texture;
            this.sourceRect = sourceRect ?? new RectangleF(0, 0, 16, 16);
        }

        public virtual void Update(double deltaTime, IBuffManager manager, ref BuffInstance buffInstance, ref Stats stats)
        {
            buffInstance.duration -= (float)deltaTime;
            buffInstance.tickInterval -= (float)deltaTime;

            if (buffInstance.tickInterval <= 0)
            {
                buffInstance.tickInterval += tickIntervalMax;

                Tick(deltaTime, manager, ref buffInstance, ref stats);
            }
        }

        public virtual void Tick(double deltaTime, IBuffManager manager, ref BuffInstance buffInstance, ref Stats stats)
        {

        }

        public virtual void Update(double deltaTime, IBuffManager manager, Player player, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {
            buffInstance.duration -= (float)deltaTime;
            buffInstance.tickInterval -= (float)deltaTime;

            if (buffInstance.tickInterval <= 0)
            {
                buffInstance.tickInterval += tickIntervalMax;

                Tick(deltaTime, manager, player, ref buffInstance, ref stats);
            }

            if (buffInstance.duration <= 0)
            {
                buffInstance.valid = false;
            }
        }

        public virtual void Tick(double deltaTime, IBuffManager manager, Player player, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {

        }

        public virtual void OnTakeDamage(Entity entity, IBuffManager buffManager, int damage, HitboxManager.Hitbox hitbox)
        {

        }

        //Default behavior: increase stack size by 1.
        public virtual void OnApplyOfSameType(BuffInstance instance)
        {
            instance.stack++;
        }
    }
}
