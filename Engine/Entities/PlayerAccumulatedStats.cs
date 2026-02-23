using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace Engine.Entities
{
    public struct PlayerAccumulatedStats
    {
        public float HPScale;           //% hp increase.
        public int HPFlat;          //flat hp increase. Applied AFTER, unmodified by scale.
        public float MPScale;
        public int MPFlat;
        public float HPRegenTime;
        public int HPRegenAmt;
        public float MPRegenTime;
        public int MPRegenAmt;
        public float MeleeAtkScale; //added to base scale value (1).
        public float RangeAtkScale;
        public float MagicAtkScale;
        public float MeleeAtkFlat;      //flat damage added on top of scale value. Added AFTER - unmodified by scale.
        public float RangeAtkFlat;
        public float MagicAtkFlat;
        public float MeleeSpdScale;
        public float RangeSpdScale;
        public float MagicSpdScale;
        public float MiningScale;   //TODO implement
        public float DefenseScale;  //% defense increase
        public int DefenseFlat;     //flat defense increase. Applied AFTER, unmodified by scale.
        public float KnockbackResist;
        public float Speed;         //Adds to xz max velocity
        public float RunSpeed;
        public float Acceleration;  //Adds to xz accel
        public float JumpSpeed;
        public int JumpNum;
        public float InvulnTime;
        public float UseSpeed;
        public int DashNum;
        public float DashSpeed;

        public IDashEffect DashEffect;

        public IJumpEffect[] JumpEffects;
        private int currentJumpEffectIndex;

        public void AddJumpEffect(IJumpEffect effect)
        {
            if (currentJumpEffectIndex >= 4)
                return;

            JumpEffects[currentJumpEffectIndex++] = effect;
        }

        public static PlayerAccumulatedStats operator +(PlayerAccumulatedStats a, PlayerAccumulatedStats b)
        {
            var stats = new PlayerAccumulatedStats()
            {
                JumpEffects = a.JumpEffects,
                currentJumpEffectIndex = a.currentJumpEffectIndex,
                HPFlat = a.HPFlat + b.HPFlat,
                HPScale = a.HPScale + b.HPScale,
                MPFlat = a.MPFlat + b.MPFlat,
                MPScale = a.MPScale + b.MPScale,
                HPRegenTime = a.HPRegenTime + b.HPRegenTime,
                HPRegenAmt = a.HPRegenAmt + b.HPRegenAmt,
                MPRegenTime = a.MPRegenTime + b.MPRegenTime,
                MPRegenAmt = a.MPRegenAmt + b.MPRegenAmt,
                MiningScale = a.MiningScale + b.MiningScale,
                DefenseScale = a.DefenseScale + b.DefenseScale,
                DefenseFlat = a.DefenseFlat + b.DefenseFlat,
                MeleeAtkScale = a.MeleeAtkScale + b.MeleeAtkScale,
                RangeAtkScale = a.RangeAtkScale + b.RangeAtkScale,
                MagicAtkScale = a.MagicAtkScale + b.MagicAtkScale,
                MeleeAtkFlat = a.MeleeAtkFlat + b.MeleeAtkFlat,
                RangeAtkFlat = a.RangeAtkFlat + b.RangeAtkFlat,
                MagicAtkFlat = a.MagicAtkFlat + b.MagicAtkFlat,
                MeleeSpdScale = a.MeleeSpdScale + b.MeleeSpdScale,
                RangeSpdScale = a.RangeSpdScale + b.RangeSpdScale,
                MagicSpdScale = a.MagicSpdScale + b.MagicSpdScale,
                KnockbackResist = a.KnockbackResist + b.KnockbackResist,
                Speed = a.Speed + b.Speed,
                RunSpeed = a.RunSpeed + b.RunSpeed,
                Acceleration = a.Acceleration + b.Acceleration,
                JumpSpeed = a.JumpSpeed + b.JumpSpeed,
                JumpNum = a.JumpNum + b.JumpNum,
                InvulnTime = a.InvulnTime + b.InvulnTime,
                UseSpeed = a.UseSpeed + b.UseSpeed,
            };

            return stats;
        }
    }
}
