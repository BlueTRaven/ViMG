using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.GameStates;

namespace ViMG.Cubes
{
    public class CubeCaveRoot : Cube
    {
        public CubeCaveRoot() : base("crop_cave_root", 1, 0)
        {
            Name = "Cave Root";
            Description = "A hardy but bitter tasting tuber. Despite its taste, it alone can sustain a man for many years.";

            //The entity is responsible for drawing this
            Transparency = TransparencyValue.Invisible;

            Client = new(this, new RectangleF(), Color.White);
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return false;
        }

        public override void PostChunkGen(WorldPrototype world, CubePosition position)
        {
            base.PostChunkGen(world, position);

            world.EntityManager.Add(new EntityCaveRoot(position));
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.world.EntityManager.Add(new EntityCaveRoot(position));
        }
    }
}
