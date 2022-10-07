using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Buffs
{
    public abstract class Buff
    {
        public string Name;
        public string Description;

        protected readonly Player player;
        protected readonly IHasStats stats;
        protected readonly float durationMax;
        protected int stack;
        protected float duration;

        public float Duration => duration;

        public Buff(IHasStats stats, float duration, int stack)
        {
            this.stats = stats;
            this.durationMax = duration;
            this.duration = duration;
            this.stack = stack;
        }

        public Buff(Player player, float duration, int stack)
        {
            this.player = player;
            this.durationMax = duration;
            this.duration = duration;
            this.stack = stack;
        }

        public virtual void Update(double deltaTime, ref Stats stats)
        {
            duration -= (float)deltaTime;

            if (duration <= 0 && stack > 1)
            {
                duration = durationMax;
                stack -= 1;
            }
        }

        public virtual void Update(double deltaTime, ref Player.AccumulatedStats stats)
        {
            duration -= (float)deltaTime;

            if (duration <= 0 && stack > 1)
            {
                duration = durationMax;
                stack -= 1;
            }
        }

        public virtual void OnApplyOfSameType(Buff buff)
        {

        }
    }
}
