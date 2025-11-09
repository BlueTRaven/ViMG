using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemIronAxe : Item
    {
        private static MeleeAttackStats meleeStats =
            new MeleeAttackStats(new AttackStats(DamageType.Melee, new ActionStats()
            {
                useTime = 1.6f,
                useAnimTime = 20f / 60f,
                preUseTime = 10f / 60f,
            }, 8, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 2.5f);

        public ItemIronAxe() : base("wepaxe_iron", StaticMaterials.Items, new RectangleF(128, 64, 32, 16))
        {
            name = "Iron Axe";
            description = "An axe made of well-crafted iron.\n" +
                meleeStats.GetTooltip();

            flipXInHand = true; 
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            origin = new Vector2(MESH_SIZE * 12f / 16f, MESH_SIZE * 4f / 16f);
            PlayerHelper.MeleeWeaponLeftClick(player, index, meleeStats, out actionStats);
            actionStats.animationType = UseAnimationType.SwingVertical;
            return true;
        }
    }
}
