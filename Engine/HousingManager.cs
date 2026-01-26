using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.IMGUIImpl;
using ViMG.Rendering;
using ViMG.VertexDeclarations;
using static ViMG.HitboxManager;

namespace ViMG
{
    public enum HousingValidity
    {
        TooLittleSpace,
        TooMuchSpace,
        Valid,
        AlreadyExists
    }

    public struct Housing
    {
        //TODO layer
        public const int VERSION = 0;
        public const int MIN_VERSION = 0;

        public int version;

        public CubePosition[] interiorPositions;
        public CubePosition[] wallPositions;
        public CubePosition homePosition;

        public int id;  //not saved

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

    public class HousingManager
    {
        public const int MAX_HOUSING_AREA = 16 * 16 * 16;
        public const int MIN_HOUSING_AREA = 8;

        private Dictionary<CubePosition, int> housingPositions = new Dictionary<CubePosition, int>();

        public void OnCubeUpdate(World world, CubePosition position, ushort id)
        {
            //TODO cleanup
            //this is super gross and could be handled way better
            if (housingPositions.ContainsKey(position))
            {
                int housingId = housingPositions[position];

                Housing housing = world.WorldInfo.housings[housingId];

                foreach (CubePosition pos in housing.interiorPositions)
                {
                    housingPositions.Remove(pos);
                }

                foreach (CubePosition pos in housing.wallPositions)
                {
                    housingPositions.Remove(pos);
                }

                world.WorldInfo.housings.RemoveAt(housingId);

                for (int i = housingId; i < world.WorldInfo.housings.Count; i++)
                {
                    Housing updateHousing = world.WorldInfo.housings[i];
                    updateHousing.id = i;

                    world.WorldInfo.housings[i] = updateHousing;
                }

                CubePosition checkPos = housing.homePosition;
                //if the home position is == position, then we placed a block on the home position.
                //If this is the case just choose a random block I guess...
                //TODO make this more robust
                if (housing.homePosition == position)
                    checkPos = housing.interiorPositions[GlobalState.random.Next(0, housing.interiorPositions.Length)];

                HousingValidity valid = DetermineIfValidHousing(world, checkPos, out Housing newHousing);

                if (valid == HousingValidity.Valid)
                    AddHousing(ref world.WorldInfo, ref newHousing);
            }
        }

        public void FinishLoading(WorldInfoIO.WorldInfo worldInfo) 
        {
            for (int i = 0; i < worldInfo.housings.Count; i++)
            {
                Housing housing = worldInfo.housings[i];

                housing.id = i;

                foreach (CubePosition pos in housing.interiorPositions)
                {
                    if (!housingPositions.ContainsKey(pos))
                        housingPositions.Add(pos, housing.id);
                }

                foreach (CubePosition pos in housing.wallPositions)
                {
                    if (!housingPositions.ContainsKey(pos))
                        housingPositions.Add(pos, housing.id);
                }

                worldInfo.housings[i] = housing;
            }
        }

        public void AddHousing(ref WorldInfoIO.WorldInfo worldInfo, ref Housing housing)
        {
            housing.id = worldInfo.housings.Count;
            worldInfo.housings.Add(housing);

            for (int i = 0; i < housing.interiorPositions.Length; i++)
            {
                CubePosition pos = housing.interiorPositions[i];
                housingPositions.Add(pos, housing.id);
            }

            for (int i = 0; i < housing.wallPositions.Length; i++)
            {
                CubePosition pos = housing.wallPositions[i];
                housingPositions.Add(pos, housing.id);
            }
        }

        public HousingValidity DetermineIfValidHousing(World world, CubePosition startPosition, out Housing housing)
        {
            if (housingPositions.ContainsKey(startPosition))
            {
                housing = world.WorldInfo.housings[housingPositions[startPosition]];
                return HousingValidity.AlreadyExists;
            }

            housing = new Housing();
            List<CubePosition> wallPositions = new List<CubePosition>();
            List<CubePosition> interiorPositions = new List<CubePosition>();

            HashSet<CubePosition> visited = new HashSet<CubePosition>();
            Queue<CubePosition> floodFills = new Queue<CubePosition>();
            floodFills.Enqueue(startPosition);

            while (floodFills.Count > 0)
            {
                if (visited.Count >= MAX_HOUSING_AREA)
                    return HousingValidity.TooMuchSpace;

                CubePosition fillPosition = floodFills.Dequeue();

                Cube cube = world.ChunkManager.CubeView.GetCube(fillPosition).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);

                bool isWall = cube.Solid || cube.Collision == Cube.CollisionValue.Door;
                bool isAir = !cube.Solid;

                if (world.ChunkManager.IsInWorldBounds(fillPosition) && !visited.Contains(fillPosition))
                {
                    if (isWall)
                    {
                        wallPositions.Add(fillPosition);
                        visited.Add(fillPosition);
                    }
                    else if (isAir)
                    {
                        interiorPositions.Add(fillPosition);

                        visited.Add(fillPosition);
                        world.ChunkManager.CubeView.SetCube(fillPosition, 0);
                        floodFills.Enqueue(new CubePosition(fillPosition.X - 1, fillPosition.Y, fillPosition.Z));
                        floodFills.Enqueue(new CubePosition(fillPosition.X + 1, fillPosition.Y, fillPosition.Z));
                        floodFills.Enqueue(new CubePosition(fillPosition.X, fillPosition.Y - 1, fillPosition.Z));
                        floodFills.Enqueue(new CubePosition(fillPosition.X, fillPosition.Y + 1, fillPosition.Z));
                        floodFills.Enqueue(new CubePosition(fillPosition.X, fillPosition.Y, fillPosition.Z - 1));
                        floodFills.Enqueue(new CubePosition(fillPosition.X, fillPosition.Y, fillPosition.Z + 1));
                    }
                }
            }

            if (visited.Count < MIN_HOUSING_AREA)
                return HousingValidity.TooLittleSpace;

            housing.interiorPositions = interiorPositions.ToArray();
            housing.wallPositions = wallPositions.ToArray();

            housing.homePosition = startPosition;

            return HousingValidity.Valid;
        }

        private static VerySimpleMesh debugMesh;

        [ConsoleCommandVar("rsv_housing_draw", "Singleplayer only. Draws housing. Default = false")]
        public static bool DoDebugDraw = false;

        // TODO
        public void DrawDebug(GraphicsDevice device, RendererDeferred renderer)
        {
            if (!DoDebugDraw) return;

            if (debugMesh.IBO == null)
            {
                FastList<VertexCube> vertices = new FastList<VertexCube>();
                List<int> indices = new List<int>();
                MeshHelper.MakeCubeVertsVertexPositionColorTextureNormal(Vector3.Zero, Vector3.One, MeshHelper.CubeFace.ALL, Color.White, vertices, indices);
                
                debugMesh = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices));
                //debugMesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);
            }

            //if (world.WorldInfo.housings != null)
            //{
            //    foreach (Housing housing in world.WorldInfo.housings)
            //    {
            //        foreach (CubePosition pos in housing.interiorPositions)
            //        {
            //            Vector3 worldPos = pos.InWorldSpace();
            //            float distance = (worldPos - Main.camera.Position).Length();
            //            Matrix transform = Matrix.CreateScale(Cube.CUBE_SCALE) *
            //                Matrix.CreateTranslation(worldPos);

            //            //private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("mana_star");
            //            Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(distance,
            //                new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), debugMesh, transform,
            //                tintColor: Color.Green * 0.125f));
            //        }
            //    }
            //}
        }
    }
}
