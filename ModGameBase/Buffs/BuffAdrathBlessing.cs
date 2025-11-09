using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffAdrathBlessing : Buff
    {
        public BuffAdrathBlessing() : base("adrath_blessing", 999, 0)
        {
            Name = "Adrath's Blessing";
            Description = "Blessed by Adrath, the God of Water.\n" +
                "While swimming in water:\n" +
                "+100% damage\n" +
                "+50% move speed\n" +
                "(unimplemented)";

            Tags.Add("blessing");
        }
    }
}
