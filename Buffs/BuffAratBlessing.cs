using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffAratBlessing : Buff
    {
        public BuffAratBlessing() : base("arat_blessing", 999, 0)
        {
            Name = "Arat's Blessing";
            Description = "Blessed by Arat, the God of Peace.\n" +
                "-20% damage\n" +
                "Enemies are less likely to detect you.";

            Tags.Add("lessenemyaggro_med");
            Tags.Add("blessing");
        }
    }
}
