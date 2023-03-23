using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;

namespace ViMG.Items
{
    public class ItemRustedSword : Item
    {
        private Buff.BuffInstance[] applyBuffs;

        private static MeleeAttackStats meleeStats =
            new MeleeAttackStats(new AttackStats(Player.DamageType.Melee, new Player.ActionStats()
            {
                useTime = 0.85f,
                useAnimTime = 8f / 60f,
                preUseTime = 6f / 60f,
            }, 5, Cube.CUBE_SCALE * 0.5f), Cube.CUBE_SCALE * 1f);

        public ItemRustedSword() : base("sword_rusted", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(32, 128, 16, 16))
        {
            name = "Rusted Sword";
            description = "A rusted and ruined sword made of iron. Perhaps it had once been a fine blade, but it is now a shadow of its former self.\n" +
                meleeStats.GetTooltip() +
                "Hitting enemies applies bleed for 7 seconds.";

            scale = 1f;
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
            base.LeftClick(player, inventory, index, facing, out actionStats);

            if (applyBuffs == null)
            {
                applyBuffs = new Buff.BuffInstance[1] { new Buff.BuffInstance(Main.Registry.BuffRegistry.Get("bleeding"), 7) };
            }

            actionStats = new Player.ActionStats(meleeStats.attackStats);
            int damage = meleeStats.attackStats.damage;
            float knockback = meleeStats.attackStats.knockback;
            player.PerformAttack(Player.DamageType.Melee, ref actionStats, ref damage, ref knockback);

            player.SpawnHitboxLater(index, damage, Player.DamageType.Melee, -Main.camera.Forward, knockback, meleeStats.range, applyBuffs);

            return true;
        }
    }
}
