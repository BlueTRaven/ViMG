using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Cubes
{
    public class CubeBonfire : Cube
    {
        public CubeBonfire() : base("bonfire", new RectangleF(240, 32, 32, 32), Color.White, 4, 0)
        {
            Transparency = TransparencyValue.Transparent | TransparencyValue.InvisibleOnDepth;

            Name = "Bonfire";
            Description = "You shouldn't really have this in your inventory...";
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.GetWorld().EntityManager.Add(new EntityCubeBonfire(position));
        }

        public override CubeAnimation GetAnimation(MeshHelper.CubeFace face, RenderPass pass, World world, CubePosition pos)
        {
            return new CubeAnimation(0.125f, 4, 32);
        }

        public override void MakeVerts(RenderPass pass, World world, Vector3 pos, Vector3 min, Vector3 max, CubeVisualInstance visual, List<VertexCube> vertices, List<int> indices)
        {
            if (pass != RenderPass.Opaque)
                return;

            DrawHelper3D.MakeXMeshVerts(pass, this, world, pos + new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f), new Vector3(2), vertices, indices);
        }
    }
}
