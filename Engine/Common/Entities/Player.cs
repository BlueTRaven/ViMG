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

        public override BasicState GetInterpolated(ClientStates client, EntityManager.EntityReference reference)
        {
            var interp = base.GetInterpolated(client, reference);

            var prev = client.Previous(1).entities.GetByRef(ref reference);
            var curr = client.Current().entities.GetByRef(ref reference);
            var extraPrev = prev.GetExtra<ViMG.Player.PlayerExtraState>();
            var extraCurr = curr.GetExtra<ViMG.Player.PlayerExtraState>();
            extraCurr.useAnimTimer = float.Lerp(extraPrev.useAnimTimer, extraCurr.useAnimTimer, (float)Main.TimeC);
            interp.SetExtra(ref extraCurr);

            return interp;
        }
    }
}
