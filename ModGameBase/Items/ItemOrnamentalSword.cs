using BrUtility;
using Engine.Entities;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemOrnamentalSword : Item
    {
        private static MeleeAttackStats meleeStats =
            new MeleeAttackStats(new AttackStats(DamageType.Melee, new ActionStats()
            {
                useTime = 1.5f,
                useAnimTime = 16f / 60f,
                preUseTime = 10f / 60f,
            }, 8, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 2.5f);

        public ItemOrnamentalSword() : base("sword_ornamental")
        {
            Client = new ClientItem(this, new RectangleF(64, 128, 16, 16));

            name = "Ornamental Sword";
            description = "A large sword that looks fancy but in reality is pretty flimsy.\n" +
                meleeStats.GetTooltip();
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        { 
            base.LeftClick(player, inventory, index, facing, out actionStats);

            actionStats = new ActionStats(meleeStats.attackStats);
            int damage = meleeStats.attackStats.damage;
            float knockback = meleeStats.attackStats.knockback;
            player.PerformAttack(DamageType.Melee, ref actionStats, ref damage, ref knockback);

            player.SpawnHitboxLater(index, damage, DamageType.Melee, -(player as IRotatable).Forward, knockback, meleeStats.range);

            actionStats.animationType = UseAnimationType.SwingHorizontal;

            return true;
        }
    }
}
