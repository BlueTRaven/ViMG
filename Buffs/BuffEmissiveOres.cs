using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffEmissiveOres : Buff
    {
        public BuffEmissiveOres(float durationMax) : base("emissive_ores", durationMax, 0)
        {
            Tags.Add("emissive_ores");
        }
    }
}
