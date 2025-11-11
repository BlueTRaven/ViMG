using BrUtility;
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
    public class ItemSuspiciouslyGlowingSkull : Item
    {
        private int light = -1;

        public ItemSuspiciouslyGlowingSkull() : base("bs_suspiciously_glowing_skull", new RectangleF(64, 96, 32, 32))
        {
            name = "Suspiciously Glowing Skull";
            description = "A skull that emits a faint red glowing light. It's unsettling...\n" +
                "Right click on an altar and something will happen.";
        }

        public override void Hold(Player player, Inventory inventory, int index)
        {
            base.Hold(player, inventory, index);

            if (light != -1)
            {
                player.GetWorld().LightManager.Remove(light);
                light = -1;
            }

            light = player.GetWorld().LightManager.Add(player.Position, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 8, Color.Red.ToVector4() * 0.4f);
        }

        public override void EndHold(Player player, Inventory inventory, int newIndex)
        {
            base.EndHold(player, inventory, newIndex);

            if (light != -1)
            {
                player.GetWorld().LightManager.Remove(light);
                light = -1;
            }
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            bool valid = base.RightClick(player, inventory, index, facing, out actionStats);

            var lookAtResult = player.GetWorld().Raycast(player.Position, player.Position + facing * Player.INTERACT_DISTANCE,
            (Vector3 pos) =>
            {
                return player.world.ChunkManager.IsInWorldBounds(pos) &&
                    player.world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air).Touchable;
            });

            if (lookAtResult.hasHit)
            {
                CubePosition pos = CubePosition.FromWorldSpace(lookAtResult.hit);

                Cube cube = player.world.ChunkManager.CubeView.GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air);
                bool a = cube == Main.Registry.CubeRegistry.Get("ancient_altar_placeable");
                bool b = cube == Main.Registry.CubeRegistry.Get("ancient_altar_generated");

                if (a || b)
                {
                    inventory.Remove(index, 1);

                    actionStats = new ActionStats(6f);

                    player.world.EntityManager.Add(new Entities.Skullhead(pos.InWorldSpace() - new Vector3(0, Cube.CUBE_SCALE * 16f, 0)));
                }
            }

            return valid;
        }
    }
}
