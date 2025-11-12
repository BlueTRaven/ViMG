using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.GameStates;
using ViMG.Rendering;
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
		private static VerySimpleMesh meshSun;
		private static VerySimpleMesh meshLavaQuad;
        private static RendererDeferred.DrawMaterial materialSun = new RendererDeferred.DrawMaterial(Main.assetsManager.GetAsset<Texture2D>("sun"));
        private static RendererDeferred.DrawMaterial materialLava = new RendererDeferred.DrawMaterial(Main.assetsManager.GetAsset<Texture2D>("lava"), emissive: Main.assetsManager.GetAsset<Texture2D>("lava"));

        private float alive;
        private DirectionalLight? directionalLight = null;
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

		private WeatherManager? weatherManager = null;

		private float weatherChangeTimer;
		private static Vector2 passiveWeatherTime = new Vector2(60 * 4f, 60 * 12f);
		private static Vector2 activeWeatherTime = new Vector2(60 * 2f, 60 * 12f);
		private const float ACTIVE_WEATHER_CHANCE = 0.25f;

        public WorldLogicIsland() : base()
        {
			
        }

        public override void FinishLoading(World world, GraphicsDevice device)
        {
            base.FinishLoading(world, device);

            weatherManager = new WeatherManager(device);

            float[] splits = [1f / 50f, 1f / 25f, 1f / 10f, 1f / 2f];

            directionalLight = new DirectionalLight(device, Main.camera, splits);

            directionalLight.WorldheightMap = Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map");

            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = [0, 1, 3, 1, 2, 3];

            Color sunColor = Color.White;
            float sunVertDist = Cube.CUBE_SCALE * 12;

            if (Main.SessionInformation.LastLoadedSave == "coconut")
            {
                sunVertDist = Cube.CUBE_SCALE * 128;
                sunColor = Color.White;
            }

            vertices.Add(new VertexCube(new Vector3(-sunVertDist, -sunVertDist, 0), sunColor, new Vector2(0, 0), new Vector3(0, 0, -1)));
            vertices.Add(new VertexCube(new Vector3(-sunVertDist, sunVertDist, 0), sunColor, new Vector2(1, 0), new Vector3(0, 0, -1)));
            vertices.Add(new VertexCube(new Vector3(sunVertDist, sunVertDist, 0), sunColor, new Vector2(1, 1), new Vector3(0, 0, -1)));
            vertices.Add(new VertexCube(new Vector3(sunVertDist, -sunVertDist, 0), sunColor, new Vector2(0, 1), new Vector3(0, 0, -1)));

            meshSun = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices));
            //meshSun = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);

            vertices = new FastList<VertexCube>();
            indices = [3, 1, 0, 3, 2, 1];

            vertices.Add(new VertexCube(new Vector3(-Cube.CUBE_SCALE, 0, -Cube.CUBE_SCALE), Color.White, new Vector2(1, 1), new Vector3(0, 1, 0)));
            vertices.Add(new VertexCube(new Vector3(-Cube.CUBE_SCALE, 0, Cube.CUBE_SCALE), Color.White, new Vector2(0, 1), new Vector3(0, 1, 0)));
            vertices.Add(new VertexCube(new Vector3(Cube.CUBE_SCALE, 0, Cube.CUBE_SCALE), Color.White, new Vector2(0, 0), new Vector3(0, 1, 0)));
            vertices.Add(new VertexCube(new Vector3(Cube.CUBE_SCALE, 0, -Cube.CUBE_SCALE), Color.White, new Vector2(1, 0), new Vector3(0, 1, 0)));

            meshLavaQuad = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));
            //meshLavaQuad = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);

            world.Skybox.Day = Main.assetsManager.GetAsset<Texture2D>("skybox_day");
            world.Skybox.Weather = Main.assetsManager.GetAsset<Texture2D>("skybox_stormy");
            world.Skybox.Night = Main.assetsManager.GetAsset<Texture2D>("skybox_night");
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

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

			if (world.player == null)
			{
				Console.WriteLine("Player was not found. Creating new one...");
                var player = new Player();
                player.FirstCreated();
				world.EntityManager.Add(player, true);

				// TODO: load spawn layer.
				// Right now this will just spawn the player at the spawn point in the currently loaded layer, which is probably not correct
                Vector3 playerSpawnPosition = world.WorldInfo.spawnPosition;
                player.Position = playerSpawnPosition;
                player.SpawnPosition = CubePosition.FromWorldSpace(playerSpawnPosition);

				world.player = player;
            }

			if (!world.WorldInfo.flags.Flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD) && world.player != null && world.player.Position.Y / Cube.CUBE_SCALE < 140)
			{
				Vector3 lavaPosition = new Vector3(world.player.Position.X, LAVA_HEIGHT + (Cube.CUBE_SCALE * 0.25f), world.player.Position.Z);

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

			if (weatherChangeTimer <= 0 || Main.inputManager.JustPressed(Keys.L))
			{
				if (!weatherManager.IsTransitioning())
				{
					bool isActive = Main.random.NextFloat() < ACTIVE_WEATHER_CHANCE;

					WeatherManager.WeatherType[] types;

					if (!isActive)
					{
						types = WeatherManager.PassiveWeatherTypes;
						weatherChangeTimer = Main.random.NextFloat(passiveWeatherTime.X, passiveWeatherTime.Y);
					}
					else
					{
						types = WeatherManager.ActiveWeatherTypes;
						weatherChangeTimer = Main.random.NextFloat(activeWeatherTime.X, activeWeatherTime.Y);
					}

					WeatherManager.WeatherType nextWeather = types[Main.random.Next(0, types.Length)];

                    weatherManager.DoTransition(nextWeather, 15f);
                }
			}
			else weatherChangeTimer -= (float)deltaTime;

            //below this point, don't even bother updating the directional light as we can't see any of it anyway. It should have no contribution to the scene.
            if (world.player != null && CubePosition.FromWorldSpace(world.player.Position).Y > 140)
			{
				Main.Renderer.DoCSMLight = true;

                Color sunlightColor = Color.White * (1 - world.GetTimeOfDay());

                if (world.GetDuskTime() > 0)
                {
                    duskColors[0] = sunlightColor;  //so that we don't snap to the wrong color...
                    duskColors[^1] = sunlightColor;
                    sunlightColor = Utility.MultiLerp(world.GetDuskTime(), Color.Lerp, duskColors);
                }

				Vector4 lightColor = sunlightColor.ToVector4();

                float angle = 360 * ((world.GetTime() % World.DAY_CYCLE_TIME) / World.DAY_CYCLE_TIME);
				Vector3 lightDir = Vector3.Transform(new Vector3(0, 0, SUN_LIGHT_DISTANCE),
					Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
					Matrix.CreateRotationY(MathHelper.ToRadians(45f)));

                weatherManager.Update(deltaTime, world, directionalLight, ref lightDir, ref lightColor, out bool lightNeedsUpdateFromWeather);

                if ((int)((world.GetTime() * 60f) % 5f) == 0 || Main.camera.IsDirty || lightNeedsUpdateFromWeather)
				{
					directionalLight.UpdateCameras(world, lightDir, lightColor);

					float ambient = 1 - world.GetTimeOfDay(dawnEndOffsetScale: 1.25f);
					Main.Renderer.EffectGBuffer.Parameters["AmbientStrength"].SetValue(ambient);
					if (!Main.inputManager.IsHeld(Keys.F6))
						Main.Renderer.EffectGBuffer.Parameters["WorldheightMapAmb"].SetValue(Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map"));
					else Main.Renderer.EffectGBuffer.Parameters["WorldheightMapAmb"].SetValue(DrawHelper.WhitePixel);
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

			weatherManager.Draw(device, world);
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

			//TODO: re-implement this easter egg
			Texture2D sunTexture = Main.assetsManager.GetAsset<Texture2D>("sun");

			if (world.LoadedFolderName == "coconut")
				sunTexture = Main.assetsManager.GetAsset<Texture2D>("coconut");

			float angle = 360 * ((world.GetTime() % World.DAY_CYCLE_TIME) / World.DAY_CYCLE_TIME);

			Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(200,
				materialSun, meshSun,
                Matrix.CreateTranslation(new Vector3(0, 0, SKYBOX_SUN_DISTANCE)) *
				Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
				Matrix.CreateTranslation(world.player.Position),
				tintColor: Color.White * (1 - world.WeatherSkyboxAlpha)));

            if (!world.WorldInfo.flags.Flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD) && world.player.Position.Y / Cube.CUBE_SCALE < 140)
			{
				Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE * 512, 1, Cube.CUBE_SCALE * 512) *
					Matrix.CreateTranslation(world.player.Position.X, Cube.CUBE_SCALE * 40.5f, world.player.Position.Z);

				RectangleF sourceRect = new RectangleF()
				{
					x = -world.player.Position.Z * 128 + this.alive,
					y = -world.player.Position.X * 128 + this.alive,
					width = 128 * 16,
					height = 128 * 16,
				};
				Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(materialLava,
					meshLavaQuad, mat, sourceRect));
			}
		}

        public override void Dispose()
        {
            base.Dispose();

			directionalLight.Dispose();
        }
    }
}
