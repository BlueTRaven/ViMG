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
using MonoGame.ImGuiNet;
using TracyNative = Tracy;
using ViMG.TracyImpl;
using System.Diagnostics;
using ViMG.IMGUIImpl;
using Engine.Mods;
using Engine;
using Engine.Entities;
using Engine.Common;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using Engine.IMGUIImpl;

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

		public static InputManager inputManager;

		public static FrameCounter frameCounter;
		public static int Frame;

		public static RasterizerState genericRS;
		public static RasterizerState reverseRS;
		public static DepthStencilState genericDSS;
		public static RasterizerState wireframeRS;
		public static DepthStencilState nodepthDSS;
		public static RasterizerState noCullRS;
		public static SamplerState clampSS;
		public static SamplerState shadowBorderClampSS;

		private bool paused;


		public static bool DebugChunks;
		public static string DEBUGPopupText = "";

		public static FogManager FogManager;

		public const int FIXED_FPS = 60;

		public const double FIXED_STEP = 1.0 / (double)FIXED_FPS;
		private double time;

		public static double TimeP = 0;

		public static bool MouseControl;
		public static bool DrawCursor;

		public static RectangleF CrosshairSourceRect = new RectangleF(0, 0, 16, 16);

		//Debugging purposes only. Sometimes we want to run (semi)headless for profiling reasons.
		private const bool NO_RENDER = false;
		public const bool ENABLE_SHADOWS = true;
		public const bool ENABLE_PCF = true;
		
		public const bool TRANSPARENT_ORES = false;
		
		public const bool DO_RENDER_MESHING = true;
		public const bool DO_COLLISION_MESHING = true;
		
		private ImGuiRenderer imguiRenderer;

		private int numFrameTimes = 0;
		private float[] frameTimes = new float[256];

		private Runner runner;

        public Main(string[] args) : base()
        {
			//FieldTest.DoTest();

			runner = new Runner();

			//GlobalState.MainThread = Thread.CurrentThread;

   //         GlobalState.SessionInformation = new SessionInformation();
   //         GlobalState.SessionIO = new SessionIO();
   //         GlobalState.SessionIO.Load();

			graphics = new GraphicsDeviceManager(this)
			{
				GraphicsProfile = GraphicsProfile.HiDef,
				//PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
				SynchronizeWithVerticalRetrace = false,
				PreferredBackBufferWidth = Options.CurrentWindowResolution.X,
				PreferredBackBufferHeight = Options.CurrentWindowResolution.Y,
			};

			if (GlobalState.Args.windowPosition != null)
			{
				this.Window.Position = GlobalState.Args.windowPosition.Value;
			}

            Content.RootDirectory = "Content";

            //GlobalState.AssetsManager = new ViMGAssetsManager(Content);
			inputManager = new InputManager(this);
			frameCounter = new FrameCounter();

			//WorldSaver saver = new WorldSaver(null, null, SessionInformation);
			//saver.LoadSession();
        }

		protected override void Initialize()
		{
			runner.Initialize(Content);

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
			//imguiRenderer.RebuildFontAtlas();

            //GlobalState.GameStateManager = new GameStateManager();
            //GlobalState.GameStateManager.Initialize();

            base.Initialize();

			Window.TextInput += WindowTextInput;
			Window.ClientSizeChanged += WindowResolutionChanged;
			Window.AllowUserResizing = true;

			runner.Register(GraphicsDevice);

			//world = new World(GraphicsDevice, 512);

#if DEBUG
			//world.LoadWorld(GraphicsDevice, SessionInformation.LastLoadedSave);
			//Main.MouseControl = false;
			//Main.DrawCursor = false;
#endif
		}

		private void WindowResolutionChanged(object? sender, EventArgs args)
        {
			Options.CurrentWindowResolution = new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
			if (GlobalState.GameStateManager.GetCurrentGameState() is GameStateTheIsland theIsland)
			{
				theIsland.GetClient()?.currInterpState.camera.MarkDirty();
				theIsland.GetClient()?.Current().camera.MarkDirty();
			}

			WindowResizedEvent?.Invoke(Options.CurrentWindowResolution);
		}

		private void WindowTextInput(object? sender, TextInputEventArgs args)
        {
			WindowTextInputEvent?.Invoke(sender, args);
        }

		protected override void LoadContent()
        {
			batch = new SpriteBatch(GraphicsDevice);
			runner.LoadContent();
            GlobalState.GameStateManager.LoadContent(GraphicsDevice);
		}

		protected override void Update(GameTime gt)
		{
            TracyImpl.Tracy.FrameMark();
			var zone = TracyImpl.Tracy.BeginZone();
		
            if (GlobalState.Exit)
				Exit();

			//WorldLoaded = world.LoadedFolderName != null;

			frameCounter.Update((float)gt.ElapsedGameTime.TotalSeconds);

			int numUpdates = runner.UnfixedUpdate(gt.ElapsedGameTime);
			for (int i = 0; i < numUpdates; i++)
			{
				runner.FixedUpdate(FIXED_STEP * Options.DEBUGTimescale);
				FixedUpdate(FIXED_STEP * Options.DEBUGTimescale);
			}

			if (GlobalState.GameStateManager.netMode != GameStateManager.NetworkingMode.Server)
			{
				IsMouseVisible = DrawCursor;
			}
			else
			{
				IsMouseVisible = true;
			}

			//time += gt.ElapsedGameTime.TotalSeconds;
			//while (time >= FIXED_STEP && !GlobalState.Exit)
			//{
			//	time -= FIXED_STEP;

			//	FixedUpdate(FIXED_STEP * Options.DEBUGTimescale);
			//}

			TimeP = time / FIXED_STEP;

			base.Update(gt);

			zone.End();
		}

		private void FixedUpdate(double deltaTime)
		{
			Frame += 1;
			Stopwatch watch = Stopwatch.StartNew();
            var zone = TracyImpl.Tracy.BeginZone();

            DEBUGPopupText = "";

            GlobalState.Time += deltaTime;

			inputManager.Update(new GameTime());

			if (inputManager.JustPressed(Keys.F1))
			{
                GlobalState.Debug = !GlobalState.Debug;
			}

			if (inputManager.JustPressed(Keys.P))
			{
				paused = !paused;
				Options.CenterMouse();
			}

			if (!paused || inputManager.JustPressed(Keys.O))
			{
				if (inputManager.JustPressed(Keys.O))
					Options.CenterMouse();

                //GlobalState.GameStateManager.Update(deltaTime);
				//if (WorldLoaded)
					//world.Update(deltaTime);
			}

			if (GlobalState.GameStateManager.netMode != GameStateManager.NetworkingMode.Server)
			{
				if (IsActive && !paused && !MouseControl)
					Options.CenterMouse();
			}

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

            imguiRenderer.BeforeLayout(gameTime);

			GraphicsDevice.Clear(Color.White);

            GlobalState.GameStateManager.Draw(GraphicsDevice, batch, gameTime.ElapsedGameTime.TotalSeconds);

            //if (WorldLoaded)
            //world.Draw(GraphicsDevice, CubeLitEffect);
            IMGUIEntIODebug.Show();
            IMGUINetworkDebug.Show();
			IMGUIClientEntityInspector.Show();
			Engine.Logger.DoImgui();

            //Renderer.Draw(batch);

            GraphicsDevice.SetRenderTarget(null);

			batch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, null);

            GlobalState.GameStateManager.DrawUI(batch);
			
			batch.End();

			batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, null);

			if (GlobalState.Debug)
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
						ImGui.MenuItem("Settings Menu", (string)null, ref IMGUISettings.Show);
						ImGui.MenuItem("Debug Info Menu", (string)null, ref IMGUISettings.ShowDebugInfo);
						ImGui.MenuItem("Console", (string)null, ref Options.ShowConsole);
						ImGui.MenuItem("Log Settings", (string)null, ref Engine.Logger.ShowLogSettings);
                        ImGui.EndMenu();
					}
					
					ImGui.EndMainMenuBar();
				}

				if (IMGUISettings.Show && ImGui.Begin("Settings", ref IMGUISettings.Show))
				{
					IMGUISettings.AutoIMGUI();

                    ImGui.End();
                }

				if (IMGUISettings.ShowDebugInfo && ImGui.Begin("Debug Info", ref IMGUISettings.ShowDebugInfo))
				{
					ImGui.Text(string.Format("FPS: {0}", frameCounter.AverageFramesPerSecond.ToString()));
					ImGui.PlotLines("Fixed Update Frame Times", ref frameTimes[0], numFrameTimes, (string)null, (float)FIXED_STEP * 4);
					ImGui.Text(string.Format("Chunks Drawn: {0} in {1} seconds", World.NumChunksDrawn, World.ChunkDrawTime));
					ImGui.Text(string.Format("Draw Calls: {0}", GraphicsDevice.Metrics.DrawCount));
					ImGui.Text(string.Format("Point Lights: {0}", RendererDeferred.NumPointLightsRendered));

					Engine.Common.Camera? camera = GlobalState.GameStateManager.TheIsland.GetClient()?.Current().camera;
                    ImGui.Text(string.Format("Position: {0}", camera?.Position));
                    var fwd = camera?.Forward ?? Vector3.Zero;
					var pitchyaw = camera?.RotationEuler ?? Vector3.Zero;
                    ImGui.Text(string.Format("Facing: {0:0.00} {1:0.00} {2:0.00}\n" +
						"Yaw: {3:0.00} Pitch: {4:0.00}", fwd.X, fwd.Y, fwd.Z, pitchyaw.Y, pitchyaw.X));
					ImGui.Text(string.Format("Chunk Pos: {0}", ChunkPosition.WorldSpaceChunk(camera?.Position ?? new()).ToString()));

					if (GlobalState.GameStateManager.GetCurrentGameState() is GameStateTheIsland theIsland && theIsland.GetWorld() != null)
					{
						ImGui.Text(string.Format("Local player: {0}", theIsland.GetWorld().localPlayerIndex));

						if (theIsland.netManagerServer != null)
						{
							theIsland.netManagerServer.IMGUIDebug();

							for (int i = 0; i < World.MAX_PLAYERS; i++)
							{
								if (theIsland.netManagerServer.netPlayers[i].playerId != -1)
									ImGui.Text(string.Format("Player {0}: {1}", i, theIsland.netManagerServer.netPlayers[i].latency));
							}
						}
						if (theIsland.netManagerClient != null)
						{
							theIsland.netManagerClient.IMGUIDebug();
						}
					}
					ImGui.End();
				}

				IMGUIConsole.Console();
			}


			batch.End();

            base.Draw(gameTime);

			while(GraphicsDevice.GraphicsDebug.TryDequeueMessage(out var message))
            {
				Console.WriteLine(message);
            }

            imguiRenderer.AfterLayout();

            zone.End();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
			IMGUIConsole.OnExiting();
            base.OnExiting(sender, args);
        }
    }
}
