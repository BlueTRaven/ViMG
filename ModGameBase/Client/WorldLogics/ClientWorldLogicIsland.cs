using BrUtility;
using Engine.Clients;
using Engine.Clients.WorldLogics;
using Engine.Common;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.VertexDeclarations;
using ViMG.WorldLogics;

namespace ModGameBase.Client.WorldLogics
{
    public class ClientWorldLogicIsland : ClientWorldLogic
    {
        private const float SKYBOX_SUN_DISTANCE = -6 * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;
        private const float SUN_LIGHT_DISTANCE = -Cube.CUBE_SCALE * 10;

        private static VerySimpleMesh meshSun;
        private static VerySimpleMesh meshLavaQuad;
        private static RendererDeferred.DrawMaterial materialSun = new RendererDeferred.DrawMaterial(Main.assetsManager.GetAsset<Texture2D>("sun"));
        private static RendererDeferred.DrawMaterial materialLava = new RendererDeferred.DrawMaterial(Main.assetsManager.GetAsset<Texture2D>("lava"), emissive: Main.assetsManager.GetAsset<Texture2D>("lava"));
        private static Color[] duskColors =
        [
            Color.White,
            Color.Salmon,
            Color.DarkBlue,
            Color.Black,
            Color.White
        ];

        private ViMG.DirectionalLight directionalLight;
        public WeatherManager WeatherManager;

        public ClientWorldLogicIsland(GraphicsDevice device) : base(device)
        {
            float[] splits = [1f / 50f, 1f / 25f, 1f / 10f, 1f / 2f];

            directionalLight = new ViMG.DirectionalLight(device, Main.NEAR, Main.FAR, splits);

            directionalLight.WorldheightMap = Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map");

            BrUtility.FastList<VertexCube> vertices = new();
            List<int> indices = [0, 1, 3, 1, 2, 3];
            
            Color sunColor = Color.White;
            float sunVertDist = Cube.CUBE_SCALE * 12;

            vertices.Add(new VertexCube(new Vector3(-sunVertDist, -sunVertDist, 0), sunColor, new Vector2(0, 0), new Vector3(0, 0, -1)));
            vertices.Add(new VertexCube(new Vector3(-sunVertDist, sunVertDist, 0), sunColor, new Vector2(1, 0), new Vector3(0, 0, -1)));
            vertices.Add(new VertexCube(new Vector3(sunVertDist, sunVertDist, 0), sunColor, new Vector2(1, 1), new Vector3(0, 0, -1)));
            vertices.Add(new VertexCube(new Vector3(sunVertDist, -sunVertDist, 0), sunColor, new Vector2(0, 1), new Vector3(0, 0, -1)));

            meshSun = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices));

            vertices = new();
            indices = [3, 1, 0, 3, 2, 1];

            vertices.Add(new VertexCube(new Vector3(-Cube.CUBE_SCALE, 0, -Cube.CUBE_SCALE), Color.White, new Vector2(1, 1), new Vector3(0, 1, 0)));
            vertices.Add(new VertexCube(new Vector3(-Cube.CUBE_SCALE, 0, Cube.CUBE_SCALE), Color.White, new Vector2(0, 1), new Vector3(0, 1, 0)));
            vertices.Add(new VertexCube(new Vector3(Cube.CUBE_SCALE, 0, Cube.CUBE_SCALE), Color.White, new Vector2(0, 0), new Vector3(0, 1, 0)));
            vertices.Add(new VertexCube(new Vector3(Cube.CUBE_SCALE, 0, -Cube.CUBE_SCALE), Color.White, new Vector2(1, 0), new Vector3(0, 1, 0)));

            meshLavaQuad = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));

            this.skybox = new Skybox
            {
                Day = Main.assetsManager.GetAsset<Texture2D>("skybox_day"),
                Weather = Main.assetsManager.GetAsset<Texture2D>("skybox_stormy"),
                Night = Main.assetsManager.GetAsset<Texture2D>("skybox_night"),
            };

            WeatherManager = new WeatherManager(device);
        }

        public override void UpdateSimulation(double deltaTime, ClientStates client)
        {
            var prev = client.Previous(1);
            var curr = client.Current();
            Color sunlightColor = Color.White * (1 - SurfaceTimeHelper.GetTimeOfDay(curr.time));

            if (SurfaceTimeHelper.GetDuskTime(curr.time) > 0)
            {
                duskColors[0] = sunlightColor;  //so that we don't snap to the wrong color...
                duskColors[^1] = sunlightColor;
                sunlightColor = Utility.MultiLerp(SurfaceTimeHelper.GetDuskTime(curr.time), Color.Lerp, duskColors);
            }

            Vector4 lightColor = sunlightColor.ToVector4();

            float time = (float)double.Lerp(prev.time, curr.time, Main.TimeC);
            float angle = 360 * ((time % World.DAY_CYCLE_TIME) / World.DAY_CYCLE_TIME);

            Vector3 lightDir = Vector3.Transform(new Vector3(0, 0, SUN_LIGHT_DISTANCE),
             Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
             Matrix.CreateRotationY(MathHelper.ToRadians(45f)));

            WeatherManager.Update(deltaTime, (float)client.Current().time, lightColor);
            var interpPlayer = Main.Registry.EntityRegistry.Get<Player>().GetInterpolated(client, curr.entities.GetLocalPlayerRef());
            WeatherManager.UpdateClient(deltaTime, interpCamera, interpPlayer.position, client.ChunkManager.CubeView);
            WeatherManager.UpdateClientLight(deltaTime, (float)curr.time, skybox, ref lightDir, ref lightColor);

            directionalLight.UpdateCameras(client, interpCamera, lightDir, lightColor);
        }

        private ViMG.Camera interpCamera = null;
        public override void Render(GraphicsDevice device, ClientStates client)
        {
            Texture2D sunTexture = Main.assetsManager.GetAsset<Texture2D>("sun");

            var curr = client.Current();
            var prev = client.Previous(1);

            if (interpCamera == null) interpCamera = new CameraPerspective(prev.camera.Position, prev.camera.RotationEuler, prev.camera.Scale, Main.FOV_DEGREES, Main.NEAR, Main.FAR);
            interpCamera.Position = Vector3.Lerp(prev.camera.Position, curr.camera.Position, (float)Main.TimeC);
            interpCamera.Rotation = Quaternion.Lerp(prev.camera.Rotation, curr.camera.Rotation, (float)Main.TimeC);

            float time = (float)double.Lerp(prev.time, curr.time, Main.TimeC);

            WeatherManager.Draw(device, interpCamera, time);

            //if (world.LoadedFolderName == "coconut")
            //    sunTexture = Main.assetsManager.GetAsset<Texture2D>("coconut");

            float angle = 360 * ((time % World.DAY_CYCLE_TIME) / World.DAY_CYCLE_TIME);

            // TODO this should be elsewhere - we don't need to update this very often?
            directionalLight.DrawShadowmap(device, interpCamera, client.ChunkManager.ChunkMesher.RenderMesher);
            directionalLight.Bind(Main.Renderer.EffectLightAccumCSM, interpCamera);

            Main.Renderer.DrawsSkyboxPass.Add(new RendererDeferred.TransparentDraw(200,
                materialSun, meshSun,
                Matrix.CreateTranslation(new Vector3(0, 0, SKYBOX_SUN_DISTANCE)) *
                Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
                Matrix.CreateTranslation(interpCamera.Position),
                tintColor: Color.White));

            var localPlayerRef = curr.entities.GetLocalPlayerRef();
            var localPlayer = curr.entities.GetByRef(localPlayerRef);

            if (!curr.flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD) && localPlayer.position.Y / Cube.CUBE_SCALE < 140)
            {
                Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE * 512, 1, Cube.CUBE_SCALE * 512) *
                    Matrix.CreateTranslation(localPlayer.position.X, Cube.CUBE_SCALE * 40.5f, localPlayer.position.Z);

                RectangleF sourceRect = new RectangleF()
                {
                    x = -localPlayer.position.Z * 128 + time,
                    y = -localPlayer.position.X * 128 + time,
                    width = 128 * 16,
                    height = 128 * 16,
                };
                Main.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(materialLava,
                    meshLavaQuad, mat, sourceRect));
            }
        }
    }
}
