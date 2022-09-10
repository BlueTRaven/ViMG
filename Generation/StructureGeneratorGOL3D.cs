using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Generation
{
    //TODO: GOL caves may spawn into the sea which is bad
    public class StructureGeneratorGOL3D : StructureGenerator
    {
        private Cube altarBrickCube;
        private Cube altarCube;
        private Cube stone;

        public StructureGeneratorGOL3D(int seed, ChunkManager chunkManager) : base("GOL3D", seed, chunkManager)
        {
            altarBrickCube = Main.Registry.CubeRegistry.Get("altar_brick");
            altarCube = Main.Registry.CubeRegistry.Get("ancient_altar");
            stone = Main.Registry.CubeRegistry.Get("stone");
        }

        protected override Structure[] GenerateOne(ref StructureTaskState state)
        {
            int range = state.sliceEnd - state.sliceStart;

            Structure[] structures = new Structure[range];

            for (int i = 0; i < range; i++)
            {
                int width = state.random.Next(16, 64);
                int height = state.random.Next(16, 64);
                int depth = state.random.Next(16, 64);

                GOL3DSim sim = new GOL3DSim(state.random, width, height, depth, 15, 0.4f, 13, 10);
                sim.DoSim();

                ushort[] data = new ushort[width * height * depth];

                for (int j = 0; j < data.Length; j++)
                {
                    //We might place tiles outside of j, so we need this check to not overwrite those.
                    if (data[j] != 0)
                        continue;

                    Util.OneDToThreeD(j, new ValuePoint3D(width, height, depth), out ValuePoint3D point);
                    bool p = sim.Get(point.x, point.y, point.z);//[point.x][point.y][point.z];

                    if (!p)
                        data[j] = 0;
                    else
                    {
                        data[j] = stone.Id;

                        //Check to see if our point has an empty space above.
                        if (point.y + 1 < height && !sim.Get(point.x, point.y + 1, point.z))
                        {
                            double placeAltarBrick = state.random.NextDouble();
                            //If it does, maybe place an altar brick on the space below it
                            if (placeAltarBrick < 0.25)
                            {
                                //This will overwrite the stone we just placed there
                                data[j] = altarBrickCube.Id;

                                double placeAltar = state.random.NextDouble();
                                //If the brick was placed, maybe place an altar on the empty space above it
                                if (placeAltar < 0.25)
                                {
                                    Util.ThreeDToOneD(new ValuePoint3D(point.x, point.y + 1, point.z), new ValuePoint3D(width, height, depth), out int upOne);
                                    data[upOne] = altarCube.Id;
                                }
                            }
                        }
                    }
                }

                structures[i] = new Structure(new Point3D(width, height, depth), data);
            }

            return structures;
        }
    }
}
