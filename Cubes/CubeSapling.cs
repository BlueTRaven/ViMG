using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeSapling : Cube
    {
        public CubeSapling() : base("sapling", new RectangleF(112, 0, 16, 16), Color.White, 1)
        {
            Transparency = TransparencyValue.Invisible;
        }

        public override void PostChunkGen(World world, ChunkManager2 manager, CubePosition position)
        {
            base.PostChunkGen(world, manager, position);

            Sapling sapling = new Sapling(position);
            world.EntityManager.Add(sapling);
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            Sapling sapling = new Sapling(position);
            player.world.EntityManager.Add(sapling);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            //TODO: acorn drop
        }
    }
}
