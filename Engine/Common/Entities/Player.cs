using Engine.Clients;
using Engine.Entities;
using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace Engine.Common.Entities
{
    public class Player : EntityType
    {
        public Player() : base(typeof(ViMG.Player))
        {
        }

        protected override BasicState GetInterpolated(ref readonly BasicState a, ref readonly BasicState b, double t)
        {
            var interp = base.GetInterpolated(in a, in b, t);
            var extraPrev = a.GetExtra<ViMG.Player.PlayerExtraState>();
            var extraCurr = b.GetExtra<ViMG.Player.PlayerExtraState>();
            extraCurr.useAnimTimer = float.Lerp(extraPrev.useAnimTimer, extraCurr.useAnimTimer, (float)t);
            interp.SetExtra(ref extraCurr);

            interp.rotation = b.rotation;

            return interp;
        }
    }
}
