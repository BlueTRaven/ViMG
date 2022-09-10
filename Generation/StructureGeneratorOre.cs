using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Generation
{
    //Generates a "string" of ores.
    public class StructureGeneratorOre : StructureGenerator
    {
        private readonly ushort oreId;
        private readonly int minNum;
        private readonly int maxNum;

        private static Point3D[] offsets = new Point3D[6]
        {
            new Point3D(1, 0, 0),
            new Point3D(-1, 0, 0),
            new Point3D(0, 1, 0),
            new Point3D(0, -1, 0),
            new Point3D(0, 0, 1),
            new Point3D(0, 0, -1)
        };

        public StructureGeneratorOre(ushort oreId, int minNum, int maxNum, int seed, ChunkManager chunkManager) : base("Ore", seed, chunkManager)
        {
            this.oreId = oreId;
            this.minNum = minNum;
            this.maxNum = maxNum;
        }

        protected override Structure[] GenerateOne(ref StructureTaskState state)
        {
            int range = state.sliceEnd - state.sliceStart;
            Structure[] structures = new Structure[range];

            for (int i = 0; i < range; i++)
            {
                int minX = 0, maxX = 0;
                int minY = 0, maxY = 0;
                int minZ = 0, maxZ = 0;

                int num = state.random.Next(minNum, maxNum);
                Point3D[] points = new Point3D[num];

                Point3D currentPoint = new Point3D(0, 0, 0);

                Point3D offset = offsets[state.random.Next(0, 6)];

                for (int j = 0; j < num; j++)
                {
                    if (currentPoint.x < minX)
                        minX = currentPoint.x;
                    if (currentPoint.x > maxX)
                        maxX = currentPoint.x;
                    if (currentPoint.y < minY)
                        minY = currentPoint.y;
                    if (currentPoint.y > maxY)
                        maxY = currentPoint.y;
                    if (currentPoint.z < minZ)
                        minZ = currentPoint.z;
                    if (currentPoint.z > maxZ)
                        maxZ = currentPoint.z;

                    points[j] = currentPoint;
                    currentPoint = new Point3D(currentPoint.x + offset.x, currentPoint.y + offset.y, currentPoint.z + offset.z);

                    offset = offsets[state.random.Next(0, 6)];
                }

                //Since we can't index arrays into the negative, we have to shift everything up to the positive
                int moveX = 0;
                int moveY = 0;
                int moveZ = 0;

                if (minX < 0)
                    moveX = Math.Abs(Math.Min(minX, maxX));
                if (minY < 0)
                    moveY = Math.Abs(Math.Min(minY, maxY));
                if (minZ < 0)
                    moveZ = Math.Abs(Math.Min(minZ, maxZ));

                for (int j = 0; j < num; j++)
                {
                    ref Point3D point = ref points[j];
                    point.x += moveX;
                    point.y += moveY;
                    point.z += moveZ;
                }

                minX += moveX;
                maxX += moveX;
                minY += moveY;
                maxY += moveY;
                minZ += moveZ;
                maxZ += moveZ;

                //Now we can convert the stored position in points into a 1d index, and set that index in the structure data array to 1
                //1 means we have an ore there. Note that we don't actually store the ore id, that's generated at paste-time.
                int width = maxX - minX + 1;
                int height = maxY - minY + 1;
                int depth = maxZ - minZ + 1;

                ushort[] data = new ushort[width * height * depth];

                for (int j = 0; j < num; j++)
                {
                    Util.ThreeDToOneD(new ValuePoint3D(points[j]), new ValuePoint3D(width, height, depth), out int ind);
                    data[ind] = oreId;
                }

                structures[i] = new Structure(new Point3D(width, height, depth), data);
            }

            return structures;
        }
    }
}
