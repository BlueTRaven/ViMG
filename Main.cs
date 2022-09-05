using A1r.Input;
using BrUtility;
using BrAssetsManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using ViMG.UIs;

namespace ViMG
{
    public class Main : Game
    {
		public static Point WindowResolution = new Point(800, 480);
		public static float AspectRatio => (float)WindowResolution.X / (float)WindowResolution.Y;

        GraphicsDeviceManager graphics;
        SpriteBatch batch;

		private const float FOV_DEGREES = 90f;
		public const float NEAR = 0.005f;
		public const float FAR = 6 * Chunk.CHUNK_SIZE * Cubes.Cube.CUBE_SCALE + (Cubes.Cube.CUBE_SCALE * 64);

		public static BasicEffect BasicEffect;
		public static Effect VertexPositionColorDebugEffect;
		public static Effect VertexPositionTextureDebugEffect;
		public static Effect CubeLitEffect;
		public static Effect CubeUnlitEffect;

		private World world;

		public static Camera camera;
		public static Camera debugCamera;

		public static InputManager inputManager;
		public static ViMGAssetsManager assetsManager;
		public static RegistryService Registry;

		public static DelayedUploader<VertexPositionColorTextureNormal, int> DelayedUploaderChunkMesh = new DelayedUploader<VertexPositionColorTextureNormal, int>();

		public static FrameCounter frameCounter;

		public static Random random = new Random(SEED);

		public const int SEED = 1338;

		public static RasterizerState genericRS;
		public static RasterizerState reverseRS;
		public static DepthStencilState genericDSS;
		public static RasterizerState wireframeRS;
		public static DepthStencilState nodepthDSS;
		public static RasterizerState noCullRS;
		public static SamplerState clampSS;
		public static SamplerState shadowBorderClampSS;

		private bool paused;

		public static bool Debug = true;
		public static bool DebugChunks;
		
		public static WorldViewProjection WVP;
		public static FogManager FogManager;
		public static LightManager LightManager;
		public static SessionInformation SessionInformation;

		public const int FIXED_FPS = 60;

		public const double FIXED_STEP = 1.0 / (double)FIXED_FPS;
		private double time;

		public static RenderTarget2D DepthTarget;
		public static RenderTarget2D WorldTarget;

		public static Thread MainThread;

		public static bool MouseControl;
		public static bool DrawCursor;

		//Debugging purposes only. Sometimes we want to run (semi)headless for profiling reasons.
		private const bool NO_RENDER = false;
		public const bool ENABLE_SHADOWS = true;
		public const bool ENABLE_PCF = true;

		public static bool Exit = false;

		public static bool WorldLoaded = false;

		private UIMainMenu ui;

        public Main()
        {
			MainThread = Thread.CurrentThread;

			graphics = new GraphicsDeviceManager(this)
			{
				GraphicsProfile = GraphicsProfile.HiDef,
				//PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
				SynchronizeWithVerticalRetrace = false,
				PreferredBackBufferWidth = WindowResolution.X,
				PreferredBackBufferHeight = WindowResolution.Y,
			};

            Content.RootDirectory = "Content";

			camera = new CameraPerspective(new Vector3(0, 0, 0), new Vector3(0, 180, 0), new Vector3(1), FOV_DEGREES, NEAR, FAR);
			debugCamera = new CameraPerspective(new Vector3(0, 0, 0), new Vector3(0, 180, 0), new Vector3(1), FOV_DEGREES, NEAR, FAR);
			assetsManager = new ViMGAssetsManager(Content);
			inputManager = new InputManager(this);
			frameCounter = new FrameCounter();

			SessionInformation = new SessionInformation();
			WorldSaver saver = new WorldSaver(null, null, SessionInformation);
			saver.LoadSession();
        }

		protected override void Initialize()
		{
			WVP.SetProjection(camera.GetProjectionMatrix());
			//WVP.SetProjection(Matrix.CreateOrthographicOffCenter(-10.0f, 10.0f, -10.0f, 10.0f, NEAR, FAR));

			BasicEffect = new BasicEffect(GraphicsDevice);
			BasicEffect.Projection = camera.GetProjectionMatrix();
			BasicEffect.TextureEnabled = true;
			BasicEffect.VertexColorEnabled = true;

			genericDSS = new DepthStencilState()
			{
				DepthBufferEnable = true,
				DepthBufferFunction = CompareFunction.LessEqual,
			};

			genericRS = new RasterizerState()
			{
				FillMode = FillMode.Solid,
				CullMode = CullMode.CullCounterClockwiseFace,
				//DepthClipEnable = true,
				//DepthBias = 0.5f
			};

			reverseRS = new RasterizerState()
			{
				FillMode = FillMode.Solid,
				CullMode = CullMode.CullClockwiseFace
			};

			nodepthDSS = new DepthStencilState()
			{
				DepthBufferEnable = false
			};

			wireframeRS = new RasterizerState()
			{
				FillMode = FillMode.WireFrame,
				CullMode = CullMode.None,
			};

			noCullRS = new RasterizerState()
			{
				FillMode = FillMode.Solid,
				CullMode = CullMode.None
			};

			clampSS = new SamplerState()
			{
				AddressU = TextureAddressMode.Clamp,
				AddressV = TextureAddressMode.Clamp,
				Filter = TextureFilter.Point,
				MaxMipLevel = 4,
			};

			shadowBorderClampSS = new SamplerState()
			{
				AddressU = TextureAddressMode.Border,
				AddressV = TextureAddressMode.Border,
				BorderColor = Color.Black,
				Filter = TextureFilter.Point,
				MaxMipLevel = 0,
				MaxAnisotropy = 0,
			};

			GraphicsDevice.DepthStencilState = genericDSS;
			GraphicsDevice.RasterizerState = genericRS;

			Mouse.SetPosition(WindowResolution.X / 2, WindowResolution.Y / 2);

			DrawHelper.LoadContent(GraphicsDevice);

			IsFixedTimeStep = false;

			base.Initialize();

			//DepthTarget = new RenderTarget2D(GraphicsDevice, 1024, 1024, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
			WorldTarget = new RenderTarget2D(GraphicsDevice, WindowResolution.X, WindowResolution.Y, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
			//GraphicsDevice.SetRenderTarget(WorldTarget);

			Registry = new RegistryService();
			Registry.Register();

			world = new World(GraphicsDevice, 512);
			//world.LoadWorld("flat01");
		}

        protected override void LoadContent()
        {
			batch = new SpriteBatch(GraphicsDevice);
			assetsManager.LoadContent(Directory.GetCurrentDirectory() + "/Content");

			CubeLitEffect = assetsManager.GetAsset<Effect>("cube_lit");
			CubeUnlitEffect = assetsManager.GetAsset<Effect>("cube_unlit");
			FogManager = new FogManager(CubeLitEffect, CubeUnlitEffect);
			LightManager = new LightManager();

			//CubeEffect.Parameters["AOStrength"].SetValue(0.5f);
			CubeLitEffect.Parameters["AmbientStrength"].SetValue(1f);
			CubeLitEffect.Parameters["SpecularStrength"].SetValue(1f);
			CubeLitEffect.Parameters["LightColor"].SetValue(Color.White.ToVector3());
			CubeLitEffect.Parameters["AmbientColor"].SetValue(Color.White.ToVector3());
			CubeLitEffect.Parameters["TintColor"].SetValue(Color.White.ToVector3());
			CubeLitEffect.Parameters["EnableFog"].SetValue(false);
			//CubeEffect.Parameters["LightResolution"].SetValue(new Vector2(1024));

			VertexPositionColorDebugEffect = assetsManager.GetAsset<Effect>("debug_vpc");
			VertexPositionColorDebugEffect.Name = "VertexPositionColorDebugEffect";
			VertexPositionColorDebugEffect.Parameters["DiffuseColor"].SetValue(Color.White.ToVector4());
			VertexPositionTextureDebugEffect = assetsManager.GetAsset<Effect>("debug_vpt");
			VertexPositionTextureDebugEffect.Name = "VertexPositionTextureDebugEffect";
			VertexPositionTextureDebugEffect.Parameters["DiffuseColor"].SetValue(Color.White.ToVector4());

			FogManager.Set(1200f, 2000f, assetsManager.GetAsset<Texture2D>("heightmap_layer1_day"), assetsManager.GetAsset<Texture2D>("heightmap_layer1_night"), 0);
			LightManager.SetToEffect(CubeLitEffect);

			ui = new UIMainMenu();
		}

		protected override void Update(GameTime gt)
		{
			if (Exit)
				Exit();

			WorldLoaded = world.LoadedFolderName != null;

			frameCounter.Update((float)gt.ElapsedGameTime.TotalSeconds);

			IsMouseVisible = DrawCursor;

			if (WorldLoaded)
				world.UnfixedUpdate();
			else ui.Update(world);

			time += gt.ElapsedGameTime.TotalSeconds;
			while (time >= FIXED_STEP && !Exit)
			{
				time -= FIXED_STEP;

				FixedUpdate(FIXED_STEP);
			}

			base.Update(gt);
		}

		private void FixedUpdate(double deltaTime)
		{
			inputManager.Update(new GameTime());

			if (inputManager.JustPressed(Keys.P))
			{
				paused = !paused;
				Mouse.SetPosition(WindowResolution.X / 2, WindowResolution.Y / 2);
			}

			if (!paused || inputManager.JustPressed(Keys.O))
			{
				if (inputManager.JustPressed(Keys.O))
					Mouse.SetPosition(WindowResolution.X / 2, WindowResolution.Y / 2);

				if (WorldLoaded)
					world.Update(deltaTime);
			}

			CubeLitEffect.Parameters["CameraPos"].SetValue(-camera.Position);
			CubeUnlitEffect.Parameters["CameraPos"].SetValue(-camera.Position);
			//CubeEffect.Parameters["LightPos"].SetValue(-camera.Position);

			if (IsActive && !paused && !MouseControl)
				Mouse.SetPosition(WindowResolution.X / 2, WindowResolution.Y / 2);
		}
		
        protected override void Draw(GameTime gameTime)
        {
			if (NO_RENDER)
				return;

			GraphicsDevice.Clear(Color.White);

			Matrix view = camera.GetViewMatrix();

			WVP.SetView(view);
			CubeLitEffect.Parameters["View"].SetValue(view);

			BasicEffect.View = view;

			if (WorldLoaded)
				world.Draw(GraphicsDevice, CubeLitEffect);

			GraphicsDevice.SetRenderTarget(null);

			batch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, null);

			batch.Draw(WorldTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

			if (WorldLoaded)
				world.DrawUI(batch);
			else ui.Draw(batch);

			batch.Draw(assetsManager.GetAsset<Texture2D>("crosshair"), new Vector2(WindowResolution.X / 2 - 8, WindowResolution.Y / 2 - 8), null, Color.White);

			batch.End();

			batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, null);

			if (Debug)
			{
				TextHelper.FontInfo font = new TextHelper.FontInfo(assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true, Color.Black);

				TextHelper.DrawText(batch, font,
					frameCounter.AverageFramesPerSecond.ToString(), Color.White, new Rectangle(0, 0, WindowResolution.X, WindowResolution.Y),
					Enums.Alignment.TopLeft, WindowResolution.X, 0, TextHelper.OverFlowAction.None);
				TextHelper.DrawText(batch, font,
					"\nPosition: " + FormatPos() + " Facing: " + FormatFacing() +
					"\nChunk Pos: " + ChunkPosition.WorldSpaceChunk(camera.Position).ToString(), Color.White, new Rectangle(0, 0, WindowResolution.X, WindowResolution.Y),
					Enums.Alignment.TopLeft, WindowResolution.X, 0, TextHelper.OverFlowAction.None);

				string queueStr = "\n\n\nNum Chunks Drawn: " + World.NumChunksDrawn + " in " + World.ChunkDrawTime + " seconds."
					+ "\nChunk Queue: " + ChunkManager.QueueGenerate + "/" + ChunkManager.QueueMesh;

				TextHelper.DrawText(batch, font, queueStr,
					Color.White, new Rectangle(0, 0, WindowResolution.X, WindowResolution.Y),
					Enums.Alignment.TopLeft, WindowResolution.X, 0, TextHelper.OverFlowAction.None);
			}


			batch.End();

            base.Draw(gameTime);

			while(GraphicsDevice.GraphicsDebug.TryDequeueMessage(out var message))
            {
				Console.WriteLine(message);
            }
        }

		private string FormatPos()
		{
			string x = String.Format("{0:0.00}", camera.Position.X);
			string y = String.Format("{0:0.00}", camera.Position.Y);
			string z = String.Format("{0:0.00}", camera.Position.Z);

			return x + " " + y + " " + z;
		}

		private string FormatFacing()
		{
			string x = String.Format("{0:0.00}", -camera.Forward.X);
			string y = String.Format("{0:0.00}", -camera.Forward.Y);
			string z = String.Format("{0:0.00}", -camera.Forward.Z);

			return x + " " + y + " " + z;
		}
    }
}
