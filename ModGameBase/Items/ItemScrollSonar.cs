using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemScrollSonar : Item
    {
        public ItemScrollSonar() : base("scroll_sonar", new RectangleF(112, 32, 16, 16))
        {
            name = "Scroll: Void";
            description = "Locates nearby empty spaces.\n" +
                "Magic Use: 5\n" +
                "Consumed on use.";
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            if (player.Magic >= 5)
            {
                player.world.EntityManager.Add(new EntitySonarTracker(player.Position));

                player.Magic -= 5;

                inventory.Remove(index, 1);

                actionStats = new ActionStats(0.5f);

                return true;
            }

            actionStats = new ActionStats();
            return false;
        }
    }
}
