using BrUtility;
using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeGrave : Cube
    {
        public CubeGrave() : base("grave", new CubeFacingLayout(new RectangleF(224, 64, 16, 16), new RectangleF(240, 64, 16, 16)), Color.White, 1)
        {
            Name = "Grave";
            Description = "Not obtainable";
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            if (data != null && (face & MeshHelper.CubeFace.SIDES) > 0 && data.GetId(parameters.position + new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.ChunkSpace)) == Id)
                return new RectangleF(224, 80, 16, 16);
            else return base.GetSourceRect(pass, data, parameters, face);
        }

        private static CubePosition[] adjacents = new CubePosition[6]
        {
            new CubePosition(-1, 0, 0),
            new CubePosition(1, 0, 0),
            new CubePosition(0, -1, 0),
            new CubePosition(0, 1, 0),
            new CubePosition(0, 0, -1),
            new CubePosition(0, 0, 1)
        };

        public override void OnMined(Player player, CubePosition position)
        {
            base.OnMined(player, position);

            int numNearby = 0;

            for (int i = 0; i < 6; i++)
            {
                if (player.world.ChunkManager.CubeView.GetId(position + adjacents[i]) == Id)
                    numNearby++;
            }

            if (numNearby <= 0)
            {
                player.world.EntityManager.Add(new Ghost(position.InWorldSpaceCenter()));
                //spawn ghost entity
            }
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            int valueToDrop = Main.random.Next(5, 25);

            ItemHelper.GetCoins(valueToDrop, out var coinsCopper, out var coinsBronze, out _, out _, out _);
            itemsToDrop.Add(coinsCopper);
            itemsToDrop.Add(coinsBronze);

            if (Main.random.NextFloat() < 0.05f)
            {
                int which = Main.random.Next(0, 3);

                switch (which)
                {
                    case 0:
                        itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("helmet_oldiron"), 1, 0));
                        break;
                    case 1:
                        itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("body_oldiron"), 1,  0));
                        break;
                    case 2:
                        itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("legs_oldiron"), 1, 0));
                        break;
                }
            }
        }
    }
}
