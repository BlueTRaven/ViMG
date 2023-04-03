using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.VertexDeclarations
{
    public static class VertexCasts
    {
        public static List<VertexOpaquePass> ToVertexOpaquePass(this List<VertexCube> cubes)
        {
            List<VertexOpaquePass> opaques = new List<VertexOpaquePass>();

            for (int i = 0; i < cubes.Count; i++)
            {
                opaques.Add(new VertexOpaquePass(cubes[i]));
            }

            return opaques;
        }

        public static List<VertexTransparentPass> ToVertexTransparentPass(this List<VertexCube> cubes)
        {
            List<VertexTransparentPass> transparents = new List<VertexTransparentPass>();

            for (int i = 0; i < cubes.Count; i++)
            {
                transparents.Add(new VertexTransparentPass(cubes[i]));
            }

            return transparents;
        }

        public static List<VertexShadowPass> ToVertexShadowPass(this List<VertexCube> cubes)
        {
            List<VertexShadowPass> shadows = new List<VertexShadowPass>();

            for (int i = 0; i < cubes.Count; i++)
            {
                shadows.Add(new VertexShadowPass(cubes[i]));
            }

            return shadows;
        }

        public static List<VertexEmptyPass> ToVertexEmptyPass(this List<VertexCube> cubes)
        {
            List<VertexEmptyPass> shadows = new List<VertexEmptyPass>();

            for (int i = 0; i < cubes.Count; i++)
            {
                shadows.Add(new VertexEmptyPass(cubes[i]));
            }

            return shadows;
        }
    }
}
