using BrUtility;
using Engine;
using Engine.Items;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemBoneWhistle : Item
    {
        public ItemBoneWhistle() : base("bone_whistle")
        {
            Client = new ClientItem(this, new RectangleF(80, 80, 16, 16));

            name = "Bone Whistle";
            description = "A whistle carved of bone.\n" +
                "+5 defense\n" +
                "Skeletons and other weak creatures of bone will no longer attack you.";

            Tags.Add("accessory");
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.DefenseScale += 0.05f;
        }

        public static bool HasBoneWhistle(Player player)
        {
            var accessoryInventory = player.world.InventoryManager.Get(player.accessoryInventory);
            return accessoryInventory.Find(GlobalState.Registry.ItemRegistry.Get("bone_whistle")).valid;
        }
    }
}
