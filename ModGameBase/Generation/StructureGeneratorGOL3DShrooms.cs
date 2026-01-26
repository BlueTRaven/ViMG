using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using Engine;

namespace ViMG.Generation
{
    public class StructureGeneratorGOL3DShrooms : StructureGenerator
    {
        private ref struct PositionId
        {
            public ValuePoint3D point;
            public ushort id;
        }

        private const float BIGMUSHROOM_CHANCE = 1f / 40f;
        private const float SMALLMUSHROOM_CHANCE = 1f / 25f;
        private const int RADIUS = 2;
        private const int STRIDE = (RADIUS * 2 + 1) - 2;
        private const int TOTAL = STRIDE * STRIDE;

        private Cube stone;
        private Cube mushroomStem;
        private Cube stoneCoveredOrange;
        private Cube stoneCoveredPurple;
        /*private Cube mushroomOrangeTop;
        private Cube mushroomOrangeSmall;
        private Cube mushroomPurpleTop;
        private Cube mushroomPurpleSmall;*/

        public StructureGeneratorGOL3DShrooms(int seed, ChunkManager chunkManager) : base("GOL3D Shrooms", seed, chunkManager)
        {
            stone = GlobalState.Registry.CubeRegistry.Get("stone");
            mushroomStem = GlobalState.Registry.CubeRegistry.Get("mushroom_stem");
            stoneCoveredOrange = GlobalState.Registry.CubeRegistry.Get("stone_covered_orange");
            stoneCoveredPurple = GlobalState.Registry.CubeRegistry.Get("stone_covered_purple");
            /*mushroomOrangeTop = GlobalState.Registry.CubeRegistry.Get("mushroom_orange_top");
            mushroomOrangeSmall = GlobalState.Registry.CubeRegistry.Get("mushroom_orange_small");
            mushroomPurpleTop = GlobalState.Registry.CubeRegistry.Get("mushroom_purple_top");
            mushroomPurpleSmall = GlobalState.Registry.CubeRegistry.Get("mushroom_purple_small");*/
        }

        protected unsafe override Structure[] GenerateOne(ref StructureTaskState state)
        {
            int range = state.sliceEnd - state.sliceStart;

            Structure[] structures = new Structure[range];

            PositionId* points = stackalloc PositionId[8 + TOTAL];

            for (int i = 0; i < range; i++)
            {
                bool mcolor = state.random.NextCoinFlip();

                Cube covered = mcolor ? stoneCoveredOrange : stoneCoveredPurple;
                /*Cube mushroomSmall = mcolor ? mushroomOrangeSmall : mushroomPurpleSmall;
                Cube mushroomTop = mcolor ? mushroomOrangeTop : mushroomPurpleTop;*/

                int width = state.random.Next(16, 40);
                int height = state.random.Next(16, 40);
                int depth = state.random.Next(16, 40);

                GOL3DSim sim = new GOL3DSim(state.random, width, height, depth, 10, 0.4f, 13, 10);
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

                        //check to see if there is an empty space above.
                        if (point.y + 1 < height && !sim.Get(point.x, point.y + 1, point.z))
                        {
                            //If there is, place covered stone at a high chance.
                            float placeStoneCovered = state.random.NextFloat();
                            if (placeStoneCovered < 0.94f)
                            {
                                //This will overwrite the stone we just placed there
                                data[j] = covered.Id;

                                /*double placeMushroom = state.random.NextDouble();
                                //If the stone was placed, maybe place a mushroom on the empty space above it
                                if (placeMushroom < BIGMUSHROOM_CHANCE)
                                {
                                    bool shouldPlace = true;
                                    int shroomSize = state.random.Next(3, 8);

                                    int pmax = 0;

                                    //first check upwards to see if there's enough room
                                    //(note we start at 1 so we don't start in the stone block.)
                                    for (int k = 1; k <= shroomSize; k++)
                                    {
                                        if (point.y + k >= height || sim.Get(point.x, point.y + k, point.z))
                                        {
                                            shouldPlace = false;
                                            goto outsideLoop;
                                        }

                                        if (k == shroomSize)
                                        {
                                            for (int kx = -RADIUS; kx <= RADIUS; kx++)
                                            {
                                                for (int kz = -RADIUS; kz <= RADIUS; kz++)
                                                {
                                                    ValuePoint3D kp = new ValuePoint3D(point.x + kx, point.y + k, point.z + kz);

                                                    int len = (int)MathF.Sqrt((kx * kx) + (kz * kz));

                                                    if (len < RADIUS)
                                                    {
                                                        points[pmax++] = new PositionId() { point = kp, id = mushroomTop.Id };
                                                        //if outside of bounds OR there's a block placed by the sim...
                                                        if (kp.x < 0 || kp.x >= width || kp.z < 0 || kp.z >= depth ||
                                                            sim.Get(kp.x, kp.y, kp.z))
                                                        {
                                                            //can't place the shroom
                                                            shouldPlace = false;
                                                            goto outsideLoop;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else points[pmax++] = new PositionId() { point = new ValuePoint3D(point.x, point.y + k, point.z), id = mushroomStem.Id };
                                    }

                                outsideLoop:
                                    if (shouldPlace)
                                    {
                                        for (int pi = 0; pi < pmax; pi++)
                                        {
                                            Util.ThreeDToOneD(points[pi].point, new ValuePoint3D(width, height, depth), out int pi2d);
                                            data[pi2d] = points[pi].id;
                                        }
                                    }
                                }
                                else if (placeMushroom >= BIGMUSHROOM_CHANCE && placeMushroom < BIGMUSHROOM_CHANCE + SMALLMUSHROOM_CHANCE)
                                {
                                    Util.ThreeDToOneD(new ValuePoint3D(point.x, point.y + 1, point.z), new ValuePoint3D(width, height, depth), out int pi);

                                    data[pi] = mushroomSmall.Id;
                                }*/
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
