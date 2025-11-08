using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Generation;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Items
{
    public class ItemDebugStructureCopier : Item
    {
        private enum State
        {
            None,
            FirstClick,
            SecondClick,
        }

        private State state;
        private CubePosition first;
        private CubePosition second;

        private VerySimpleMesh meshWireframeCube;

        public ItemDebugStructureCopier() : base("DEBUGStructureCopier", StaticMaterials.Items, new RectangleF(112, 112, 16, 16))
        {
            name = "DEBUG STRUCTURE COPIER";
            description = "Right click to begin selecting.\n" +
                "Select two points, then press shift+left click to save to file.\n" +
                "Press shift+right click to reset at any point.";
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
            if (player.IsLooking)
            {
                if (state == State.None)
                {
                    state = State.FirstClick;
                    first = player.PlaceAtPos;
                    second = player.PlaceAtPos;
                }
                else if (state == State.FirstClick || state == State.SecondClick)
                {
                    state = State.SecondClick;
                    second = player.PlaceAtPos;
                }

                if (Main.inputManager.IsHeld(Microsoft.Xna.Framework.Input.Keys.LeftShift))
                {
                    state = State.None;
                }
            }

            return base.RightClick(player, inventory, index, facing, out actionStats);
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
            if (state == State.SecondClick && Main.inputManager.IsHeld(Microsoft.Xna.Framework.Input.Keys.LeftShift))
            {
                int width = Math.Abs(second.X - first.X) + 1;
                int height = Math.Abs(second.Y - first.Y) + 1;
                int depth = Math.Abs(second.Z - first.Z) + 1;

                int minX = Math.Min(first.X, second.X);
                int maxX = Math.Max(first.X, second.X) + 1;
                int minY = Math.Min(first.Y, second.Y);
                int maxY = Math.Max(first.Y, second.Y) + 1;
                int minZ = Math.Min(first.Z, second.Z);
                int maxZ = Math.Max(first.Z, second.Z) + 1;

                //TODO check
                //this may or may not actually work
                int pi = 0;
                Span<CubePosition> positions = stackalloc CubePosition[width * height * depth];
                ushort[] data = new ushort[width * height * depth];

                for (int x = minX; x < maxX; x++)
                {
                    for (int y = minY; y < maxY; y++)
                    {
                        for (int z = minZ; z < maxZ; z++)
                        {
                            positions[pi] = new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);
                            pi++;
                        }
                    }
                }

                player.world.ChunkManager.CubeView.GetIds(positions, data.AsSpan());
            
                /*for (int x = minX; x < maxX; x++)
                {
                    for (int y = minY; y < maxY; y++)
                    {
                        for (int z = minZ; z < maxZ; z++)
                        {
                            ushort id = player.GetWorld().ChunkManager2.GetCube(new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace)).GetOrDefault(Main.Registry.CubeRegistry.Air).Id;

                            Util.ThreeDToOneD(new ValuePoint3D(x - minX, y - minY, z - minZ), new ValuePoint3D(width, height, depth), out int i);

                            data[i] = id;
                        }
                    }
                }*/

                Structure structure = new Structure(new Point3D(width, height, depth), data);
                List<byte> bytes = new List<byte>();
                structure.Serialize(bytes);
                File.WriteAllBytes("./STRUCTURE_OUTPUT.struct", bytes.ToArray());
            }

            return base.LeftClick(player, inventory, index, facing, out actionStats);
        }

        public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
        {
            base.DrawInWorld(device, world, item, transform);

            if (meshWireframeCube.IBO == null)
            {
                FastList<VertexCube> vertices = new FastList<VertexCube>();
                List<int> indices = new List<int>();
                MeshHelper.MakeCubeVertsVertexPositionColorTextureNormal(Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, vertices, indices);
                meshWireframeCube = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices));
                //meshWireframeCube = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices); //MeshHelper.MakeCubeVertexPositionColorTextureNormal(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
            }

            if (state != State.None)
            {
                Vector3 start = first.InWorldSpace(out bool ok) + new Vector3(Cube.CUBE_SCALE);
                Vector3 scale = (second - first).InWorldSpace(out ok);

                if (scale.X > 0)
                {
                    start.X -= Cube.CUBE_SCALE;
                    scale.X += Cube.CUBE_SCALE;
                }
                else scale.X -= Cube.CUBE_SCALE;
                if (scale.Y > 0)
                {
                    start.Y -= Cube.CUBE_SCALE;
                    scale.Y += Cube.CUBE_SCALE;
                }
                else scale.Y -= Cube.CUBE_SCALE;
                if (scale.Z > 0)
                {
                    start.Z -= Cube.CUBE_SCALE;
                    scale.Z += Cube.CUBE_SCALE;
                }
                else scale.Z -= Cube.CUBE_SCALE;

                Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(0,
                    new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), meshWireframeCube, 
                    Matrix.CreateScale(scale / Cube.CUBE_SCALE) * Matrix.CreateTranslation(start), 
                    tintColor: Color.White * 0.5f));
            }
        }
    }
}
