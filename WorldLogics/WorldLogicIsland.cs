using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.GameStates;
using ViMG.Spawners;
using ViMG.VertexDeclarations;

namespace ViMG.WorldLogics
{
    public class WorldLogicIsland : WorldLogic
    {
		private const float SKYBOX_SUN_DISTANCE = -6 * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;
		private const float SUN_LIGHT_DISTANCE = -Cube.CUBE_SCALE * 10;
		private const float SUN_LIGHT_ANGLE = 5f; //rotate 5 degrees
		private const float LAVA_HEIGHT = Cube.CUBE_SCALE * 40.5f;
		private static (VertexBuffer VBO, IndexBuffer IBO) meshSun;
		private static (VertexBuffer VBO, IndexBuffer IBO) meshLavaQuad;
        private static (VertexBuffer VBO, IndexBuffer IBO) skyboxCloudsMesh;

        private float alive;
        private DirectionalLight directionalLight;
		//1 and last are replaced by the previous directional light color to prevent jumping colors.
		private static Color[] duskColors = new Color[] 
		{ 
			Color.White, 
			Color.Salmon, 
			Color.DarkBlue, 
			Color.Black, 
			Color.White 
		};
        
		private int lavaLight;

		private WeatherManager weatherManager;

        public WorldLogicIsland(string worldName, GraphicsDevice device) : base(device)
        {
			weatherManager = new WeatherManager(device);

			float[] splits = new float[] { 1f / 50f, 1f / 25f, 1f / 10f, 1f / 2f };

            directionalLight = new DirectionalLight(device, Main.camera, splits);
			
            directionalLight.WorldheightMap = Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map");

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			indices.Add(0);
			indices.Add(1);
			indices.Add(3);
			indices.Add(1);
			indices.Add(2);
			indices.Add(3);

			Color sunColor = Color.White;
			float sunVertDist = Cube.CUBE_SCALE * 12;

			if (worldName == "coconut")
			{
				sunVertDist = Cube.CUBE_SCALE * 128;
				sunColor = Color.White;
			}

			vertices.Add(new VertexCube(new Vector3(-sunVertDist, -sunVertDist, 0), sunColor, new Vector2(0, 0), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(new Vector3(-sunVertDist, sunVertDist, 0), sunColor, new Vector2(1, 0), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(new Vector3(sunVertDist, sunVertDist, 0), sunColor, new Vector2(1, 1), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(new Vector3(sunVertDist, -sunVertDist, 0), sunColor, new Vector2(0, 1), new Vector3(0, 0, -1)));

			meshSun = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);

			vertices = new List<VertexCube>();
			indices = new List<int>();

			indices.Add(3);
			indices.Add(1);
			indices.Add(0);
			indices.Add(3);
			indices.Add(2);
			indices.Add(1);

			vertices.Add(new VertexCube(new Vector3(-Cube.CUBE_SCALE, 0, -Cube.CUBE_SCALE), Color.White, new Vector2(1, 1), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(new Vector3(-Cube.CUBE_SCALE, 0, Cube.CUBE_SCALE), Color.White, new Vector2(0, 1), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(new Vector3(Cube.CUBE_SCALE, 0, Cube.CUBE_SCALE), Color.White, new Vector2(0, 0), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(new Vector3(Cube.CUBE_SCALE, 0, -Cube.CUBE_SCALE), Color.White, new Vector2(1, 0), new Vector3(0, 1, 0)));

			meshLavaQuad = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);

			vertices = new List<VertexCube>();
			indices = new List<int>();

            const int CYLINDER_NUM_SIDES = 16;

            for (int i = 0; i < CYLINDER_NUM_SIDES; i++)
            {
                float tc = (float)i / (float)CYLINDER_NUM_SIDES;
                float tn = ((float)i + 1) / (float)CYLINDER_NUM_SIDES;

                float cc = float.Cos(float.Pi * 2 * tc);
                float sc = float.Sin(float.Pi * 2 * tc);
                float cn = float.Cos(float.Pi * 2 * tn);
                float sn = float.Sin(float.Pi * 2 * tn);

                //cylinder_left/right_top/bottom_near/far
                Vector3 p1 = new Vector3(cc, 0, sc);
                Vector3 p2 = new Vector3(cc, 1, sc);
                Vector3 p3 = new Vector3(cn, 1, sn);
                Vector3 p4 = new Vector3(cn, 0, sn);

                Vector2 tc1 = new Vector2(tc * 4f, 1);
                Vector2 tc2 = new Vector2(tc * 4f, 0);
                Vector2 tc3 = new Vector2(tn * 4f, 0);
                Vector2 tc4 = new Vector2(tn * 4f, 1);

                int offset = vertices.Count;
                indices.Add(offset + 0);
                indices.Add(offset + 1);
                indices.Add(offset + 2);
                indices.Add(offset + 2);
                indices.Add(offset + 3);
                indices.Add(offset + 0);

                vertices.Add(new VertexCube(p1, Color.White, tc1, new Vector3(0, 1, 0)));
                vertices.Add(new VertexCube(p2, Color.White, tc2, new Vector3(0, 1, 0)));
                vertices.Add(new VertexCube(p3, Color.White, tc3, new Vector3(0, 1, 0)));
                vertices.Add(new VertexCube(p4, Color.White, tc4, new Vector3(0, 1, 0)));
            }

            skyboxCloudsMesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			world.Skybox.Day = Main.assetsManager.GetAsset<Texture2D>("skybox_day");
			world.Skybox.Weather = Main.assetsManager.GetAsset<Texture2D>("skybox_stormy");
            world.Skybox.Night = Main.assetsManager.GetAsset<Texture2D>("skybox_night");

			world.PassiveSpawnerManager.AddPassiveSpawner(new PSMerchant(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager.AddPassiveSpawner(new PSSlime(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager.AddPassiveSpawner(new PSSKeleton(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager.AddPassiveSpawner(new PSImp(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager.AddPassiveSpawner(new PSCaveSlime(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager.AddPassiveSpawner(new PSSnake(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager.AddPassiveSpawner(new PSStoneBeetle(world.PassiveSpawnerManager, world.EntityManager));
		}

        public override void Update(World world, double deltaTime)
        {
            base.Update(world, deltaTime);
			alive += (float)deltaTime;

			Color sunlightColor = Color.White * (1 - world.GetTimeOfDay());

            if (world.GetDuskTime() > 0)
            {
                duskColors[0] = sunlightColor;  //so that we don't snap to the wrong color...
                duskColors[^1] = sunlightColor;
                sunlightColor = Utility.MultiLerp(world.GetDuskTime(), Color.Lerp, duskColors);
            }

            weatherManager.Update(deltaTime, world, directionalLight, ref sunlightColor);

			if (!world.WorldInfo.flags.Flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD) && world.player.Position.Y / Cube.CUBE_SCALE < 140)
			{
				Vector3 lavaPosition = new Vector3(world.player.Position.X, LAVA_HEIGHT, world.player.Position.Z);

				if (world.player.Position.Y < lavaPosition.Y)
					world.player.Kill();

				if (lavaLight == -1)
					lavaLight = world.LightManager.Add(lavaPosition, Cube.CUBE_SCALE * 28, Cube.CUBE_SCALE * 32, Color.OrangeRed.ToVector4());
				else
					world.LightManager.Update(lavaLight, lavaPosition, Cube.CUBE_SCALE * 28, Cube.CUBE_SCALE * 32, Color.OrangeRed.ToVector4());
			}
			else
			{
				if (lavaLight != -1)
				{
					world.LightManager.Remove(lavaLight);
					lavaLight = -1;
				}
			}

			//below this point, don't even bother updating the directional light as we can't see any of it anyway. It should have no contribution to the scene.
			if (CubePosition.FromWorldSpace(world.player.Position).Y > 140)
			{
				Main.Renderer.DoCSMLight = true;

				if ((int)((world.GetTime() * 60f) % 5f) == 0 || Main.camera.IsDirty)
				{
					float angle = 360 * ((world.GetTime() % World.DAY_CYCLE_TIME) / World.DAY_CYCLE_TIME);
					directionalLight.UpdateCameras(world, Vector3.Transform(new Vector3(0, 0, SUN_LIGHT_DISTANCE),
						Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
						Matrix.CreateRotationY(MathHelper.ToRadians(SUN_LIGHT_ANGLE))), sunlightColor);

					float ambient = 1 - world.GetTimeOfDay(dawnEndOffsetScale: 1.25f);
					//Main.CubeLitEffect.Parameters["AmbientStrength"].SetValue(ambient);
					Main.Renderer.EffectGBuffer.Parameters["AmbientStrength"].SetValue(ambient);
					if (!Main.inputManager.IsHeld(Keys.F6))
						Main.Renderer.EffectGBuffer.Parameters["WorldheightMapAmb"].SetValue(Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map"));
					else Main.Renderer.EffectGBuffer.Parameters["WorldheightMapAmb"].SetValue(DrawHelper.WhitePixel);
					//Main.Renderer.EffectGBuffer.Parameters["Heightmap"].SetValue(ChunkManager.Heightmap);
					Main.Renderer.EffectTransparent.Parameters["AmbientStrength"].SetValue(ambient);
					Main.Renderer.EffectTransparent.Parameters["WorldheightMapAmb"].SetValue(Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map"));
				}
			}
			else
			{
				Main.Renderer.DoCSMLight = false;

				if (Main.inputManager.IsHeld(Keys.F6))
					Main.Renderer.EffectGBuffer.Parameters["WorldheightMapAmb"].SetValue(DrawHelper.WhitePixel);
			}
		}

        public override bool AllowsLoadingNextLayer(World world)
        {
			return world.WorldInfo.flags.Flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD);
        }

        public override void Draw(World world, GraphicsDevice device)
        {
            base.Draw(world, device);

			weatherManager.Draw(device);
            /*if (Main.inputManager.JustPressed(Keys.V))
            {
                directionalLight.Dispose();

                //float[] splits = new float[] { 1f / 50f, 1f / 25f, 1f / 10f, 1f / 2f };
                float[] splits = new float[] { 0.001f, 0.005f, 0.01f, 0.1f };

                directionalLight = new DirectionalLight(device, Main.camera, splits);

                directionalLight.WorldheightMap = Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map");
            }*/

            directionalLight.DrawShadowmap(device, world);
			directionalLight.Bind(Main.Renderer.EffectLightAccumCSM);

			Texture2D sunTexture = Main.assetsManager.GetAsset<Texture2D>("sun");

			if (world.LoadedFolderName == "coconut")
				sunTexture = Main.assetsManager.GetAsset<Texture2D>("coconut");

			float angle = 360 * ((world.GetTime() % World.DAY_CYCLE_TIME) / World.DAY_CYCLE_TIME);

			Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(200,
				Matrix.CreateTranslation(new Vector3(0, 0, SKYBOX_SUN_DISTANCE)) *
				Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
				Matrix.CreateTranslation(world.player.Position),
				sunTexture, DrawHelper.WhitePixel, meshSun.VBO, meshSun.IBO));

            Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw()
            {
                SortValue = 199,
                Diffuse = Main.assetsManager.GetAsset<Texture2D>("skybox_clouds"),
                Emissive = null,
                TintColor = Color.White.ToVector4() * 0.65f * (1 - world.GetTimeOfDay()),
                Transform = 
				Matrix.CreateScale(1, 0.5f, 1) *
				Matrix.CreateRotationY(MathHelper.ToRadians(angle)) *
				Matrix.CreateTranslation(Main.camera.Position - Vector3.Up * 0.25f),
                VBO = skyboxCloudsMesh.VBO,
                IBO = skyboxCloudsMesh.IBO,
            });

            Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw()
            {
                SortValue = 199,
                Diffuse = DrawHelper.WhitePixel,
                Emissive = null,
                TintColor = Color.White.ToVector4() * 0.65f * (1 - world.GetTimeOfDay()),
                Transform =
                Matrix.CreateRotationY(MathHelper.ToRadians(angle)) *
                Matrix.CreateTranslation(Main.camera.Position - Vector3.Up * 1.25f),
                VBO = skyboxCloudsMesh.VBO,
                IBO = skyboxCloudsMesh.IBO,
            });

            if (!world.WorldInfo.flags.Flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD) && world.player.Position.Y / Cube.CUBE_SCALE < 140)
			{
				Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE * 512, 1, Cube.CUBE_SCALE * 512) *
					Matrix.CreateTranslation(world.player.Position.X, Cube.CUBE_SCALE * 40.5f, world.player.Position.Z);

				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("lava"),
					DrawHelper.BlackPixel, DrawHelper.WhitePixel, meshLavaQuad.VBO, meshLavaQuad.IBO, mat));
			}
		}

        public override void Dispose()
        {
            base.Dispose();

			directionalLight.Dispose();
        }
    }
}
