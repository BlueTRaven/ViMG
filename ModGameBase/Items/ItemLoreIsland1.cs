using BrUtility;
using Engine;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemLoreIsland1 : Item
    {
        private static string text = "The Island of Vi: A well-known legend.\n" +
            "Its origins are unknown, but there are references of it dating back to as early as the third age, well over three hundred years ago.\n" +
            "It is simple to describe - if only because those who have seen its surface have never been able to leave, " +
            "and thus, we lack proper description of the island itself.\n" +
            "Its surroundings, however, are easy to supply: the island itself is surrounded by a thick, impenetrable fog, no matter the time " +
            "of day or weather. Rain or shine, thunderstorm or placid sea; this layer of fog is eternal and has never once ceased its vigil.\n" +
            "All those who enter this fog are forever lost.";

        public ItemLoreIsland1() : base("book_lore_island1")
        {
            name = "The Island of Vi: A Mythica";
            description = "(Lore Item)\n" +
                "By Serris of Agaldam";
        }

        public override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(48, 32, 16, 16));
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            player.world.MenuDialogue.StartText(text);
            GlobalState.GameStateManager.TheIsland.PushMenu(player.world.MenuDialogue);

            return base.RightClick(player, inventory, index, facing, out actionStats);
        }
    }
}
