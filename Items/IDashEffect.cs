using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public interface IDashEffect
    {
        //Run when the player begins the dash.
        void StartDash(Player player, ref Vector3 velocity, out Vector3 dashDirection, out float dashDuration, in Player.AccumulatedStats stats);

        //Run while in the dash state.
        void DoDash(Player player, ref Vector3 velocity, ref Vector3 dashDirection, in Player.AccumulatedStats stats);
    }

    public class DefaultDashEffect : IDashEffect
    {
        public void DoDash(Player player, ref Vector3 velocity, ref Vector3 dashDirection, in Player.AccumulatedStats stats)
        {
        }

        public void StartDash(Player player, ref Vector3 velocity, out Vector3 dashDirection, out float dashDuration, in Player.AccumulatedStats stats)
        {
            dashDirection = -Main.camera.ForwardYawOnly;
            dashDuration = 0.2f;

            velocity = Vector3.Normalize(dashDirection) * stats.DashSpeed;
        }
    }
}
