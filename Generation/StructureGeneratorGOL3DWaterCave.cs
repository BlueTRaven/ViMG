using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Generation
{
    public class StructureGeneratorGOL3DWaterCave : StructureGenerator
    {
        private static Cube water;
        private static Cube stone;

        public StructureGeneratorGOL3DWaterCave(int seed, ChunkManager chunkManager) : base("GOL3DWaterCave", seed, chunkManager)
        {
            water = Main.Registry.CubeRegistry.Get("water");
            stone = Main.Registry.CubeRegistry.Get("stone");
        }

        protected override Structure[] GenerateOne(ref StructureTaskState state)
        {
            int range = state.sliceEnd - state.sliceStart;

            Structure[] structures = new Structure[range];

            for (int i = 0; i < range; i++)
            {
                int width = state.random.Next(8, 32);
                int height = state.random.Next(8, 32);
                int depth = state.random.Next(8, 32);

                GOL3DSim sim = new GOL3DSim(state.random, width, height, depth, 4, 0.4f, 13, 10);
                sim.DoSim();

                ushort[] data = new ushort[width * height * depth];

                Point3D[] empties = new Point3D[data.Length];
                int lastEmpty = 0;

                for (int j = 0; j < data.Length; j++)
                {
                    //We might place tiles outside of j, so we need this check to not overwrite those.
                    if (data[j] != 0)
                        continue;

                    Util.OneDToThreeD(j, new ValuePoint3D(width, height, depth), out ValuePoint3D point);
                    bool p = sim.Get(point.x, point.y, point.z);

                    if (!p)
                    {
                        data[j] = 0;
                        empties[lastEmpty++] = new Point3D(point.x, point.y, point.z);
                    }
                    else
                    {
                        data[j] = stone.Id;
                    }
                }

                if (lastEmpty > 0)
                {
                    Point3D randomEmpty = empties[state.random.Next(0, lastEmpty)];

                    Util.ThreeDToOneD(new ValuePoint3D(randomEmpty), new ValuePoint3D(width, height, depth), out int wi);
                    //data[wi] = water.Id;

                    //perform a flood fill
                    //TODO: currently we flood-fill only on the x+-, z+-, and y- axes. This might produce inaccurate results.
                    //It might be better to do y+-, and just say "y < randomEmpty.Y not inside".
                    //TODO: this should be moved to world pasting code.
                    Queue<Point3D> waterFloodFills = new Queue<Point3D>();
                    waterFloodFills.Enqueue(randomEmpty);

                    while (waterFloodFills.Count > 0)
                    {
                        Point3D n = waterFloodFills.Dequeue();

                        if (n.X >= 0 && n.X < width && n.Y >= 0 && n.Y < height && n.Z >= 0 && n.Z < depth)
                        {
                            Util.ThreeDToOneD(new ValuePoint3D(n), new ValuePoint3D(width, height, depth), out wi);

                            if (data[wi] == 0)
                            {
                                data[wi] = water.Id;
                                waterFloodFills.Enqueue(new Point3D(n.X - 1, n.Y, n.Z));
                                waterFloodFills.Enqueue(new Point3D(n.X + 1, n.Y, n.Z));
                                waterFloodFills.Enqueue(new Point3D(n.X, n.Y - 1, n.Z));
                                waterFloodFills.Enqueue(new Point3D(n.X, n.Y, n.Z - 1));
                                waterFloodFills.Enqueue(new Point3D(n.X, n.Y, n.Z + 1));
                            }
                        }
                    }
                }
                else i--;   //decrement iterator; causes it to try to generate again

                structures[i] = new Structure(new Point3D(width, height, depth), data);
            }

            return structures;
        }

        public static void PlaceInWorld(ChunkManager manager, Structure structure, CubePosition pos)
        {
            HashSet<CubePosition> touchedPositions = new HashSet<CubePosition>();
            Queue<CubePosition> waterFloodFills = new Queue<CubePosition>();
            List<CubePosition> actualFills = new List<CubePosition>();

            CubePosition seedPos = new CubePosition();

            for (int i = 0; i < structure.data.Length; i++)
            {
                if (structure.data[i] == water.Id)
                {
                    Util.OneDToThreeD(i, new ValuePoint3D(structure.size.X, structure.size.Y, structure.size.Z), out ValuePoint3D spos);
                    seedPos = new CubePosition(spos.x, spos.y, spos.z) + pos;
                    waterFloodFills.Enqueue(seedPos);
                    touchedPositions.Add(seedPos);
                }
            }

            bool placeWater = true;

            int numTries = Chunk.NUM_CUBES_IN_CHUNK * 8;
            while (waterFloodFills.Count > 0)
            {
                CubePosition waterPosition = waterFloodFills.Dequeue();

                if (manager.IsInWorldBounds(waterPosition) && !touchedPositions.Contains(waterPosition) && waterPosition.Y <= seedPos.Y)
                {
                    if (waterPosition.Y < 40)
                    {
                        placeWater = false;
                        break;
                    }

                    if (manager.InitializerView.GetCube(waterPosition).GetOrDefault(Main.Registry.CubeRegistry.Air) == Main.Registry.CubeRegistry.Air)
                    {
                        actualFills.Add(waterPosition);
                        touchedPositions.Add(waterPosition);

                        waterFloodFills.Enqueue(new CubePosition(waterPosition.X - 1, waterPosition.Y, waterPosition.Z));
                        waterFloodFills.Enqueue(new CubePosition(waterPosition.X + 1, waterPosition.Y, waterPosition.Z));
                        waterFloodFills.Enqueue(new CubePosition(waterPosition.X, waterPosition.Y - 1, waterPosition.Z));
                        waterFloodFills.Enqueue(new CubePosition(waterPosition.X, waterPosition.Y + 1, waterPosition.Z));
                        waterFloodFills.Enqueue(new CubePosition(waterPosition.X, waterPosition.Y, waterPosition.Z - 1));
                        waterFloodFills.Enqueue(new CubePosition(waterPosition.X, waterPosition.Y, waterPosition.Z + 1));
                    }
                }

                numTries--;

                if (numTries < 0)
                {
                    placeWater = false;
                    break;
                }
            }

            //Console.WriteLine("Placing water: {0}", placeWater);

            for (int x = 0; x < structure.size.X; x++)
            {
                for (int y = 0; y < structure.size.Y; y++)
                {
                    for (int z = 0; z < structure.size.Z; z++)
                    {
                        Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(structure.size.X, structure.size.Y, structure.size.Z), out int i);
                        CubePosition realPos = new CubePosition(pos.X + x, pos.Y + y, pos.Z + z, pos.Coord);

                        int overwritingId = manager.InitializerView.GetCube(realPos).GetOrDefault(Main.Registry.CubeRegistry.Air).Id;

                        if (overwritingId == stone.Id || (overwritingId == 0 && structure.data[i] == water.Id && placeWater))
                            manager.InitializerView.SetCube(realPos, structure.data[i]);
                    }
                }
            }

            if (placeWater)
            {
                foreach (CubePosition actualPos in actualFills)
                {
                    manager.InitializerView.SetCube(actualPos, water.Id);
                    //manager.GetChunk(actualPos).GetData().SetCube(actualPos, water.Id);
                }
            }
        }
    }
}
