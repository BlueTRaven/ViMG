using BepuPhysics.Constraints;
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
using ViMG.Entities;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemRodOfShock : Item
    {
        private MagicAttackStats magicStats = new MagicAttackStats(new AttackStats(DamageType.Magic, 1f, 6, 1f), 3);  //TODO 3 magic use

        public ItemRodOfShock() : base("staff_spell_shock")
        {
            name = "Staff of Shock";
            description = "Delivers a brief shock in a line in front of you. Ouch.\n" +
                magicStats.GetTooltip();
        }

        public override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(112, 96, 16, 16));
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            base.LeftClick(player, inventory, index, facing, out actionStats);

            if (magicStats.CanUse(player))
            {
                player.Magic -= magicStats.magicUse;

                player.world.EntityManager.Add(new AimedLightning(player.Position - (player as IRotatable).Forward * Cube.CUBE_SCALE / 4f + (player as IRotatable).Right * Cube.CUBE_SCALE / 4f, 
                    -(player as IRotatable).Forward, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 10f, Cube.CUBE_SCALE,
                new HitboxManager.HitboxStats()
                {
                    damage = magicStats.attackStats.damage,
                    knockback = magicStats.attackStats.knockback,
                    group = HitboxManager.Group.PLAYER_DEAL
                }));

                actionStats = magicStats.attackStats.actionStats;

                return true;
            }

            return false;
        }
    }
}
