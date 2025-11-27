using BrUtility;
using Engine.Entities;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemBookWinds : Item
    {
        private static MagicAttackStats magicStats = 
            new MagicAttackStats(new AttackStats(DamageType.Magic, 2f, 0, Cube.CUBE_SCALE * 8), 2);

        public ItemBookWinds() : base("book_spell_winds", new RectangleF(80, 32, 16, 16))
        {
            name = "Spellbook: Winds";
            description = "A spellbook with an explanation of how to cast \"Winds\".\n" +
                "Press LMB to use.\n" +
                magicStats.GetTooltip() +
                "Creates a powerful gust of wind, knocking enemies away.\n" +
                "Magic Cost: 2";

            flipXInHand = true;
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            bool val = base.LeftClick(player, inventory, index, facing, out actionStats);

            if (player.Magic < 2)
            {
                actionStats = new ActionStats();
                return false;
            }

            actionStats = new ActionStats(magicStats.attackStats);
            int damage = magicStats.attackStats.damage;
            float knockback = magicStats.attackStats.knockback;
            player.PerformAttack(DamageType.Magic, ref actionStats, ref damage, ref knockback);

            player.Magic -= 2;
            player.SpawnHitboxLater(index, damage, DamageType.Magic, -(player as IRotatable).ForwardYawOnly, knockback, Cube.CUBE_SCALE * 2f);
            
            return val;
        }
    }
}
