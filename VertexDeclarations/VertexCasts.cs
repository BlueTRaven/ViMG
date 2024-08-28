using BrUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.VertexDeclarations
{
    public static class VertexCasts
    {
        public static unsafe FastList<VertexOpaquePass> ToVertexOpaquePass(this FastList<VertexCube> cubes)
        {
            FastList<VertexOpaquePass> opaques = new FastList<VertexOpaquePass>(cubes.Length);

            for (int i = 0; i < cubes.Length; i++)
            {
                opaques.Add(new VertexOpaquePass(cubes[i]));
            }

            return opaques;
        }
         
        public static unsafe FastList<VertexTransparentPass> ToVertexTransparentPass(this FastList<VertexCube> cubes)
        {
            FastList<VertexTransparentPass> transparents = new FastList<VertexTransparentPass>(cubes.Length);

            for (int i = 0; i < cubes.Length; i++)
            {
                transparents.Add(new VertexTransparentPass(cubes[i]));
            }

            return transparents;
        }

        public static unsafe FastList<VertexShadowPass> ToVertexShadowPass(this FastList<VertexCube> cubes)
        {
            FastList<VertexShadowPass> shadows = new FastList<VertexShadowPass>(cubes.Length);

            for (int i = 0; i < cubes.Length; i++)
            {
                shadows.Add(new VertexShadowPass(cubes[i]));
            }

            return shadows;
        }

        public static unsafe FastList<VertexEmptyPass> ToVertexEmptyPass(this FastList<VertexCube> cubes)
        {
            FastList<VertexEmptyPass> shadows = new FastList<VertexEmptyPass>(cubes.Length);

            for (int i = 0; i < cubes.Length; i++)
            {
                shadows.Add(new VertexEmptyPass(cubes[i]));
            }

            return shadows;
        }
    }
}
