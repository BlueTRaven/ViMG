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

            Register(new TestBuff());
            Register(new BuffLeatherGlove());
            Register(new DebuffPoisoned());
            Register(new BuffEmissiveOres(5f * 60f));
            Register(new BuffShimuBlessing());
            Register(new BuffIratBlessing());
            Register(new BuffIratVengeance());
            Register(new BuffAdrathBlessing());
            Register(new BuffAkkatBlessing());
            Register(new BuffGidamuBlessing());
            Register(new BuffAratBlessing());
            Register(new BuffHeartEnemySpawnRateIncrease());
            Register(new DebuffBleeding());
            Register(new BuffWellFed());
        }
    }
}
