using BrUtility;
using Engine.Clients;
using Engine.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using ViMG.VertexDeclarations;

namespace ViMG.Rendering
{
    public class WorldRenderer
    {
        [ConsoleCommandVar("r_draw_dist_horiz", "Horizontal draw distance, in chunks. Default = 6")]
        public static int DrawDistanceHoriz = 6;   //radius in chunks that we should be able to see
        [ConsoleCommandVar("r_draw_dist_vert", "Vertical draw distance, in chunks. Default = 6")]
        public static int DrawDistanceVert = 6;

        [ConsoleCommandVar("r_draw_skybox", "Draw the skybox. Default = true")]
        public static bool DrawSkybox = true;

        private bool chunkDrawPositionsDirty;
        private List<ChunkPosition> culledChunkDrawPositions = new List<ChunkPosition>();

        private VerySimpleMesh skyboxMesh;

        public WorldRenderer(GraphicsDevice device)
        {
            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

            Vector3 l_b_f = new Vector3(0, 0, 1);
            Vector3 r_b_f = new Vector3(1, 0, 1);
            Vector3 r_b_n = new Vector3(1, 0, 0);
            Vector3 l_b_n = new Vector3(0, 0, 0);

            Vector3 l_t_n = new Vector3(0, 1, 0);
            Vector3 r_t_n = new Vector3(1, 1, 0);
            Vector3 r_t_f = new Vector3(1, 1, 1);
            Vector3 l_t_f = new Vector3(0, 1, 1);

            const float SKYBOX_SIDE_SIZE = 1024f;
            const float SKYBOX_WIDTH = SKYBOX_SIDE_SIZE * 4f;
            const float SKYBOX_HEIGHT = SKYBOX_SIDE_SIZE * 2f;

            //front face
            int offset = vertices.Length;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            vertices.Add(new VertexCube(r_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(0, 0, 1)));
            vertices.Add(new VertexCube(l_b_n, Color.White, new Vector2(0, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(0, 0, 1)));
            vertices.Add(new VertexCube(l_t_n, Color.White, new Vector2(0, 0), new Vector3(0, 0, 1)));
            vertices.Add(new VertexCube(r_t_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE / SKYBOX_WIDTH, 0), new Vector3(0, 0, 1)));

            //right face
            offset = vertices.Length;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            vertices.Add(new VertexCube(r_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(-1, 0, 0)));
            vertices.Add(new VertexCube(r_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 1f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(-1, 0, 0)));
            vertices.Add(new VertexCube(r_t_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 1f / SKYBOX_WIDTH, 0), new Vector3(-1, 0, 0)));
            vertices.Add(new VertexCube(r_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2f / SKYBOX_WIDTH, 0), new Vector3(-1, 0, 0)));

            //back face
            offset = vertices.Length;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            vertices.Add(new VertexCube(l_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 3f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(0, 0, -1)));
            vertices.Add(new VertexCube(r_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(0, 0, -1)));
            vertices.Add(new VertexCube(r_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2f / SKYBOX_WIDTH, 0), new Vector3(0, 0, -1)));
            vertices.Add(new VertexCube(l_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 3f / SKYBOX_WIDTH, 0), new Vector3(0, 0, -1)));

            //left face
            offset = vertices.Length;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            vertices.Add(new VertexCube(l_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 4f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(1, 0, 0)));
            vertices.Add(new VertexCube(l_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 3f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(1, 0, 0)));
            vertices.Add(new VertexCube(l_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 3f / SKYBOX_WIDTH, 0), new Vector3(1, 0, 0)));
            vertices.Add(new VertexCube(l_t_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 4f / SKYBOX_WIDTH, 0), new Vector3(1, 0, 0)));

            //top face
            offset = vertices.Length;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            vertices.Add(new VertexCube(l_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 2f / SKYBOX_HEIGHT), new Vector3(0, -1, 0)));
            vertices.Add(new VertexCube(r_t_f, Color.White, new Vector2(0, SKYBOX_SIDE_SIZE * 2f / SKYBOX_HEIGHT), new Vector3(0, -1, 0)));
            vertices.Add(new VertexCube(r_t_n, Color.White, new Vector2(0, SKYBOX_SIDE_SIZE * 1f / SKYBOX_HEIGHT), new Vector3(0, -1, 0)));
            vertices.Add(new VertexCube(l_t_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 1f / SKYBOX_HEIGHT), new Vector3(0, -1, 0)));


            //bottom face
            offset = vertices.Length;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            vertices.Add(new VertexCube(r_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2 / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 2f / SKYBOX_HEIGHT), new Vector3(0, 1, 0)));
            vertices.Add(new VertexCube(l_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 1 / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 2f / SKYBOX_HEIGHT), new Vector3(0, 1, 0)));
            vertices.Add(new VertexCube(l_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 1 / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 1f / SKYBOX_HEIGHT), new Vector3(0, 1, 0)));
            vertices.Add(new VertexCube(r_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2 / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 1f / SKYBOX_HEIGHT), new Vector3(0, 1, 0)));

            skyboxMesh = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices));
        }

        public void Render(ClientStates client)
        {
            var previous = client.Previous(1);
            var current = client.Current();
            previous.camera.FrameBegin();
            current.camera.FrameBegin();
            bool isDirty = previous.camera.Position != current.camera.Position || previous.camera.RotationEuler != current.camera.RotationEuler || previous.camera.Scale != current.camera.Scale;
            if (isDirty) current.camera.MarkDirty();

            var time = double.Lerp(previous.time, current.time, Main.TimeC);

            if (chunkDrawPositionsDirty || current.camera.IsDirty)
            {
                ChunkPosition camPos = ChunkPosition.WorldSpaceChunk(current.camera.Position);

                culledChunkDrawPositions.Clear();

                for (int x = Math.Max(0, camPos.X - DrawDistanceHoriz); x <= Math.Min(client.ChunkManager.SizeInChunks, camPos.X + DrawDistanceHoriz); x++)
                {
                    for (int y = Math.Max(0, camPos.Y - DrawDistanceVert); y <= Math.Min(client.ChunkManager.SizeInChunks, camPos.Y + DrawDistanceVert); y++)
                    {
                        for (int z = Math.Max(0, camPos.Z - DrawDistanceHoriz); z <= Math.Min(client.ChunkManager.SizeInChunks, camPos.Z + DrawDistanceHoriz); z++)
                        {
                            ChunkPosition chunkPos = new ChunkPosition(x, y, z);

                            //int length = (int)(new Vector3(chunkPos.X, chunkPos.Y, chunkPos.Z) - new Vector3(camPos.X, camPos.Y, camPos.Z)).Length();

                            if (client.ChunkManager.IsInWorldBounds(chunkPos) &&
                                current.camera.FrustumIntersects(new Rectangle3D(chunkPos.InWorldSpace(), new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE))))
                            {
                                culledChunkDrawPositions.Add(chunkPos);
                            }
                        }
                    }
                }

                chunkDrawPositionsDirty = false;
            }

            foreach (ChunkPosition pos in culledChunkDrawPositions)
            {
                Matrix transform = Matrix.Identity; //ChunkManager.GetTransform(pos);

                RendererDeferred.DrawMaterial cubesMaterial = StaticMaterials.Cubes;
                // TODO buffs
                //if (GetLocalPlayer()?.GetBuffManager().HasBuff("emissive_ores") ?? false)
                //    cubesMaterial = StaticMaterials.CubesWithEmissiveOres;

                VerySimpleMesh mesh = client.ChunkManager.ChunkMesher?.RenderMesher?.GetMesh(pos, Cubes.Cube.RenderPass.Opaque) ?? new();
                if (mesh.IBO != null)
                    Main.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(cubesMaterial, mesh, transform));

                mesh = client.ChunkManager.ChunkMesher?.RenderMesher?.GetMesh(pos, Cubes.Cube.RenderPass.Transparent) ?? new();
                if (mesh.IBO != null)
                {
                    Vector3 minBounds = current.camera.Position - pos.InWorldSpace();
                    Vector3 maxBounds = current.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

                    Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));
                    //Vector3 max = new Vector3(Math.Max(minBounds.X, maxBounds.X), Math.Max(minBounds.Y, maxBounds.Y), Math.Max(minBounds.Z, maxBounds.Z));

                    Main.Renderer.AddTransparentDraw(new RendererDeferred.TransparentDraw((int)min.Length(), cubesMaterial, mesh, transform));
                }

                if (Main.Renderer.EffectEmptyEnabled)
                {
                    mesh = client.ChunkManager.ChunkMesher?.RenderMesher?.GetMesh(pos, Cubes.Cube.RenderPass.Air) ?? new();
                    if (mesh.IBO != null)
                    {
                        Vector3 minBounds = current.camera.Position - pos.InWorldSpace();
                        Vector3 maxBounds = current.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

                        Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));

                        Main.Renderer.DrawsEmptyPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)min.Length(),
                            StaticMaterials.Cubes, mesh, transform));
                    }
                }

                //NumChunksDrawn++;
            }

            if (DrawSkybox)
            {
                float alphaDay = 1 - SurfaceTimeHelper.GetTimeOfDay(time);
                float alphaNight = SurfaceTimeHelper.GetTimeOfNight(time);

                if (alphaDay < 1)
                {
                    const float mp = (World.DAY_CYCLE_TIME * 1.5f);
                    const float my = (World.DAY_CYCLE_TIME * 1.34f);
                    float p = (float)Math.Sin(Math.PI * 2 * ((time % mp) / mp));
                    float y = (float)Math.Sin(Math.PI * 2 * ((time % my) / my));

                    Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(1001,
                        new RendererDeferred.DrawMaterial(client.WorldLogic.skybox.Night),
                        skyboxMesh,
                        Matrix.CreateTranslation(new Vector3(-0.5f)) *
                        Matrix.CreateFromYawPitchRoll(y, p, 0) *
                        Matrix.CreateTranslation(current.camera.Position),
                        null, Color.White));
                }

                if (alphaDay > 0)
                {
                    Main.Renderer.EffectRadialFog.Parameters["ColorInterpolate"].SetValue(new Vector3(0, 1, 1 - alphaDay));

                    Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(1000,
                        new RendererDeferred.DrawMaterial(client.WorldLogic.skybox.Day),
                        skyboxMesh,
                        Matrix.CreateTranslation(new Vector3(-0.5f)) *
                        Matrix.CreateTranslation(current.camera.Position),
                        null, Color.White * alphaDay));
                }

                //if (WeatherSkyboxAlpha > 0)
                //{
                //    Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw()
                //    {
                //        SortValue = 100,
                //        Material = new Rendering.RendererDeferred.DrawMaterial(client.WorldLogic.skybox.Weather),
                //        TintColor = WeatherSkyboxColor.ToVector4() * WeatherSkyboxAlpha,
                //        Transform = Matrix.CreateTranslation(new Vector3(-0.5f)) *
                //            Matrix.CreateTranslation(current.camera.Position),
                //        Mesh = skyboxMesh.Value,
                //    });
                //}
            }
        }
    }
}
