using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using ViMG.Cubes;

namespace ViMG
{
    public struct Housing
    {
        public const int VERSION = 0;
        public const int MIN_VERSION = 0;

        public int version;

        public CubePosition[] interiorPositions;
        public CubePosition[] wallPositions;
        public CubePosition homePosition;

        public void OnSave(List<byte> saveBytes) 
        {
            //h - header
            // v - version
            // s - size
            //h - housing block
            // ic - interior positions count
            // i - interior positions array
            // wc - wall positions count
            // w - wall positions array
            // h - home position
            SaveHelper.SaveInt32(saveBytes, VERSION);

            List<byte> housingBlockBytes = new List<byte>();

            SaveHelper.SaveInt32(housingBlockBytes, interiorPositions.Length);
            for (int i = 0; i < interiorPositions.Length; i++)
                SaveHelper.SaveCubePosition(housingBlockBytes, interiorPositions[i]);
            SaveHelper.SaveInt32(housingBlockBytes, wallPositions.Length);
            for (int i = 0; i < wallPositions.Length; i++)
                SaveHelper.SaveCubePosition(housingBlockBytes, wallPositions[i]);
            SaveHelper.SaveCubePosition(housingBlockBytes, homePosition);

            SaveHelper.SaveInt32(saveBytes, housingBlockBytes.Count);
            SaveHelper.SaveBytesFlat(saveBytes, housingBlockBytes);
        }

        public void OnLoad(byte[] loadBytes, ref int offset)
        {
            version = SaveHelper.LoadInt32(loadBytes, ref offset);
            int size = SaveHelper.LoadInt32(loadBytes, ref offset);

            int countInteriorPositions = SaveHelper.LoadInt32(loadBytes, ref offset);
            interiorPositions = new CubePosition[countInteriorPositions];
            for (int i = 0; i < countInteriorPositions; i++)
                interiorPositions[i] = SaveHelper.LoadCubePosition(loadBytes, ref offset);

            int countWallPositions = SaveHelper.LoadInt32(loadBytes, ref offset);
            wallPositions = new CubePosition[countWallPositions];
            for (int i = 0; i < countWallPositions; i++)
                wallPositions[i] = SaveHelper.LoadCubePosition(loadBytes, ref offset);

            homePosition = SaveHelper.LoadCubePosition(loadBytes, ref offset);
        }
    }

    public class HousingTasker
    {
        public const int MAX_HOUSING_AREA = 16 * 16 * 16;
        public const int MIN_HOUSING_AREA = 8;

        public bool DetermineIfValidHousing(ChunkManager manager, CubePosition startPosition, out Housing housing)
        {
            housing = new Housing();
            List<CubePosition> wallPositions = new List<CubePosition>();

            HashSet<CubePosition> visited = new HashSet<CubePosition>();
            Queue<CubePosition> floodFills = new Queue<CubePosition>();
            floodFills.Enqueue(startPosition);

            while (floodFills.Count > 0)
            {
                if (visited.Count >= MAX_HOUSING_AREA)
                    return false;

                CubePosition fillPosition = floodFills.Dequeue();

                Cube cube = manager.ThreadedView.GetCube(fillPosition).GetOrDefault(Main.Registry.CubeRegistry.Air);

                bool isWall = cube.Solid || cube is CubeDoor;
                bool isAir = !cube.Solid;

                if (isWall)
                    wallPositions.Add(fillPosition);

                if (manager.IsInWorldBounds(fillPosition) && isAir && !isWall && !visited.Contains(fillPosition))
                {
                    visited.Add(fillPosition);
                    manager.InitializerView.SetCube(fillPosition, 0);
                    floodFills.Enqueue(new CubePosition(fillPosition.X - 1, fillPosition.Y, fillPosition.Z));
                    floodFills.Enqueue(new CubePosition(fillPosition.X + 1, fillPosition.Y, fillPosition.Z));
                    floodFills.Enqueue(new CubePosition(fillPosition.X, fillPosition.Y - 1, fillPosition.Z));
                    floodFills.Enqueue(new CubePosition(fillPosition.X, fillPosition.Y + 1, fillPosition.Z));
                    floodFills.Enqueue(new CubePosition(fillPosition.X, fillPosition.Y, fillPosition.Z - 1));
                    floodFills.Enqueue(new CubePosition(fillPosition.X, fillPosition.Y, fillPosition.Z + 1));
                }
            }

            if (visited.Count < MIN_HOUSING_AREA)
                return false;

            housing.interiorPositions = visited.ToArray();
            housing.wallPositions = wallPositions.ToArray();
            housing.homePosition = startPosition;

            return true;
        }
    }
}
