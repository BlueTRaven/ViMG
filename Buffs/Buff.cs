using System;
using System.Collections.Generic;
using System.Linq;
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

            public bool valid;

            public BuffInstance(Buff buff, float duration = -1)
            {
                this.buff = buff;
                this.duration = duration <= -1 ? buff.durationMax : duration;
                tickInterval = buff.tickIntervalMax;

                valid = true;
            }
        }

        public string Name;
        public string Description;

        protected readonly float durationMax;
        protected readonly float tickIntervalMax;

        public string Identifier { get; private set; }

        public Buff(string identifier, float durationMax, float tickIntervalMax)
        {
            this.Identifier = identifier;
            this.durationMax = durationMax;
            this.tickIntervalMax = tickIntervalMax;
        }

        public virtual void Update(double deltaTime, ref BuffInstance buffInstance, ref Stats stats)
        {
            buffInstance.duration -= (float)deltaTime;
            buffInstance.tickInterval -= (float)deltaTime;

            if (buffInstance.tickInterval <= 0)
            {
                buffInstance.tickInterval += tickIntervalMax;

                Tick(deltaTime, ref buffInstance, ref stats);
            }

            if (buffInstance.duration <= 0)
            {
                buffInstance = new BuffInstance();
            }
        }

        public virtual void Tick(double deltaTime, ref BuffInstance buffInstance, ref Stats stats)
        {

        }

        public virtual void Update(double deltaTime, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {
            buffInstance.duration -= (float)deltaTime;
            buffInstance.tickInterval -= (float)deltaTime;

            if (buffInstance.tickInterval <= 0)
            {
                buffInstance.tickInterval += tickIntervalMax;

                Tick(deltaTime, ref buffInstance, ref stats);
            }

            if (buffInstance.duration <= 0)
            {
                buffInstance = new BuffInstance();
            }
        }

        public virtual void Tick(double deltaTime, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {

        }

        public virtual void OnApplyOfSameType(BuffInstance instance)
        {

        }
    }
}
