using BrUtility;
using Engine;
using Engine.Clients;
using Engine.Items;
using Engine.Networking;
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
        public ItemSuspiciouslyGlowingSkull() : base("bs_suspiciously_glowing_skull")
        {
            name = "Suspiciously Glowing Skull";
            description = "A skull that emits a faint red glowing light. It's unsettling...\n" +
                "Right click on an altar and something will happen.";
        }

        public override ClientItem ClientInit()
        {
            return new ClientItemSuspiciouslyGlowingSkull(this);
        }

        public override void Hold(Player player, Inventory inventory, int index)
        {
            base.Hold(player, inventory, index);

            player.world.LightManager2.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
            {
                position = player.Position,
                min = Cube.CUBE_SCALE * 4,
                max = Cube.CUBE_SCALE * 16,
                color = Color.Red.ToVector4() * 0.4f,
            });
        }

        public override void EndHold(Player player, Inventory inventory, int newIndex)
        {
            base.EndHold(player, inventory, newIndex);
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            bool valid = base.RightClick(player, inventory, index, facing, out actionStats);

            var lookAtResult = player.GetWorld().Raycast(player.Position, player.Position + facing * Player.INTERACT_DISTANCE,
            (Vector3 pos) =>
            {
                return player.world.ChunkManager.IsInWorldBounds(pos) &&
                    player.world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Touchable;
            });

            if (lookAtResult.hasHit)
            {
                CubePosition pos = CubePosition.FromWorldSpace(lookAtResult.hit);

                Cube cube = player.world.ChunkManager.CubeView.GetCube(pos).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);
                bool a = cube == GlobalState.Registry.CubeRegistry.Get("ancient_altar_placeable");
                bool b = cube == GlobalState.Registry.CubeRegistry.Get("ancient_altar_generated");

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

    public class ClientItemSuspiciouslyGlowingSkull : ClientItem
    {
        public ClientItemSuspiciouslyGlowingSkull(Item item) : base(item, new RectangleF(64, 96, 32, 32))
        {
        }

        public override void Hold(ClientStates client, BasicState player, Inventory inventory, int index)
        {
            base.Hold(client, player, inventory, index);

            client.LightManager.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
            {
                position = player.position,
                min = Cube.CUBE_SCALE * 4,
                max = Cube.CUBE_SCALE * 16,
                color = Color.Red.ToVector4() * 0.4f,
            });
        }
    }
}
