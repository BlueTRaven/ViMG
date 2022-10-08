using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffRegistry : ObjRegistry<Buff>
    {
        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new BuffLeatherGlove());
            Register(new DebuffPoisoned());
        }
    }
}
