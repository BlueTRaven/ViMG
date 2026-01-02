using BrUtility;
using Engine.Clients;
using Engine.Common;
using Microsoft.Xna.Framework;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.IMGUIImpl;

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

        public void Render(ClientStates client)
        {
            var previous = client.Previous(1);
            var current = client.Current();
            previous.camera.FrameBegin();
            current.camera.FrameBegin();
            bool isDirty = previous.camera.Position != current.camera.Position || previous.camera.Rotation != current.camera.Rotation || previous.camera.Scale != current.camera.Scale;
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
                    Vector3 minBounds = Main.camera.Position - pos.InWorldSpace();
                    Vector3 maxBounds = Main.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

                    Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));
                    //Vector3 max = new Vector3(Math.Max(minBounds.X, maxBounds.X), Math.Max(minBounds.Y, maxBounds.Y), Math.Max(minBounds.Z, maxBounds.Z));

                    Main.Renderer.AddTransparentDraw(new RendererDeferred.TransparentDraw((int)min.Length(), cubesMaterial, mesh, transform));
                }

                if (Main.Renderer.EffectEmptyEnabled)
                {
                    mesh = client.ChunkManager.ChunkMesher?.RenderMesher?.GetMesh(pos, Cubes.Cube.RenderPass.Air) ?? new();
                    if (mesh.IBO != null)
                    {
                        Vector3 minBounds = Main.camera.Position - pos.InWorldSpace();
                        Vector3 maxBounds = Main.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

                        Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));

                        Main.Renderer.DrawsEmptyPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)min.Length(),
                            StaticMaterials.Cubes, mesh, transform));
                    }
                }

                //NumChunksDrawn++;
            }

            //if (DrawSkybox)
            //{
            //    float alphaDay = 1 - SurfaceTimeHelper.GetTimeOfDay(time);
            //    float alphaNight = SurfaceTimeHelper.GetTimeOfNight(time);

            //    if (alphaDay < 1)
            //    {
            //        const float mp = (World.DAY_CYCLE_TIME * 1.5f);
            //        const float my = (World.DAY_CYCLE_TIME * 1.34f);
            //        float p = (float)Math.Sin(Math.PI * 2 * ((time % mp) / mp));
            //        float y = (float)Math.Sin(Math.PI * 2 * ((time % my) / my));

            //        Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(1001,
            //            new RendererDeferred.DrawMaterial(Skybox.Night),
            //            skyboxMesh.Value,
            //            Matrix.CreateTranslation(new Vector3(-0.5f)) *
            //            Matrix.CreateFromYawPitchRoll(y, p, 0) *
            //            Matrix.CreateTranslation(Main.camera.Position),
            //            null, Color.White));
            //    }

            //    if (alphaDay > 0)
            //    {
            //        Main.Renderer.EffectRadialFog.Parameters["ColorInterpolate"].SetValue(new Vector3(0, 1, 1 - alphaDay));

            //        Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(1000,
            //            new RendererDeferred.DrawMaterial(Skybox.Day),
            //            skyboxMesh.Value,
            //            Matrix.CreateTranslation(new Vector3(-0.5f)) *
            //            Matrix.CreateTranslation(Main.camera.Position),
            //            null, Color.White * alphaDay));
            //    }

            //    if (WeatherSkyboxAlpha > 0)
            //    {
            //        Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw()
            //        {
            //            SortValue = 100,
            //            Material = new Rendering.RendererDeferred.DrawMaterial(Skybox.Weather),
            //            TintColor = WeatherSkyboxColor.ToVector4() * WeatherSkyboxAlpha,
            //            Transform = Matrix.CreateTranslation(new Vector3(-0.5f)) *
            //                Matrix.CreateTranslation(Main.camera.Position),
            //            Mesh = skyboxMesh.Value,
            //        });
            //    }

            //    //if (Main.Debug)
            //    //    HitboxManager.DrawDebug(device);

            //    //if (Main.Debug)
            //    //    HousingManager.DrawDebug(this, device);
            //}
        }
    }
}
