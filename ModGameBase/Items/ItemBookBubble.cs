using BrUtility;
using Engine;
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
    public class ItemBookBubble : Item
    {
        private static MagicAttackStats magicStats = new MagicAttackStats(new AttackStats(DamageType.Magic, 1f, 8, 8), 5);

        public ItemBookBubble() : base("book_spell_bubble")
        {
            name = "Spellbook: Bubble";
            description = "A spellbook with an explanation of how to cast \"Bubble\".\n" +
                "Press LMB to use.\n" +
                magicStats.GetTooltip() +
                "Creates a floating bubble. Enemies that touch this bubble will cause it to explode and deal heavy damage.\n";
        }

        public override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(64, 48, 16, 16), flipXInHand: true);
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            bool valid = base.LeftClick(player, inventory, index, facing, out actionStats);

            if (magicStats.CanUse(player))
            {
                var lookAtResult = player.GetWorld().Raycast(player.Position, player.Position - (player as IRotatable).Forward * Player.INTERACT_DISTANCE,
                (Vector3 pos) =>
                {
                    //TODO check solidity, not id != 0
                    return player.GetWorld().ChunkManager.IsInWorldBounds(pos) && 
                        player.GetWorld().ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Solid;
                });

                Vector3 hitPos = lookAtResult.hasHit ? lookAtResult.hit : lookAtResult.end;
                Vector3 placeOffset = lookAtResult.hasHit ? CubePosition.ToWorldSpaceV3(lookAtResult.normal) * 2f : Vector3.Zero;

                int damage = magicStats.attackStats.damage;
                float knockback = magicStats.attackStats.knockback;
                player.PerformAttack(DamageType.Magic, ref actionStats, ref damage, ref knockback);
                player.GetWorld().EntityManager.Add(new PlayerBubble(hitPos + placeOffset, damage, knockback, index));

                magicStats.Use(player);

                return true;
            }

            actionStats = new ActionStats();
            return false;
        }
    }
}
