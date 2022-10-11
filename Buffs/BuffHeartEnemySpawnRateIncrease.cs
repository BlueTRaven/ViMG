using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffHeartEnemySpawnRateIncrease : Buff
    {
        //Note that, at the moment, this buff doesn't actually do anything. It doesn't increase spawnrates.
        //We rely on the heart to do that for us instead since it's better at managing things and has better "exit" conditions.
        public BuffHeartEnemySpawnRateIncrease() : base("heart_enemy_spawnrate_increase", 999f, 0f)
        {
            Name = "Mysterious Beating Noise";
            Description = "What is that strange, unsettling noise you hear nearby?\n" +
                "Enemy spawn rates increased...";
        }

        public override void OnApplyOfSameType(BuffInstance instance)
        {
            base.OnApplyOfSameType(instance);

            instance.duration = 1;
        }
    }
}
