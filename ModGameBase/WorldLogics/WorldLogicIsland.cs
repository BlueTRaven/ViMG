using BrUtility;
using Engine.Networking.Messages;
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
using ViMG.Entities;
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

		public WeatherManager? WeatherManager = null;

		private double timeSyncWeather;

		public float WeatherChangeTimer;
		private static Vector2 passiveWeatherTime = new Vector2(60 * 4f, 60 * 12f);
		private static Vector2 activeWeatherTime = new Vector2(60 * 2f, 60 * 12f);
		private const float ACTIVE_WEATHER_CHANCE = 0.25f;

        public WorldLogicIsland() : base()
        {
			
        }

        public override void FinishLoading(World world, GraphicsDevice device)
        {
            base.FinishLoading(world, device);

            WeatherManager = new WeatherManager(device);

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

			world.PassiveSpawnerManager?.AddPassiveSpawner(new PSMerchant(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager?.AddPassiveSpawner(new PSSlime(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager?.AddPassiveSpawner(new PSSKeleton(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager?.AddPassiveSpawner(new PSImp(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager?.AddPassiveSpawner(new PSCaveSlime(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager?.AddPassiveSpawner(new PSSnake(world.PassiveSpawnerManager, world.EntityManager));
			world.PassiveSpawnerManager?.AddPassiveSpawner(new PSStoneBeetle(world.PassiveSpawnerManager, world.EntityManager));
		}

		public override void Update(World world, double deltaTime)
		{
			base.Update(world, deltaTime);
			alive += (float)deltaTime;

            if (Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Server && Main.Time - timeSyncWeather > 1)
            {
                Main.Registry.MessageRegistry.SendMessageToAll(SyncWeather.Instance, Main.gameStateManager.TheIsland.netManager.netManager, null);
                timeSyncWeather = Main.Time;
            }

            foreach (Player player in world.player)
			{
				if (!world.WorldInfo.flags.Flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD) && player != null && player.Position.Y / Cube.CUBE_SCALE < 140)
				{
					Vector3 lavaPosition = new Vector3(player.Position.X, LAVA_HEIGHT + (Cube.CUBE_SCALE * 0.25f), player.Position.Z);

					if (player.Position.Y < lavaPosition.Y)
						player.Kill();
				}
			}

			var localPlayer = world.GetLocalPlayer();
			if (!world.WorldInfo.flags.Flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD) && localPlayer != null && localPlayer.Position.Y / Cube.CUBE_SCALE < 140)
			{
				Vector3 lavaPosition = new Vector3(localPlayer.Position.X, LAVA_HEIGHT + (Cube.CUBE_SCALE * 0.25f), localPlayer.Position.Z);

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

			if (WeatherChangeTimer <= 0 || Main.inputManager.JustPressed(Keys.L))
			{
				if (!WeatherManager.IsTransitioning())
				{
					bool isActive = Main.random.NextFloat() < ACTIVE_WEATHER_CHANCE;

					WeatherManager.WeatherType[] types;

					if (!isActive)
					{
						types = WeatherManager.PassiveWeatherTypes;
						WeatherChangeTimer = Main.random.NextFloat(passiveWeatherTime.X, passiveWeatherTime.Y);
					}
					else
					{
						types = WeatherManager.ActiveWeatherTypes;
						WeatherChangeTimer = Main.random.NextFloat(activeWeatherTime.X, activeWeatherTime.Y);
					}

					WeatherManager.WeatherType nextWeather = types[Main.random.Next(0, types.Length)];

					WeatherManager.DoTransition(nextWeather, 15f);
				}
			}
			else WeatherChangeTimer -= (float)deltaTime;

			//below this point, don't even bother updating the directional light as we can't see any of it anyway. It should have no contribution to the scene.
			if (localPlayer != null && CubePosition.FromWorldSpace(localPlayer.Position).Y > 140)
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

				WeatherManager.Update(deltaTime, world, directionalLight, ref lightDir, ref lightColor, out bool lightNeedsUpdateFromWeather);

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

			if (Main.inputManager.JustPressed(Keys.V))
			{
				//world.ChunkManager.CubeView.TestPalettize(ChunkPosition.WorldSpaceChunk(world.GetLocalPlayer().Position));
				world.EntityManager.Add(new SnakeFlying(world.GetLocalPlayer().Position - Main.camera.Forward * Cube.CUBE_SCALE * 5f));

                //var visStats = new ProjectileManager.ProjectileVisStats(new RectangleF(0, 16, 16, 16), Cube.CUBE_SCALE);
                //visStats.rollFollowsVelocity = true;

                //world.ProjectileManager.Add(new ProjectileManager.Projectile(this, Position - Main.camera.Forward * Cube.CUBE_SCALE * 5f,
                //	-Main.camera.Forward * Cube.CUBE_SCALE * 0.25f, 10,
                //	visStats, new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 1, Cube.CUBE_SCALE * 1f, Cube.CUBE_SCALE * 0.125f, Cube.CUBE_SCALE)),
                //	new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.5f), new Vector3(Cube.CUBE_SCALE)));

                //for (int i = 0; i < 8; i++)
                //world.EntityManager.Add(new SkullheadEye(Position - Main.camera.Forward * Cube.CUBE_SCALE * 5f, slime));
                //world.EntityManager.Add(new ManaStar(new Vector2(Main.random.NextFloat(-70, 70), Main.random.NextFloat(-180, 180))));
                //world.EntityManager.Add(new Lightning(Position - Main.camera.Forward * Cube.CUBE_SCALE * 5));
            }
        }

        public override bool AllowsLoadingNextLayer(World world)
        {
			return world.WorldInfo.flags.Flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD);
        }

        public override void Draw(World world, GraphicsDevice device)
        {
            base.Draw(world, device);

			WeatherManager.Draw(device, world);
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
				Matrix.CreateTranslation(Main.camera.Position),
				tintColor: Color.White * (1 - world.WeatherSkyboxAlpha)));

            if (world.GetLocalPlayer() != null && !world.WorldInfo.flags.Flags.HasFlag(WorldFlags.FlagValues.SKULLHEAD_DEAD) && world.GetLocalPlayer().Position.Y / Cube.CUBE_SCALE < 140)
			{
				Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE * 512, 1, Cube.CUBE_SCALE * 512) *
					Matrix.CreateTranslation(world.player[world.localPlayerIndex].Position.X, Cube.CUBE_SCALE * 40.5f, world.GetLocalPlayer().Position.Z);

				RectangleF sourceRect = new RectangleF()
				{
					x = -world.player[world.localPlayerIndex].Position.Z * 128 + this.alive,
					y = -world.player[world.localPlayerIndex].Position.X * 128 + this.alive,
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
