using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeSapling : Cube
    {
        public CubeSapling() : base("sapling", 1)
        {
            Transparency = TransparencyValue.Invisible;
        }

        public override ClientCube ClientInit()
        {
            return new(this, new RectangleF(112, 0, 16, 16), Color.White);
        }

        public override void PostChunkGen(WorldPrototype world, CubePosition position)
        {
            base.PostChunkGen(world, position);

            Sapling sapling = new Sapling(position);
            world.AddEntity(sapling);
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
