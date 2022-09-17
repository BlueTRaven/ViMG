using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
//using SimplexNoise;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Generation;
using ViMG.Items;
using ViMG.Spawners;

namespace ViMG
{
	public class World
	{
		public string LoadedFolderName;

		public const float GRAVITY = -9.8f / 20f * Cube.CUBE_SCALE;
		public const float DAY_CYCLE_TIME = 60f * 10f;

		private const float SUN_DISTANCE = -6 * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;
		private const float SUN_ANGLE = 5f;	//rotate 5 degrees
		public readonly int sizeInChunks;
		public readonly int sizeInCubes;

		public ChunkManager ChunkManager;
		private ChunkManager ChunkManager2;	//Test to see if we can have 2 worlds loaded at once

		private static SimpleMesh<VertexPositionColor, int> meshWireframeCube;
		private static SimpleMesh<VertexPositionColor, int> meshWireframeUnscaled;
		private static SimpleMesh<VertexPositionColorTextureNormal, int> meshMiningCube;
		private static SimpleMesh<VertexPositionColorTextureNormal, int> meshSun;
		private static (VertexBuffer VBO, IndexBuffer IBO) meshUVSphere;

		private static SimpleMesh<VertexPositionColorTextureNormal, int> meshMaxDrawDistBottom;
		private static bool meshesLoaded;

		public Player player;
		private Vector3 playerStartPos;

		public int DrawDistanceHoriz = 6;   //radius in chunks that we should be able to see
		public int DrawDistanceVert = 6;
		public int DrawRadius = 6;

		public HitboxManager HitboxManager = new HitboxManager(32);
		public ProjectileManager ProjectileManager;
		public EntityManager EntityManager;
		public LightManager LightManager;
		public List<PassiveSpawner> Spawners = new List<PassiveSpawner>();

		public Color SkyColor = new Color(94, 107, 154);

		public List<ChunkPosition> CulledChunkDrawPositions = new List<ChunkPosition>();
		private bool chunkDrawPositionsDirty = true;
		private ChunkPosition oldChunkPosition;
		private Vector3 oldCameraRotation;

		private struct MinedCube
		{
			public CubePosition position;
			public Chunk chunk;
			public float timer;
			public int progress;	//goes up one per "mine"
		}

		private Dictionary<CubePosition, MinedCube> miningCubes = new Dictionary<CubePosition, MinedCube>();
		private List<CubePosition> miningRemove = new List<CubePosition>();
		private List<MinedCube> miningUpdate = new List<MinedCube>();

		private WorldSaver saver;
		private WorldSaver saver2;

		public ChunkLoadManager ChunkLoadManager;
		private ChunkLoadManager ChunkLoadManager2;

		private const float SUN_LIGHT_DISTANCE = -Cube.CUBE_SCALE * 10;
		public DirectionalLight directionalLight;
		private int currentCascadeDebug;

		public World(GraphicsDevice device, int worldSize)
		{
			this.sizeInCubes = worldSize;

			sizeInChunks = (int)((float)worldSize / Chunk.CHUNK_SIZE);

			if (!meshesLoaded)
				CreateMeshes(device);

			ChunkManager = new ChunkManager(device, sizeInChunks, sizeInCubes, this);
			//ChunkManager2 = new ChunkManager(device, sizeInChunks, sizeInCubes, this);

			ProjectileManager = new ProjectileManager(device);
			EntityManager = new EntityManager(this);
			LightManager = new LightManager(device);
			LightManager.UpdateDatas(Main.CubeLitEffect);
			Spawners.Add(new PSSlime(EntityManager));

			Main.CubeLitEffect.Parameters["WorldSize"].SetValue(new Vector3(worldSize));
			Main.CubeLitEffect.Parameters["CubeSize"].SetValue(new Vector3(Cube.CUBE_SCALE));
			Main.CubeUnlitEffect.Parameters["WorldSize"].SetValue(new Vector3(worldSize));
			Main.CubeUnlitEffect.Parameters["CubeSize"].SetValue(new Vector3(Cube.CUBE_SCALE));

			directionalLight = new DirectionalLight(device, Main.camera, Main.camera.Near, Main.camera.Far / 50f, 
				new float[] { 1f / 50f,  1f / 25f, 1f / 10f, 1f / 2f});
		}

		private void CreateMeshes(GraphicsDevice device)
        {
			meshWireframeCube = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			meshWireframeUnscaled = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(1), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			meshMiningCube = Cube.MakeCubeWithCorrectedTextureCoordinates(device, Color.White, Main.assetsManager.GetAsset<Texture2D>("mine"));

			Vector3 min = Vector3.Zero;
			Vector3 max = new Vector3(DrawDistanceHoriz * 2 * (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE));
			max.Y = 0;
			Vector3 a = new Vector3(min.X, min.Y, min.Z);
			Vector3 b = new Vector3(max.X, min.Y, min.Z);
			Vector3 c = new Vector3(max.X, min.Y, max.Z);
			Vector3 d = new Vector3(min.X, min.Y, max.Z);

			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
			List<int> indices = new List<int>();

			indices.Add(0);
			indices.Add(1);
			indices.Add(3);
			indices.Add(1);
			indices.Add(2);
			indices.Add(3);

			vertices.Add(new VertexPositionColorTextureNormal(a, Color.Black, Vector2.Zero, new Vector3(0, 1, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(b, Color.Black, Vector2.Zero, new Vector3(0, 1, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.Black, Vector2.Zero, new Vector3(0, 1, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.Black, Vector2.Zero, new Vector3(0, 1, 0)));

			max.Y = DrawDistanceHoriz * 2 * (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);
			Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
			Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
			Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
			Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);

			Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
			Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
			Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
			Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(r_t_n, Color.White, new Vector2(1, 1), new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(l_t_n, Color.White, new Vector2(0, 1), new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_n, Color.White, new Vector2(0, 0), new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_n, Color.White, new Vector2(1, 0), new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(r_t_f, Color.White, new Vector2(1, 1), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_t_n, Color.White, new Vector2(0, 1), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_n, Color.White, new Vector2(0, 0), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_f, Color.White, new Vector2(1, 0), new Vector3(-1, 0, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(l_t_f, Color.White, new Vector2(1, 1), new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(r_t_f, Color.White, new Vector2(0, 1), new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_f, Color.White, new Vector2(0, 0), new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_f, Color.White, new Vector2(1, 0), new Vector3(0, 0, -1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(l_t_n, Color.White, new Vector2(1, 1), new Vector3(1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(l_t_f, Color.White, new Vector2(0, 1), new Vector3(1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_f, Color.White, new Vector2(0, 0), new Vector3(1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_n, Color.White, new Vector2(1, 0), new Vector3(1, 0, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(l_b_f, Color.White, new Vector2(1, 0), new Vector3(0, -1, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_f, Color.White, new Vector2(0, 0), new Vector3(0, -1, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_n, Color.White, new Vector2(0, 0), new Vector3(0, -1, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_n, Color.White, new Vector2(1, 0), new Vector3(0, -1, 0)));

			meshMaxDrawDistBottom = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, DrawHelper.WhitePixel);

			List<VertexPositionColorTextureNormal> sunVertices = new List<VertexPositionColorTextureNormal>();
			List<int> sunIndices = new List<int>();

			sunIndices.Add(0);
			sunIndices.Add(1);
			sunIndices.Add(3);
			sunIndices.Add(1);
			sunIndices.Add(2);
			sunIndices.Add(3);

			const float SUN_VERT_DIST = Cube.CUBE_SCALE * 4;
			sunVertices.Add(new VertexPositionColorTextureNormal(new Vector3(-SUN_VERT_DIST, -SUN_VERT_DIST, 0), Color.Yellow, Vector2.Zero, new Vector3(0, 0, -1)));
			sunVertices.Add(new VertexPositionColorTextureNormal(new Vector3(-SUN_VERT_DIST, SUN_VERT_DIST, 0), Color.Yellow, Vector2.Zero, new Vector3(0, 0, -1)));
			sunVertices.Add(new VertexPositionColorTextureNormal(new Vector3(SUN_VERT_DIST, SUN_VERT_DIST, 0), Color.Yellow, Vector2.Zero, new Vector3(0, 0, -1)));
			sunVertices.Add(new VertexPositionColorTextureNormal(new Vector3(SUN_VERT_DIST, -SUN_VERT_DIST, 0), Color.Yellow, Vector2.Zero, new Vector3(0, 0, -1)));

			meshSun = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, sunVertices, sunIndices);

			meshUVSphere = DrawHelper3D.MakeUVSphere(device, 1);

			meshesLoaded = true;
		}

		public void LoadWorld(string folderName)
		{
			//ChunkManager.Initialize(this);

			int x = Main.random.Next(sizeInCubes / 2 - 4, sizeInCubes / 2 + 4);
			int z = Main.random.Next(sizeInCubes / 2 - 4, sizeInCubes / 2 + 4);

			CubePosition playerPos = CubePosition.FromWorldSpace(new Vector3(sizeInCubes * Cube.CUBE_SCALE / 2f, sizeInCubes * Cube.CUBE_SCALE, sizeInCubes * Cube.CUBE_SCALE / 2f));
			playerPos.X = x;
			playerPos.Z = z;
			playerPos.Y = sizeInCubes;

			saver = new WorldSaver(ChunkManager, EntityManager, Main.SessionInformation);
			saver2 = new WorldSaver(ChunkManager2, null, Main.SessionInformation);

			if (!saver.DoesSaveExist(folderName))
			{
				ChunkManager.GenerateWorld(this);
				
				saver.Save(folderName);

				//chunkLoadManager = new ChunkLoadManager(saver, chunkManager, DrawDistanceHoriz, DrawDistanceVert, DrawRadius + 1);

				player = new Player();
				EntityManager.Add(player);
				player.FirstCreated();

				player.Position = ChunkManager.GetPlayerSpawnPos(this);
				playerStartPos = player.Position;
			}
			else
			{
				WorldSaver.LoadError error = saver.Load(this, folderName);
				//error = saver2.Load(this, "flat01");

				if (error == WorldSaver.LoadError.InvalidVersion)
					Console.WriteLine("Save file could not be loaded. The save file is too low of a version.");

				if (EntityManager.GetAll<Player>().Count > 0)
					player = EntityManager.GetAll<Player>().First() as Player;
				else
				{
					player = new Player();
					EntityManager.Add(player);
					player.FirstCreated();
				}

				playerStartPos = GetFirstSolidDown(playerPos.InWorldSpace(null)).InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE * 3, 0);
			}

			Main.FogManager.Set(1300f, 1700f, Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_day"), Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_night"), 0);

			ChunkLoadManager = new ChunkLoadManager(saver, ChunkManager, 6, 6, 8);
			ChunkLoadManager2 = new ChunkLoadManager(saver2, ChunkManager2, 6, 6, 8);

			LoadedFolderName = folderName;
			Main.SessionInformation.LastLoadedSave = LoadedFolderName;
		}

		public void UnfixedUpdate()
		{
		}

		private float alive;

		public void Update(double deltaTime)
		{
			/*if (Main.inputManager.JustPressed(Keys.K))
            {
				ChunkLoadManager.UnloadAll();
				var tmpLM = ChunkLoadManager;
				ChunkLoadManager = ChunkLoadManager2;
				ChunkLoadManager2 = tmpLM;

				var tmpCM = ChunkManager;
				ChunkManager = ChunkManager2;
				ChunkManager2 = tmpCM;
            }*/

			ChunkLoadManager.UpdateLoadTarget(player.Position);

			alive += (float)deltaTime;

			if (Main.inputManager.JustPressed(Keys.Escape))
			{
				Main.Exit = true;
			}

			if (Main.inputManager.JustPressed(Keys.T))
            {
				//TODO open pause GUI. This maybe should be done in Main.cs instead?
				saver.Save(LoadedFolderName);
			}

			ChunkManager.ProcessChunkQueue(this, 0);
			ChunkLoadManager.Update(deltaTime);

			ProjectileManager.Update(this, deltaTime);
			EntityManager.Update(deltaTime);

			foreach (var mined in miningCubes)
			{
				MinedCube mc = mined.Value;

				Cube cube = mc.chunk.GetData().GetCube(mc.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

				mc.timer -= (float)deltaTime;
				if (mc.timer <= 0)
				{
					mc.progress--;
					mc.timer = 2;
				}

				if (mc.progress <= 0 || cube == Main.Registry.CubeRegistry.Air)
					miningRemove.Add(mc.position);
				else miningUpdate.Add(mc);
			}

			foreach (var pos in miningRemove)
			{
				miningCubes.Remove(pos);
			}

			foreach (var mc in miningUpdate)
			{
				miningCubes[mc.position] = mc;
			}

			miningRemove.Clear();
			miningUpdate.Clear();

			Spawners.ForEach(x => x.Update(deltaTime, this));

			ChunkPosition camPos = ChunkPosition.WorldSpaceChunk(Main.camera.Position);

			if (chunkDrawPositionsDirty || camPos != oldChunkPosition || (oldCameraRotation - Main.camera.Rotation).Length() > MathHelper.ToRadians(1))
			{
				CulledChunkDrawPositions.Clear();

				for (int x = Math.Max(0, camPos.X - DrawDistanceHoriz); x <= Math.Min(sizeInChunks, camPos.X + DrawDistanceHoriz); x++)
				{
					for (int y = Math.Max(0, camPos.Y - DrawDistanceVert); y <= Math.Min(sizeInChunks, camPos.Y + DrawDistanceVert); y++)
					{
						for (int z = Math.Max(0, camPos.Z - DrawDistanceHoriz); z <= Math.Min(sizeInChunks, camPos.Z + DrawDistanceHoriz); z++)
						{
							ChunkPosition chunkPos = new ChunkPosition(x, y, z);

							int length = (int)(new Vector3(chunkPos.X, chunkPos.Y, chunkPos.Z) - new Vector3(camPos.X, camPos.Y, camPos.Z)).Length();

							if (ChunkManager.IsInWorldBounds(chunkPos) && length < DrawRadius && 
								Main.camera.FrustumIntersects(new Rectangle3D(chunkPos.InWorldSpace(), new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE))))
							{
								CulledChunkDrawPositions.Add(chunkPos);
							}
						}
					}
				}

				chunkDrawPositionsDirty = false;
			}

			oldCameraRotation = Main.camera.Rotation;
			oldChunkPosition = camPos;

            //if (!IsNight())
            {
                float angle = 360 * ((alive % DAY_CYCLE_TIME) / DAY_CYCLE_TIME);
				directionalLight.UpdateCameras(Vector3.Transform(new Vector3(0, 0, SUN_LIGHT_DISTANCE),
					Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
					Matrix.CreateRotationY(MathHelper.ToRadians(SUN_ANGLE))), Color.White * (1 - GetTimeOfDay()));
				Main.CubeLitEffect.Parameters["AmbientStrength"].SetValue(1 - GetTimeOfDay(dawnEndOffsetScale: 1.25f));
				Main.Renderer.EffectGBuffer.Parameters["AmbientStrength"].SetValue(1 - GetTimeOfDay(dawnEndOffsetScale: 1.25f));
			}

			if (Main.inputManager.IsHeld(Keys.F1))
			{
				if (Main.inputManager.JustPressed(Keys.OemOpenBrackets))
					currentCascadeDebug = currentCascadeDebug - 1 < 0 ? directionalLight.cameras.Length - 1 : currentCascadeDebug - 1;
				if (Main.inputManager.JustPressed(Keys.OemCloseBrackets))
					currentCascadeDebug = (currentCascadeDebug + 1) % directionalLight.cameras.Length;

				Main.debugCamera.Position = directionalLight.cameras[currentCascadeDebug].Position;
				Main.debugCamera.Rotation = directionalLight.cameras[currentCascadeDebug].Rotation;
				//directionalLight.camera.Position = Main.camera.Position;
				//directionalLight.camera.Rotation = Main.camera.Rotation;
			}
		}

		//Gets a list of all chunks that should be rendered by the main camera.
		public List<ChunkPosition> GetChunkDrawPositions()
        {
			return CulledChunkDrawPositions;
        }

		public static int NumChunksDrawn;
		public static double ChunkDrawTime;

		public void Draw(GraphicsDevice device, Effect effect)
		{
			NumChunksDrawn = 0;
			ChunkDrawTime = 0;

			Stopwatch drawTime = Stopwatch.StartNew();

			directionalLight.DrawShadowmap(device, this);
			directionalLight.Bind(Main.CubeLitEffect);
			directionalLight.Bind(Main.Renderer.EffectLightAccumCSM);

			LightManager.Draw(device);
			//LightManager.DrawShadowmap(device, this);

			//Main.CubeLitEffect.Parameters["TexturesLightDepth"].SetValue(directionalLight.GetShadowmapBuffers());

			device.SetRenderTarget(Main.WorldTarget);
			//device.Clear(ClearOptions.Target, SkyColor, 1, 0);
			device.Clear(SkyColor);

			device.DepthStencilState = Main.genericDSS;
			device.RasterizerState = Main.genericRS;
			device.SamplerStates[2] = Main.clampSS;

			bool drawSkybox = true;
			if (Main.inputManager.IsHeld(Keys.F1))
			{
				Main.WVP.SetProjection(directionalLight.cameras[currentCascadeDebug].GetProjectionMatrix());
				Main.WVP.SetView(directionalLight.cameras[currentCascadeDebug].GetViewMatrix());
				directionalLight.SetPipelineState(device);

				drawSkybox = false;
			}

			float dist = DrawDistanceHoriz * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE - (16 * Cube.CUBE_SCALE);
			Vector3 camChunkPosWS = Main.camera.Position;
			if (camChunkPosWS.Y < Cube.CUBE_SCALE * 100)
				dist = MathHelper.Lerp(64 * Cube.CUBE_SCALE, dist, camChunkPosWS.Y / (Cube.CUBE_SCALE * 100));
			else if (camChunkPosWS.Y < -200f)
				dist = 64 * Cube.CUBE_SCALE;

			camChunkPosWS.X -= DrawDistanceHoriz * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;
			camChunkPosWS.Y -= dist;
			camChunkPosWS.Z -= DrawDistanceHoriz * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;

			if (!player.InWater)
			{
				Main.FogManager.Set(Math.Max(0, dist - 20 * Cube.CUBE_SCALE), dist, Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_day"), Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_night"), GetTimeOfDay());
			}
			else
				Main.FogManager.Set(1, 800, Main.assetsManager.GetAsset<Texture2D>("heightmap_underwater"), Main.assetsManager.GetAsset<Texture2D>("heightmap_underwater"), 0);
			//Main.CubeLitEffect.Parameters["AmbientStrength"].SetValue(1 - GetTimeOfDay());

			foreach (ChunkPosition pos in CulledChunkDrawPositions)
			{
				ChunkMesh mesh = ChunkManager.GetMesh(pos);
				Matrix transform = ChunkManager.GetTransform(pos);

				if (mesh != null)
				{
					Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("cubes_textures"),
						DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
						transform, null));

					NumChunksDrawn++;
				}

				if (Main.Debug && Main.DebugChunks)
				{
					ChunkManager.GetChunk(pos).DrawDebug(device);

					device.RasterizerState = Main.genericRS;
					device.DepthStencilState = Main.genericDSS;
				}
			}

			if (drawSkybox)
			{
				float angle = 360 * ((alive % DAY_CYCLE_TIME) / DAY_CYCLE_TIME);

				meshMaxDrawDistBottom.Draw(device, Main.CubeUnlitEffect, camChunkPosWS, Vector3.Zero, Vector3.One);

				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_day"),
					DrawHelper.BlackPixel, DrawHelper.WhitePixel, meshMaxDrawDistBottom.VBO, meshMaxDrawDistBottom.IBO,
					Matrix.CreateTranslation(camChunkPosWS), null));

				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(DrawHelper.WhitePixel,
					DrawHelper.BlackPixel, DrawHelper.WhitePixel, meshSun.VBO, meshSun.IBO,
					Matrix.CreateTranslation(new Vector3(0, 0, SUN_DISTANCE)) *
					Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
					//Matrix.CreateRotationY(MathHelper.ToRadians(SUN_ANGLE)) *
					Matrix.CreateTranslation(player.Position), null));

				Main.CubeLitEffect.Parameters["TintColor"].SetValue(Color.White.ToVector3());
				Main.FogManager.Disable();

				meshSun.Draw(device, Main.CubeLitEffect,
					Matrix.CreateTranslation(new Vector3(0, 0, SUN_DISTANCE)) *
					Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
					//Matrix.CreateRotationY(MathHelper.ToRadians(SUN_ANGLE)) *
					Matrix.CreateTranslation(player.Position));

				Main.FogManager.Enable();
			}

			foreach (var mined in miningCubes)
			{
				Cube cube = mined.Value.chunk.GetData().GetCube(mined.Value.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

				if (cube != Main.Registry.CubeRegistry.Air)
				{
					float percent = (float)mined.Value.progress / (float)cube.MineProgressRequirement;

					float stepped = ((int)(percent * 8f)) / 8f;

					RectangleF sourceRect = new RectangleF(128f * stepped, 0, 16, 16);

					effect.Parameters["TexCoordOffset"].SetValue(new Vector2(stepped, 0));
					meshMiningCube.Draw(device, effect,
						Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
						Matrix.CreateScale(1.125f) *
						Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
						Matrix.CreateTranslation(mined.Value.position.InWorldSpace(mined.Value.chunk)));
					effect.Parameters["TexCoordOffset"].SetValue(Vector2.Zero);
				}
			}

			LightManager.UpdateDatas(Main.CubeLitEffect);
			//LightManager.UpdateDatas(Main.Renderer.EffectDeferred);
			LightManager.UpdateDatas(Main.Renderer.EffectLightAccumPointLight);

			ProjectileManager.Draw(device, effect);
			EntityManager.Draw(device, Main.CubeLitEffect);

			drawTime.Stop();
			ChunkDrawTime = drawTime.Elapsed.TotalSeconds;

			//player.Draw(device);
			player.DrawDebug(device);
		}

		public void DrawShadowmap(GraphicsDevice device, Effect effect)
        {
			//Draw a mesh that should obscure our shadowmap once the sun falls below the horizon.
			//This point is always at sea level.
			//We could use any mesh for this, but the sun mesh is convenient for the moment. We might have to change this later.
			//This mesh only needs to be the width/height of the directional light so there's no bleeding.
			/*meshSun.Draw(device, Main.CubeEffect,
				Matrix.CreateRotationY(MathHelper.ToRadians(180)) *
				Matrix.CreateScale((1f / 80f) * directionalLight.width, (1f / 80f) * directionalLight.height, 1) *
				Matrix.CreateTranslation(new Vector3(0, -directionalLight.height, SUN_LIGHT_DISTANCE + 50)) *
				Matrix.CreateTranslation(player.Position.X, ChunkGeneratorIsland.SEA_LEVEL * Cube.CUBE_SCALE, player.Position.Z));

			meshSun.Draw(device, Main.CubeEffect,
				Matrix.CreateScale((1f / 80f) * directionalLight.width, (1f / 80f) * directionalLight.height, 1) *
				Matrix.CreateTranslation(new Vector3(0, -directionalLight.height, -(SUN_LIGHT_DISTANCE + 50))) *
				Matrix.CreateTranslation(player.Position.X, ChunkGeneratorIsland.SEA_LEVEL * Cube.CUBE_SCALE, player.Position.Z));*/
		}

		public void DrawUI(SpriteBatch batch)
		{
			player.DrawUI(batch);
		}

		public void DrawWireframeCube(GraphicsDevice device, Vector3 position, Color? color = null)
		{
			device.RasterizerState = Main.wireframeRS;

			/*if (color.HasValue)
			{
				Main.BasicEffect.DiffuseColor = color.Value.ToVector3();
			}*/
			meshWireframeCube.DrawDebugVertexPositionColor(device, Main.VertexPositionColorDebugEffect, color.GetValueOrDefault(Color.White), Matrix.CreateTranslation(position));
			/*if (color.HasValue)
			{
				Main.BasicEffect.DiffuseColor = Color.White.ToVector3();
			}*/
		}

		public void DrawWireframeUnscaled(GraphicsDevice device, Vector3 position, Vector3 scale, Color? color = null)
		{
			/*if (color.HasValue)
			{
				Main.BasicEffect.DiffuseColor = color.Value.ToVector3();
			}*/
			meshWireframeUnscaled.DrawDebugVertexPositionColor(device, Main.VertexPositionColorDebugEffect, color.GetValueOrDefault(Color.White), Matrix.CreateTranslation(position) * Matrix.CreateScale(scale));
			//meshWireframeUnscaled.Draw(device, Main.VertexPositionColorDebugEffect, Matrix.CreateScale(scale) * Matrix.CreateTranslation(position));
			/*if (color.HasValue)
			{
				Main.BasicEffect.DiffuseColor = Color.White.ToVector3();
			}*/
		}

		public void DrawWireframeUnscaled(GraphicsDevice device, Rectangle3D bounds, Color? color = null)
		{
			DrawWireframeUnscaled(device, bounds.Position, bounds.Size, color);
		}

		public void DrawWireframe(GraphicsDevice device, Matrix transform, Color? color = null)
        {
			meshWireframeUnscaled.DrawDebugVertexPositionColor(device, Main.VertexPositionColorDebugEffect, color.GetValueOrDefault(Color.White), transform);
		}

		public ChunkManager GetChunkManager()
		{
			return ChunkManager;
		}

		public void OnCubeUpdate(ChunkData updatingParent, CubePosition updating, int updatedId)
		{
			foreach (Entity entity in EntityManager.GetEntities())
			{
				//if (updatingParent.IsInChunkBounds(entity.Position))
					entity.OnCubeUpdated(updatingParent, updating, updatedId);
			}
		}

		public void SetTimeOfDay(float time)
		{
			alive = time;
		}

		public void AddTime(float time)
        {
			alive += time;
        }

		private float GetTimeOfDay(float dawnStartOffsetScale = 1f, float dawnEndOffsetScale = 1f, float duskStartOffsetScale = 1, float duskEndOffsetScale = 1)
		{
			//values here are in % of day cycle time;
			//dawn starts at the last 8% of the total cycle
			const float DAWN_START = 0.92f;
			//dawn ends after 16% of the total cycle (8% of the day cycle)
			const float DAWN_END = 0.16f;

			//Dusk starts at the last 8% of the day cycle.
			const float DUSK_START = 0.42f;
			//dusk ends after 16% of the night cycle.
			const float DUSK_END = 0.66f;

			float dawnStart = 1 - ((1 - DAWN_START) * dawnStartOffsetScale);
			float dawnEnd = DAWN_END * dawnEndOffsetScale;
			float duskStart = 0.5f - ((1 - DUSK_START - 0.5f) * duskStartOffsetScale);
			float duskEnd = ((DUSK_END - 0.5f) * duskEndOffsetScale) + 0.5f;

			float timeOfDayPercent = (alive % DAY_CYCLE_TIME) / DAY_CYCLE_TIME;

			//Night time
			if (timeOfDayPercent > duskEnd && timeOfDayPercent <= dawnStart)
				return 1;
            else if (timeOfDayPercent > duskStart && timeOfDayPercent <= duskEnd)
					return (timeOfDayPercent - duskStart) / (duskEnd - duskStart);
			else if ((timeOfDayPercent > dawnStart && timeOfDayPercent <= 1) || (timeOfDayPercent >= 0 && timeOfDayPercent <= dawnEnd))
            {
				float percent = 0;
				if (timeOfDayPercent > dawnStart)
					percent = (timeOfDayPercent - dawnStart) / ((timeOfDayPercent + dawnEnd) - dawnStart);
				else if (timeOfDayPercent <= dawnEnd)
					percent = (timeOfDayPercent + (1 - dawnStart)) / dawnEnd;

				percent = MathHelper.Clamp(percent, 0, 1);

				return 1 - percent;
			}

			return 0;
			
			//const float endOfDay = DAY_CYCLE_TIME;
			/*float timeOfDay = alive % endOfDay;

			float startOffset = endOfDay * 0.08f;
			float dawnStart = endOfDay - startOffset;
			float dawnEnd = endOfDay * 0.16f;

			float duskStart = (endOfDay / 2) - (endOfDay * 0.08f);
			float duskEnd = (endOfDay / 2) + (endOfDay * 0.16f);

			if (timeOfDay > duskEnd && timeOfDay <= dawnStart)
				return 1;
			else if (timeOfDay > duskStart && timeOfDay <= duskEnd)
			{
				return (timeOfDay - duskStart) / (duskEnd - duskStart);
			}
			else if ((timeOfDay > dawnStart && timeOfDay <= endOfDay) || (timeOfDay >= 0 && timeOfDay <= dawnEnd))
			{
				float percent = 0;
				if (timeOfDay > dawnStart)
					percent = (timeOfDay - dawnStart) / ((timeOfDay + dawnEnd) - dawnStart);
				else if (timeOfDay <= dawnEnd)
					percent = (timeOfDay - (-startOffset)) / dawnEnd;

				percent = MathHelper.Clamp(percent, 0, 1);

				return 1 - percent;
			}

			return 0;*/
		}

		public bool IsNight()
        {
			return (alive % DAY_CYCLE_TIME) > DAY_CYCLE_TIME / 2f;
        }

		public float GetTimeOfNight()
        {
			float timeOfDay = alive % DAY_CYCLE_TIME;

			if (!IsNight())
				return 0;
            else
            {
				float nightTime = timeOfDay - (DAY_CYCLE_TIME / 2f);

				return nightTime / (DAY_CYCLE_TIME / 2f);
            }
        }

		public void MineCube(CubePosition position, bool instant = false)
		{
			Chunk chunk = ChunkManager.GetChunk(position);

			MinedCube mined = new MinedCube()
			{
				position = position,
				chunk = chunk,
				progress = 1,
				timer = 2
			};

			Cube cube = Main.Registry.CubeRegistry.Get(ChunkManager.GetRaw(position));

			if (cube != null)
			{
				if (instant)
				{
					mined.chunk.GetData().SetCube(position, 0);

					List<ItemInstance> items = new List<ItemInstance>();
					cube.GetDrops(items);

					foreach (ItemInstance item in items)
					{
						EntityItem ent = new EntityItem(position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2f), item);
						ent.Velocity = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2), Cube.CUBE_SCALE, Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2));
						EntityManager.Add(ent);
					}

					return;
				}

				if (miningCubes.ContainsKey(position))
				{
					mined.progress = miningCubes[position].progress + 1;
					if (mined.progress >= cube.MineProgressRequirement)
					{
						miningCubes.Remove(position);
						mined.chunk.GetData().SetCube(position, 0);

						List<ItemInstance> items = new List<ItemInstance>();
						cube.GetDrops(items);

						foreach (ItemInstance item in items)
						{
							EntityItem ent = new EntityItem(position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2f), item);
							ent.Velocity = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2), Cube.CUBE_SCALE, Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2));
							EntityManager.Add(ent);
						}
					}
					else miningCubes[position] = mined;
				}
				else
				{
					if (mined.progress < cube.MineProgressRequirement)
						miningCubes.Add(position, mined);
					else
					{
						mined.chunk.GetData().SetCube(position, 0);

						List<ItemInstance> items = new List<ItemInstance>();
						cube.GetDrops(items);

						foreach (ItemInstance item in items)
						{
							EntityItem ent = new EntityItem(position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2f), item);
							ent.Velocity = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2), Cube.CUBE_SCALE, Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2));
							EntityManager.Add(ent);
						}
					} 
				}
			}
		}

		public CubePosition GetFirstSolidDown(Vector3 start)
		{
			CubePosition startPos = CubePosition.FromWorldSpace(start);

			for (int y = 0; y < sizeInCubes; y++)
			{
				CubePosition pos = new CubePosition(startPos.X, startPos.Y - y, startPos.Z);
				if (ChunkManager.IsInWorldBounds(pos) && ChunkManager.GetRaw(pos) != 0)
					return pos;
			}

			return new CubePosition(-1, -1, -1);
		}

		public List<CubePosition> GetAdjacentsInWorld(CubePosition position)
		{
			CubePosition down = new CubePosition(position.X, position.Y - 1, position.Z);
			CubePosition up = new CubePosition(position.X, position.Y + 1, position.Z);
			CubePosition left = new CubePosition(position.X - 1, position.Y, position.Z);
			CubePosition right = new CubePosition(position.X + 1, position.Y, position.Z);
			CubePosition front = new CubePosition(position.X, position.Y, position.Z - 1);
			CubePosition back = new CubePosition(position.X, position.Y, position.Z + 1);

			List<CubePosition> positions = new List<CubePosition>();

			if (ChunkManager.IsInWorldBounds(down))
				positions.Add(down);
			if (ChunkManager.IsInWorldBounds(up))
				positions.Add(up);
			if (ChunkManager.IsInWorldBounds(left))
				positions.Add(left);
			if (ChunkManager.IsInWorldBounds(right))
				positions.Add(right);
			if (ChunkManager.IsInWorldBounds(front))
				positions.Add(front);
			if (ChunkManager.IsInWorldBounds(back))
				positions.Add(back);

			return positions;
		}

		public MeshHelper.CubeFace GetClearSides(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
				position = position.InCubeSpace(ChunkManager.GetChunk(position));

			if (ChunkManager.GetRaw(position) == 0)
				return MeshHelper.CubeFace.ALL;

			MeshHelper.CubeFace faces = MeshHelper.CubeFace.NONE;

			if (position.X == 0 || ChunkManager.GetCubeInstance(position.X - 1, position.Y, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.LEFT;
			if (position.X == sizeInCubes - 1 || ChunkManager.GetCubeInstance(position.X + 1, position.Y, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.RIGHT;

			if (position.Y == 0 || ChunkManager.GetCubeInstance(position.X, position.Y - 1, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.DOWN;
			if (position.Y == sizeInCubes - 1 || ChunkManager.GetCubeInstance(position.X, position.Y + 1, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.UP;

			if (position.Z == 0 || ChunkManager.GetCubeInstance(position.X, position.Y, position.Z - 1).cubeId == 0)
				faces |= MeshHelper.CubeFace.FRONT;
			if (position.Z == sizeInCubes - 1 || ChunkManager.GetCubeInstance(position.X, position.Y, position.Z + 1).cubeId == 0)
				faces |= MeshHelper.CubeFace.BACK;

			return faces;
		}

		private CubePosition[] positions;
		// Marks the cubes around a position dirty.
		public void MarkCubesDirty(CubePosition position)
		{
			if (positions == null)
				positions = new CubePosition[6];

			positions[0] = new CubePosition(position.X - 1, position.Y, position.Z);//left 
			positions[1] = new CubePosition(position.X + 1, position.Y, position.Z);//right

			positions[2] = new CubePosition(position.X, position.Y - 1, position.Z);//down
			positions[3] = new CubePosition(position.X, position.Y + 1, position.Z);//up

			positions[4] = new CubePosition(position.X, position.Y, position.Z - 1);//front
			positions[5] = new CubePosition(position.X, position.Y, position.Z + 1);//back

			for (int i = 0; i < 6; i++)
			{
				//avoid a copy with ref...
				ref CubePosition pos = ref positions[i];

				Chunk chunk = ChunkManager.GetChunk(pos);

				chunk.GetData().MarkDirty(pos);
			}
		}

		public struct RaycastResult 
		{
			public Vector3 start;
			public Vector3 end;
			public Vector3 hit; //== end if hasHit == false
			public Vector3 normal;
			public bool hasHit;
		}

		public RaycastResult Raycast(Vector3 start, Vector3 end, Func<Vector3, bool> callback)
		{
			RaycastResult result = new RaycastResult();

			const float ONE_CUBE = Cube.CUBE_SCALE;

			result.start = start;
			result.end = end;

			float x1 = start.X / ONE_CUBE;
			float y1 = start.Y / ONE_CUBE;
			float z1 = start.Z / ONE_CUBE;
			float x2 = end.X / ONE_CUBE;
			float y2 = end.Y / ONE_CUBE;
			float z2 = end.Z / ONE_CUBE;

			int i = (int)x1;
			int j = (int)y1;
			int k = (int)z1;

			int iend = (int)x2;
			int jend = (int)y2;
			int kend = (int)z2;

			int di = ((x1 < x2) ? 1 : ((x1 > x2) ? -1 : 0));
			int dj = ((y1 < y2) ? 1 : ((y1 > y2) ? -1 : 0));
			int dk = ((z1 < z2) ? 1 : ((z1 > z2) ? -1 : 0));

			float deltatx = 1.0f / Math.Abs(x2 - x1);
			float deltaty = 1.0f / Math.Abs(y2 - y1);
			float deltatz = 1.0f / Math.Abs(z2 - z1);

			float minx = (int)x1, maxx = minx + 1;
			float tx = ((x1 > x2) ? (x1 - minx) : (maxx - x1)) * deltatx;
			float miny = (int)y1, maxy = miny + 1;
			float ty = ((y1 > y2) ? (y1 - miny) : (maxy - y1)) * deltaty;
			float minz = (int)z1, maxz = minz + 1;
			float tz = ((z1 > z2) ? (z1 - minz) : (maxz - z1)) * deltatz;

			Vector3 hitPos = new Vector3(x1 * ONE_CUBE, y1 * ONE_CUBE, z1 * ONE_CUBE);

			while (true)
			{
				if (callback(hitPos))
				{
					result.hasHit = true;
					result.hit = hitPos;
					return result;
				}

				if (tx <= ty && tx <= tz)
				{
					if (i == iend)
					{
						result.hit = result.end;
						break;
					}
					tx += deltatx;
					i += di;

					if (di == 1) hitPos.X += ONE_CUBE;
					if (di == -1) hitPos.X -= ONE_CUBE;

					result.normal = new Vector3(-di, 0, 0);
				}
				else if (ty <= tz)
				{
					if (j == jend)
					{
						result.hit = result.end;
						break;
					}
					ty += deltaty;
					j += dj;

					if (dj == 1) hitPos.Y += ONE_CUBE;
					if (dj == -1) hitPos.Y -= ONE_CUBE;

					result.normal = new Vector3(0, -dj, 0);
				}
				else
				{
					if (k == kend)
					{
						result.hit = result.end;
						break;
					}
					tz += deltatz;
					k += dk;

					if (dk == 1) hitPos.Z += ONE_CUBE;
					if (dk == -1) hitPos.Z -= ONE_CUBE;

					result.normal = new Vector3(0, 0, -dk);
				}
			}

			return result;
		}

		public RaycastResult RaycastVector(Vector3 start, Vector3 direction, float distance, Func<Vector3, bool> callback)
		{
			return Raycast(start, start + Vector3.Normalize(direction) * distance, callback);
		}
	}
}
