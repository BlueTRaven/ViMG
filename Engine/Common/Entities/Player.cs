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
using ViMG.UIs;

namespace Engine.Common.Entities
{
    public class Player : EntityType
    {
        public Player() : base(typeof(ViMG.Player))
        {
        }

        protected override SyncedEntity GetInterpolated(ref readonly SyncedEntity prev, ref readonly SyncedEntity prevInterp, ref readonly SyncedEntity curr, double t)
        {
            var interp = base.GetInterpolated(in prev, in prevInterp, in curr, t);
            var extraPrev = prev.GetExtra<ViMG.Player.PlayerExtraState>();
            var extraPrevI = prevInterp.GetExtra<ViMG.Player.PlayerExtraState>();
            var extraCurr = curr.GetExtra<ViMG.Player.PlayerExtraState>();
            var extraInterp = extraCurr;
            if (extraPrev.useAnimType != extraCurr.useAnimType)
                extraInterp.useAnimTimer = extraInterp.useAnimTime;
            else
            {
                extraInterp.useAnimTimer = EntityRegistry.NetworkLerp(extraPrev.useAnimTimer, extraPrevI.useAnimTimer, extraCurr.useAnimTimer, (float)t, 1.0f / 20.0f);// float.Lerp(extraPrev.useAnimTimer, extraCurr.useAnimTimer, (float)t);
            }

            //if (GlobalState.gameStateManager.GetCurrentGameState().GetCurrentMenu() is not MenuPause)
            //    Console.WriteLine("useAnimTimer: {0:0.0000}. Prev = {1:0.00} curr = {2:0.00} - t = {3:0.000}", extraInterp.useAnimTimer, extraPrevI.useAnimTimer, extraCurr.useAnimTimer, t);

            interp.SetExtra(ref extraInterp);

            interp.rotation = curr.rotation;

            return interp;
        }
    }
}
