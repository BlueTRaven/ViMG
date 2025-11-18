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
using ViMG.Rendering;
using ViMG.GameStates;
using ImGuiNET;
using MonoGame.ImGuiNet;
using TracyNative = Tracy;
using ViMG.TracyImpl;
using System.Diagnostics;
using ViMG.IMGUIImpl;
using Engine.Mods;

//Client-server separation
//Specifically, stuff like the player needs a way of separating client code from server code, as they should not be shipped together.
//Menus in particular
//Inventories will be kept on server, but menus do not need to be there
//We can rename renderer stuff to client stuff, and perform client-specific stuff there
//Or, we can keep renderer stuff separate. Client stuff becomes a third thing. 
//Client creates a list of ClientEntity that match Entities in EntityManager
//If an entity does not need a client entity, it's just null, but still in the same spot as in the entity in EntityManager
//This would require reworking how entities are laid out, since right now we do a naive O(n) remove when removing entities, which shuffles everything. 
//This is already bad, but it gets worse with clients, which have to do the same thing, so we do it twice
namespace ViMG
{
    public class Main : Game
    {
		public static event Action<Point> WindowResizedEvent;
		public static event EventHandler<TextInputEventArgs> WindowTextInputEvent;

        GraphicsDeviceManager graphics;
        SpriteBatch batch;

		public const float FOV_DEGREES = 90f;
		public const float NEAR = 0.005f;
		public const float FAR = 12 * Chunk.CHUNK_SIZE * Cubes.Cube.CUBE_SCALE;

		public static Effect VertexPositionColorDebugEffect;
		public static Effect VertexPositionTextureDebugEffect;

		//private World world;
		public static GameStateManager gameStateManager;

		public static Camera camera;
		public static Camera debugCamera;

		public static InputManager inputManager;
		public static ViMGAssetsManager assetsManager;
		public static RegistryService Registry;

		public static FrameCounter frameCounter;
		public static int Frame;

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

#if DEBUG
		public static bool Debug = true;
#else
		public static bool Debug = false;
#endif

		public static bool DebugChunks;
		public static string DEBUGPopupText = "";

		public static WorldViewProjection WVP;
		public static FogManager FogManager;
		public static SessionInformation SessionInformation;
		public static SessionIO SessionIO;

		public const int FIXED_FPS = 60;

		public const double FIXED_STEP = 1.0 / (double)FIXED_FPS;
		private double time;

		public static RenderTarget2D DepthTarget;
		public static RenderTarget2D WorldTarget;

		public static RendererDeferred Renderer;

		public static Thread MainThread;

		public static bool MouseControl;
		public static bool DrawCursor;

		public static RectangleF CrosshairSourceRect = new RectangleF(0, 0, 16, 16);

		//Debugging purposes only. Sometimes we want to run (semi)headless for profiling reasons.
		private const bool NO_RENDER = false;
		public const bool ENABLE_SHADOWS = true;
		public const bool ENABLE_PCF = true;
		public const bool DO_DETAIL = true;
		public const bool TRANSPARENT_ORES = false;
		public const bool ENABLE_ENT_SPAWNING = true;
		public const float RANDOM_UPDATES_TIME = 8f / 60f;
		public const int RANDOM_UPDATES_PER_CHUNK = 1;
		public const bool DO_RENDER_MESHING = true;
		public const bool DO_COLLISION_MESHING = true;
		public const bool MULTITHREADING = true;
		public const bool MULTITHREAD_BROAD_PHASE = MULTITHREADING && true;
		public const bool MULTITHREAD_LOADING = MULTITHREADING && true;
		public const bool MULTITHREAD_MESHING = MULTITHREADING && true;
		public const bool MULTITHREAD_UPLOADMESH = MULTITHREADING && true;

		public static double Time;
        public static bool IsHeadless = false;

        public static bool Exit = false;

		//public static bool WorldLoaded = false;

		//private MenuMain ui;

		private ImGuiRenderer imguiRenderer;

		private int numFrameTimes = 0;
		private float[] frameTimes = new float[256];

		private ModManager modManager = new ModManager();

        public Main(bool headless = false, bool cli = false) : base()
        {
			MainThread = Thread.CurrentThread;

			SessionInformation = new SessionInformation();
			SessionIO = new SessionIO();
			SessionIO.Load(graphics);

			graphics = new GraphicsDeviceManager(this)
			{
				GraphicsProfile = GraphicsProfile.HiDef,
				//PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
				SynchronizeWithVerticalRetrace = false,
				PreferredBackBufferWidth = Options.CurrentWindowResolution.X,
				PreferredBackBufferHeight = Options.CurrentWindowResolution.Y,
			};

            Content.RootDirectory = "Content";

			camera = new CameraPerspective(new Vector3(0, 0, 0), new Vector3(0, 180, 0), new Vector3(1), FOV_DEGREES, NEAR, FAR);
			debugCamera = new CameraPerspective(new Vector3(0, 0, 0), new Vector3(0, 180, 0), new Vector3(1), FOV_DEGREES, NEAR, FAR);
			assetsManager = new ViMGAssetsManager(Content);
			inputManager = new InputManager(this);
			frameCounter = new FrameCounter();

			//WorldSaver saver = new WorldSaver(null, null, SessionInformation);
			//saver.LoadSession();
        }

		protected override void Initialize()
		{
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

			Options.CenterMouse();

			DrawHelper.LoadContent(GraphicsDevice);

			IsFixedTimeStep = false;

			imguiRenderer = new ImGuiRenderer(this);
			imguiRenderer.RebuildFontAtlas();

            gameStateManager = new GameStateManager();
            gameStateManager.Initialize();

            base.Initialize();

			Window.TextInput += WindowTextInput;
			Window.ClientSizeChanged += WindowResolutionChanged;
			Window.AllowUserResizing = true;

			WorldTarget = new RenderTarget2D(GraphicsDevice, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);

			modManager.LoadModDlls();
			Registry = new RegistryService(GraphicsDevice);
			Registry.Register();

			//world = new World(GraphicsDevice, 512);

#if DEBUG
			//world.LoadWorld(GraphicsDevice, SessionInformation.LastLoadedSave);
			//Main.MouseControl = false;
			//Main.DrawCursor = false;
#endif

			Renderer = new RendererDeferred(GraphicsDevice);
		}

		private void WindowResolutionChanged(object? sender, EventArgs args)
        {
			Options.CurrentWindowResolution = new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
			WorldTarget?.Dispose();
			WorldTarget = new RenderTarget2D(GraphicsDevice, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y, 
				false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
			camera.MarkDirty();

			WindowResizedEvent?.Invoke(Options.CurrentWindowResolution);
		}

		private void WindowTextInput(object? sender, TextInputEventArgs args)
        {
			WindowTextInputEvent?.Invoke(sender, args);
        }

		protected override void LoadContent()
        {
			batch = new SpriteBatch(GraphicsDevice);
			assetsManager.LoadContent(Directory.GetCurrentDirectory() + "/Content");
			gameStateManager.LoadContent(GraphicsDevice);
		}

		protected override void Update(GameTime gt)
		{
            TracyImpl.Tracy.FrameMark();
			var zone = TracyImpl.Tracy.BeginZone();
		
            if (Exit)
				Exit();

			camera.FrameBegin();

			//WorldLoaded = world.LoadedFolderName != null;

			frameCounter.Update((float)gt.ElapsedGameTime.TotalSeconds);

			IsMouseVisible = DrawCursor;

			//if (WorldLoaded)
				//world.UnfixedUpdate();
			//else ui.Update(GraphicsDevice, gt.ElapsedGameTime.TotalSeconds);

			time += gt.ElapsedGameTime.TotalSeconds;
			while (time >= FIXED_STEP && !Exit)
			{
				time -= FIXED_STEP;

				FixedUpdate(FIXED_STEP * Options.DEBUGTimescale);
			}

			base.Update(gt);

			zone.End();
		}

		private void FixedUpdate(double deltaTime)
		{
			Frame += 1;
			Stopwatch watch = Stopwatch.StartNew();
            var zone = TracyImpl.Tracy.BeginZone();

            DEBUGPopupText = "";

			Time += deltaTime;

			inputManager.Update(new GameTime());

			if (inputManager.JustPressed(Keys.P))
			{
				paused = !paused;
				Options.CenterMouse();
			}

			if (!paused || inputManager.JustPressed(Keys.O))
			{
				if (inputManager.JustPressed(Keys.O))
					Options.CenterMouse();

				gameStateManager.Update(deltaTime);
				//if (WorldLoaded)
					//world.Update(deltaTime);
			}

			Renderer.Update(deltaTime);

			if (IsActive && !paused && !MouseControl)
				Options.CenterMouse();

			zone.End();

			watch.Stop();

			for (int i = 0; i <= frameTimes.Length - 2; i++)
			{
				frameTimes[i] = frameTimes[i + 1];
			}
			frameTimes[frameTimes.Length - 1] = (float)watch.Elapsed.TotalSeconds;
			
			if (numFrameTimes < frameTimes.Length)
				numFrameTimes += 1;
		}
		
        protected override void Draw(GameTime gameTime)
        {
			if (NO_RENDER)
				return;

            var zone = TracyImpl.Tracy.BeginZone();

            imguiRenderer.BeginLayout(gameTime);

            Renderer.FrameStart();

			GraphicsDevice.Clear(Color.White);

			Matrix view = camera.GetViewMatrix();

			WVP.SetView(view);
			
			gameStateManager.Draw(GraphicsDevice);

            //if (WorldLoaded)
            //world.Draw(GraphicsDevice, CubeLitEffect);
            IMGUIEntIODebug.Show();

            Renderer.Draw(batch);

			GraphicsDevice.SetRenderTarget(null);

			batch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, null);

			batch.Draw(Renderer.GetOutput().RenderTarget as RenderTarget2D, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
			//batch.Draw(WorldTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

			gameStateManager.DrawUI(batch);
			/*if (WorldLoaded)
				world.DrawUI(batch);
			else ui.Draw(batch);*/

			batch.Draw(assetsManager.GetAsset<Texture2D>("crosshair"), new Vector2(Options.CurrentWindowResolution.X / 2 - 8, 
				Options.CurrentWindowResolution.Y / 2 - 8), CrosshairSourceRect.ToRectangle(), Color.White);

			batch.End();

			batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, null);

			if (Debug)
			{
				//TextHelper.FontInfo font = new TextHelper.FontInfo(assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true, Color.Black);

				//TextHelper.DrawText(batch, font,
				//	frameCounter.AverageFramesPerSecond.ToString(), Color.White, new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
				//	Enums.Alignment.TopLeft, Options.CurrentWindowResolution.X, 0, TextHelper.OverFlowAction.None);
				//TextHelper.DrawText(batch, font,
				//	"\nPosition: " + FormatPos() + " Facing: " + FormatFacing() +
				//	"\nChunk Pos: " + ChunkPosition.WorldSpaceChunk(camera.Position).ToString(), Color.White, 
				//	new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
				//	Enums.Alignment.TopLeft, Options.CurrentWindowResolution.X, 0, TextHelper.OverFlowAction.None);

				//string queueStr = "\n\n\nNum Chunks Drawn: " + World.NumChunksDrawn + " in " + World.ChunkDrawTime + " seconds.";

				//TextHelper.DrawText(batch, font, queueStr,
				//	Color.White, new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
				//	Enums.Alignment.TopLeft, Options.CurrentWindowResolution.X, 0, TextHelper.OverFlowAction.None);

				//TextHelper.DrawText(batch, font, "GBuffer: " + Renderer.GetOutputString(),
				//	Color.White, new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
				//	Enums.Alignment.TopRight, Options.CurrentWindowResolution.X, 0, TextHelper.OverFlowAction.None);
				//TextHelper.DrawText(batch, font, "AA: " + Options.CurrentAntiAliasing.ToString() + 
				//	(Options.CurrentAntiAliasing == Options.AntiAliasing.SMAA ? " " + Options.CurrentSMAAQuality.ToString() : ""),
				//	Color.White, new Rectangle(0, (int)font.LineSpacing, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
				//	Enums.Alignment.TopRight, Options.CurrentWindowResolution.X, 0, TextHelper.OverFlowAction.None);
				//TextHelper.DrawText(batch, font, "Num Draw Calls: " + GraphicsDevice.Metrics.DrawCount,
				//	Color.White, new Rectangle(0, (int)font.LineSpacing * 2, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
				//	Enums.Alignment.TopRight, Options.CurrentWindowResolution.X, 0, TextHelper.OverFlowAction.None);
				//TextHelper.DrawText(batch, font, "Num Point Lights: " + RendererDeferred.NumPointLightsRendered + "(instanced: " + Options.UseInstancedLightVolumes + ")", 
				//	Color.White, new Rectangle(0, (int)font.LineSpacing * 3, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
				//	Enums.Alignment.TopRight, Options.CurrentWindowResolution.X, 0, TextHelper.OverFlowAction.None);

				//TextHelper.DrawText(batch, font, DEBUGPopupText,
				//	Color.White, new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
				//	Enums.Alignment.Left, Options.CurrentWindowResolution.X, 0, TextHelper.OverFlowAction.None);


				if (ImGui.GetIO().WantCaptureKeyboard)
				{
					inputManager.InputCaptured = true;
				}
				else inputManager.InputCaptured = false;

				if (ImGui.BeginMainMenuBar())
				{
					if (ImGui.BeginMenu("Menu"))
					{
						ImGui.MenuItem("Settings Menu", null, ref IMGUISettings.Show);
						ImGui.MenuItem("Debug Info Menu", null, ref IMGUISettings.ShowDebugInfo);
						ImGui.MenuItem("Console", null, ref Options.ShowConsole);
                        ImGui.EndMenu();
					}
					
					ImGui.EndMainMenuBar();
				}

				if (IMGUISettings.Show && ImGui.Begin("Settings", ref IMGUISettings.Show))
				{
					IMGUISettings.AutoIMGUI();
				}
				ImGui.End();

				if (IMGUISettings.ShowDebugInfo && ImGui.Begin("Debug Info", ref IMGUISettings.ShowDebugInfo))
				{
					ImGui.Text(string.Format("FPS: {0}", frameCounter.AverageFramesPerSecond.ToString()));
					ImGui.PlotLines("Fixed Update Frame Times", ref frameTimes[0], numFrameTimes, 0, null, 0, (float)(FIXED_STEP * 4), new(0, 80));
					ImGui.Text(string.Format("Chunks Drawn: {0} in {1} seconds", World.NumChunksDrawn, World.ChunkDrawTime));
					ImGui.Text(string.Format("Draw Calls: {0}", GraphicsDevice.Metrics.DrawCount));
					ImGui.Text(string.Format("Point Lights: {0}", RendererDeferred.NumPointLightsRendered));

					ImGui.Text(string.Format("Position: {0}", FormatPos()));
					ImGui.Text(string.Format("Facing: {0}", FormatFacing()));
					ImGui.Text(string.Format("Chunk Pos: {0}", ChunkPosition.WorldSpaceChunk(camera.Position).ToString()));

					if (gameStateManager.GetCurrentGameState() is GameStateTheIsland theIsland && theIsland.GetWorld() != null)
					{
						ImGui.Text(string.Format("Local player: {0}", theIsland.GetWorld().localPlayerIndex));

						if (gameStateManager.connectedType != GameStateManager.ConnectedType.Singleplayer)
						{
							for (int i = 0; i < World.MAX_PLAYERS; i++)
							{
								if (theIsland.netManager.netPlayers[i].playerId != -1)
									ImGui.Text(string.Format("Player {0}: {1}", i, theIsland.netManager.netPlayers[i].latency));
							}
						}
					}
				}
                ImGui.End();

				IMGUIConsole.Console();
			}


			batch.End();

            base.Draw(gameTime);

			while(GraphicsDevice.GraphicsDebug.TryDequeueMessage(out var message))
            {
				Console.WriteLine(message);
            }

            imguiRenderer.EndLayout();

            zone.End();
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

        protected override void OnExiting(object sender, EventArgs args)
        {
			IMGUIConsole.OnExiting();
            base.OnExiting(sender, args);
        }
    }
}
