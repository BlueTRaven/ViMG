using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
//using SimplexNoise;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG
{
	public class World
	{
		public const float GRAVITY = -9.8f;
		public const float DAY_CYCLE_TIME = 60f * 10f;

		public readonly int sizeInChunks;
		public readonly int sizeInCubes;

		private readonly ChunkManager chunkManager;

		private SimpleMesh<VertexPositionColor, int> meshWireframeCube;
		private SimpleMesh<VertexPositionColor, int> meshWireframeUnscaled;
		private SimpleMesh<VertexPositionColorTextureNormal, int> meshMiningCube;
		private SimpleMesh<VertexPositionColorTextureNormal, int> meshSun;

		private SimpleMesh<VertexPositionColorTextureNormal, int> meshMaxDrawDistBottom;

		public Player player;
		private Vector3 playerStartPos;

		public int DrawDistanceHoriz = 6;   //radius in chunks that we should be able to see
		public int DrawDistanceVert = 6;
		public int DrawRadius = 6;

		public HitboxManager HitboxManager = new HitboxManager(32);
		public ProjectileManager ProjectileManager;
		public EntityManager EntityManager;

		public Color SkyColor = new Color(94, 107, 154);

		private List<ChunkPosition> chunkDrawPositions = new List<ChunkPosition>();
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

		public World(GraphicsDevice device, int worldSize)
		{
			this.sizeInCubes = worldSize;

			sizeInChunks = (int)((float)worldSize / Chunk.CHUNK_SIZE);

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

			vertices.Add(new VertexPositionColorTextureNormal(r_t_n, Color.Black, Vector2.Zero, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(l_t_n, Color.Black, Vector2.Zero, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_n, Color.Black, Vector2.Zero, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_n, Color.Black, Vector2.Zero, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(r_t_f, Color.Black, Vector2.Zero, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_t_n, Color.Black, Vector2.Zero, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_n, Color.Black, Vector2.Zero, new Vector3(-1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_f, Color.Black, Vector2.Zero, new Vector3(-1, 0, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(l_t_f, Color.Black, Vector2.Zero, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(r_t_f, Color.Black, Vector2.Zero, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_f, Color.Black, Vector2.Zero, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_f, Color.Black, Vector2.Zero, new Vector3(0, 0, -1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(l_t_n, Color.Black, Vector2.Zero, new Vector3(1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(l_t_f, Color.Black, Vector2.Zero, new Vector3(1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_f, Color.Black, Vector2.Zero, new Vector3(1, 0, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_n, Color.Black, Vector2.Zero, new Vector3(1, 0, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(l_b_f, Color.Black, Vector2.Zero, new Vector3(0, -1, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_f, Color.Black, Vector2.Zero, new Vector3(0, -1, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(r_b_n, Color.Black, Vector2.Zero, new Vector3(0, -1, 0)));
			vertices.Add(new VertexPositionColorTextureNormal(l_b_n, Color.Black, Vector2.Zero, new Vector3(0, -1, 0)));

			meshMaxDrawDistBottom = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, DrawHelper.WhitePixel);

			List<VertexPositionColorTextureNormal> sunVertices = new List<VertexPositionColorTextureNormal>();
			List<int> sunIndices = new List<int>();

			sunIndices.Add(0);
			sunIndices.Add(1);
			sunIndices.Add(3);
			sunIndices.Add(1);
			sunIndices.Add(2);
			sunIndices.Add(3);

			sunVertices.Add(new VertexPositionColorTextureNormal(new Vector3(-80, -80, 0), Color.Yellow, Vector2.Zero, new Vector3(0, 0, -1)));
			sunVertices.Add(new VertexPositionColorTextureNormal(new Vector3(-80, 80, 0), Color.Yellow, Vector2.Zero, new Vector3(0, 0, -1)));
			sunVertices.Add(new VertexPositionColorTextureNormal(new Vector3(80, 80, 0), Color.Yellow, Vector2.Zero, new Vector3(0, 0, -1)));
			sunVertices.Add(new VertexPositionColorTextureNormal(new Vector3(80, -80, 0), Color.Yellow, Vector2.Zero, new Vector3(0, 0, -1)));

			meshSun = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, sunVertices, sunIndices);

			chunkManager = new ChunkManager(device, sizeInChunks, sizeInCubes, this);

			ProjectileManager = new ProjectileManager(device);
			EntityManager = new EntityManager(this);

			Main.CubeEffect.Parameters["WorldSize"].SetValue(new Vector3(worldSize));
			Main.CubeEffect.Parameters["CubeSize"].SetValue(new Vector3(Cube.CUBE_SCALE));
		}

		public void Initialize()
		{
			chunkManager.Initialize(this);

			player = new Player();
			EntityManager.Add(player);

			int x = Main.random.Next(sizeInCubes / 2 - 4, sizeInCubes / 2 + 4);
			int z = Main.random.Next(sizeInCubes / 2 - 4, sizeInCubes / 2 + 4);

			CubePosition playerPos = CubePosition.FromWorldSpace(player.Position);
			playerPos.X = x;
			playerPos.Z = z;
			playerPos.Y = sizeInCubes;

			for (int y = 0; y < sizeInChunks; y++)
			{
				ChunkPosition pos = ChunkPosition.CubeChunk(playerPos);
				pos.Y -= y;

				if (chunkManager.IsInWorldBounds(pos) && !GetChunkManager().IsChunkGenerated(pos))
				{
					GetChunkManager().MarkGenerateDirty(pos);
				}
			}

			// do this sync because we have to wait anyway
			chunkManager.ProcessChunkQueueSync(this);

			bool ok = false;
			player.Position = GetFirstSolidDown(playerPos.InWorldSpace(out ok)).InWorldSpace(out ok) + new Vector3(0, Cube.CUBE_SCALE * 3, 0);
			playerStartPos = player.Position;

			Main.FogManager.Set(1300f, 1700f, Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_day"), Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_night"), 0);
			//Main.FogHandler.Set(750f, 800f, SkyColor);
		}

		public void UnfixedUpdate()
		{
		}

		private float alive;

		public void Update(double deltaTime)
		{
			alive += (float)deltaTime;

			chunkManager.ProcessChunkQueue(this, 0);

			//player.Update(deltaTime);

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

			var slimes = EntityManager.GetAll<Slime>();

			if ((slimes == null || slimes.Count < 32) && Main.random.Next(0, 32) == 0)
			{
				CubePosition pos = GetFirstSolidDown(new Vector3(Main.random.Next(0, sizeInCubes * Cube.CUBE_SCALE), sizeInCubes * Cube.CUBE_SCALE, Main.random.Next(0, sizeInCubes * Cube.CUBE_SCALE)));

				if (chunkManager.IsInWorldBounds(pos))
				{
					EntityManager.Add(new Slime(pos.InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE, 0)));
					//slimes.Add(new Slime(this, pos.InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE, 0)));
				}
			}

			ChunkPosition camPos = ChunkPosition.WorldSpaceChunk(-Main.camera.Position);

			if (chunkDrawPositionsDirty || camPos != oldChunkPosition || (oldCameraRotation - Main.camera.Rotation).Length() > MathHelper.ToRadians(1))
			{
				chunkDrawPositions.Clear();

				for (int x = Math.Max(0, camPos.X - DrawDistanceHoriz); x <= Math.Min(sizeInChunks, camPos.X + DrawDistanceHoriz); x++)
				{
					for (int y = Math.Max(0, camPos.Y - DrawDistanceVert); y <= Math.Min(sizeInChunks, camPos.Y + DrawDistanceVert); y++)
					{
						for (int z = Math.Max(0, camPos.Z - DrawDistanceHoriz); z <= Math.Min(sizeInChunks, camPos.Z + DrawDistanceHoriz); z++)
						{
							ChunkPosition chunkPos = new ChunkPosition(x, y, z);

							int length = (int)(new Vector3(chunkPos.X, chunkPos.Y, chunkPos.Z) - new Vector3(camPos.X, camPos.Y, camPos.Z)).Length();

							if (chunkManager.IsInWorldBounds(chunkPos) && length < DrawRadius && 
								Main.camera.FrustumIntersects(new Rectangle3D(chunkPos.InWorldSpace(), new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE))))
							{
								chunkDrawPositions.Add(chunkPos);
							}
						}
					}
				}

				chunkDrawPositionsDirty = false;
			}

			oldCameraRotation = Main.camera.Rotation;
			oldChunkPosition = camPos;
		}

		public static int NumChunksDrawn;
		public static double ChunkDrawTime;

		public void Draw(GraphicsDevice device, Effect effect)
		{
			NumChunksDrawn = 0;
			ChunkDrawTime = 0;

			Stopwatch drawTime = Stopwatch.StartNew();

			DrawDepth(device);
			Main.CubeEffect.Parameters["TextureLightDepth"].SetValue(Main.DepthTarget);

			device.SetRenderTarget(Main.WorldTarget);
			//device.Clear(ClearOptions.Target, SkyColor, 1, 0);
			device.Clear(SkyColor);

			device.DepthStencilState = Main.genericDSS;
			device.RasterizerState = Main.genericRS;
			device.SamplerStates[2] = Main.clampSS;
			float dist = 1700f;

			Vector3 camChunkPosWS = -Main.camera.Position;
			if (camChunkPosWS.Y < Cube.CUBE_SCALE * 100)
				dist = MathHelper.Lerp(200f, 1700f, camChunkPosWS.Y / (Cube.CUBE_SCALE * 100));
			else if (camChunkPosWS.Y < -200f)
				dist = 200f;

			camChunkPosWS.X -= DrawDistanceHoriz * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;
			camChunkPosWS.Y -= dist;
			camChunkPosWS.Z -= DrawDistanceHoriz * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;
			//if (camChunkPosWS.Y < 0)
			//camChunkPosWS.Y = 0;

			Vector2 center = new Vector2(sizeInCubes * Cube.CUBE_SCALE / 2f, sizeInCubes * Cube.CUBE_SCALE / 2f);
			Vector2 distFromCenter = new Vector2(center.X - (-Main.camera.Position.X), center.Y - (-Main.camera.Position.Z));

			float len = distFromCenter.Length();

			if (len > (sizeInCubes * Cube.CUBE_SCALE / 2f) - 200f)
			{
				float lend = len - ((sizeInCubes * Cube.CUBE_SCALE / 2f) - 200f);
				float percent = 1 - (lend / 100f);
				percent = MathHelper.Clamp(percent, 0, 1);

				dist = MathHelper.Lerp(0, dist, percent);
			}

			if (!player.InWater)
			{
				Main.FogManager.Set(Math.Max(0, dist - 400f), dist, Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_day"), Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_night"), GetTimeOfDay());
			}
			else
				Main.FogManager.Set(1, 800, Main.assetsManager.GetAsset<Texture2D>("heightmap_underwater"), Main.assetsManager.GetAsset<Texture2D>("heightmap_underwater"), 0);
			Main.CubeEffect.Parameters["AmbientStrength"].SetValue(1 - GetTimeOfDay());

			foreach (ChunkPosition pos in chunkDrawPositions)
			{
				ChunkMesh mesh = chunkManager.GetMesh(pos);
				Matrix transform = chunkManager.GetTransform(pos);

				if (mesh != null)
				{
					mesh.Draw(device, effect, transform);
					NumChunksDrawn++;
				}

				/*chunkManager.GetChunk(pos).DrawDebug(device);

				device.RasterizerState = Main.genericRS;
				device.DepthStencilState = Main.genericDSS;*/
			}

			meshMaxDrawDistBottom.Draw(device, effect, camChunkPosWS, Vector3.Zero, Vector3.One);

			Main.CubeEffect.Parameters["TintColor"].SetValue(Color.White.ToVector3());
			Main.CubeEffect.Parameters["EnableFog"].SetValue(0);
			float angle = 360 * ((alive % DAY_CYCLE_TIME) / DAY_CYCLE_TIME);
			meshSun.Draw(device, Main.CubeEffect, 
				Matrix.CreateTranslation(new Vector3(0, 0, -DrawDistanceHoriz * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE + Cube.CUBE_SCALE)) *
				Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
				Matrix.CreateTranslation(player.Position));
			Main.CubeEffect.Parameters["EnableFog"].SetValue(1);

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

			ProjectileManager.Draw(device, effect);
			EntityManager.Draw(device);

			drawTime.Stop();
			ChunkDrawTime = drawTime.Elapsed.TotalSeconds;

			//player.Draw(device);
			player.DrawDebug(device);

			//device.RasterizerState = Main.wireframeRS;
			//mesh.Draw(device, Main.BasicEffect, Vector3.Zero, Vector3.Zero, Vector3.One);
		}

		public void DrawUI(SpriteBatch batch)
		{
			player.DrawUI(batch);
		}

		public void DrawDepth(GraphicsDevice device)
		{
			if (!Main.ENABLE_SHADOWS)
			{
				Main.CubeEffect.Parameters["EnableShadows"].SetValue(0);
				return;
			}
			else Main.CubeEffect.Parameters["EnableShadows"].SetValue(1);

			device.SetRenderTarget(Main.DepthTarget);

			device.DepthStencilState = Main.genericDSS;
			device.RasterizerState = Main.reverseRS;
			device.SamplerStates[2] = Main.shadowBorderClampSS;

			//device.Clear(Color.White);
			device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, device.Viewport.MaxDepth, 0);

			Vector3 lightPos = playerStartPos + new Vector3(0, 100, 0); 
			Matrix lightProj = Matrix.CreateOrthographicOffCenter(-1000, 1000, 1000, -1000, Main.NEAR, Main.FAR);

			//float rotation = 360f * ((alive % 10f) / 10f);
			Matrix camera = Matrix.CreateTranslation(-lightPos) * Matrix.CreateRotationX(MathHelper.ToRadians(90)); //Main.camera.GetViewMatrix();

			//Main.WVP.SetProjection(lightProj);
			//Main.WVP.SetView(camera);

			for (int x = -DrawDistanceHoriz; x <= DrawDistanceHoriz; x++)
			{
				for (int y = -DrawDistanceVert; y <= DrawDistanceVert; y++)
				{
					for (int z = -DrawDistanceHoriz; z < DrawDistanceHoriz; z++)
					{
						ChunkPosition chunkPos = ChunkPosition.WorldSpaceChunk(lightPos);
						chunkPos.X += x;
						chunkPos.Y += y;
						chunkPos.Z += z;
						ChunkMesh mesh = chunkManager.GetMesh(chunkPos);
						Matrix transform = chunkManager.GetTransform(chunkPos);

						if (mesh != null)
						{
							mesh.Draw(device, Main.assetsManager.GetAsset<Effect>("depth"), transform);
						}
					}
				}
			}

			Main.WVP.SetView(Main.camera.GetViewMatrix());
			Main.WVP.SetProjection(Main.camera.GetProjectionMatrix());

			Main.CubeEffect.Parameters["LightViewProjection"].SetValue(lightProj * camera);
			//Main.CubeEffect.Parameters["LightPos"].SetValue(-lightPos);
		}

		public void DrawWireframeCube(GraphicsDevice device, Vector3 position, Color? color = null)
		{
			device.RasterizerState = Main.wireframeRS;

			if (color.HasValue)
			{
				Main.BasicEffect.DiffuseColor = color.Value.ToVector3();
			}
			meshWireframeCube.Draw(device, Main.BasicEffect, position, Vector3.Zero, Vector3.One);
			if (color.HasValue)
			{
				Main.BasicEffect.DiffuseColor = Color.White.ToVector3();
			}
		}

		public void DrawWireframeUnscaled(GraphicsDevice device, Vector3 position, Vector3 scale, Color? color = null)
		{
			if (color.HasValue)
			{
				Main.BasicEffect.DiffuseColor = color.Value.ToVector3();
			}
			meshWireframeUnscaled.Draw(device, Main.BasicEffect, Matrix.CreateScale(scale) * Matrix.CreateTranslation(position));
			if (color.HasValue)
			{
				Main.BasicEffect.DiffuseColor = Color.White.ToVector3();
			}
		}

		public void DrawWireframeUnscaled(GraphicsDevice device, Rectangle3D bounds, Color? color = null)
		{
			DrawWireframeUnscaled(device, bounds.Position, bounds.Size, color);
		}

		public ChunkManager GetChunkManager()
		{
			return chunkManager;
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

		private float GetTimeOfDay()
		{
			const float endOfDay = DAY_CYCLE_TIME;

			float timeOfDay = alive % endOfDay;

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

			return 0;
		}

		public void MineCube(CubePosition position, bool instant = false)
		{
			Chunk chunk = chunkManager.GetChunk(position);

			MinedCube mined = new MinedCube()
			{
				position = position,
				chunk = chunk,
				progress = 1,
				timer = 2
			};

			Cube cube = Main.Registry.CubeRegistry.Get(chunkManager.GetRaw(position));

			if (cube != null)
			{
				if (instant)
				{
					mined.chunk.GetData().SetCube(position, 0);

					List<ItemInstance> items = new List<ItemInstance>();
					cube.GetDrops(items);

					foreach (ItemInstance item in items)
					{
						EntityItem ent = new EntityItem(position.InWorldSpace(null), item);
						ent.Velocity = new Vector3(Main.random.NextFloat(-100, 100), 32, Main.random.NextFloat(-100, 100));
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
							EntityItem ent = new EntityItem(position.InWorldSpace(null), item);
							ent.Velocity = new Vector3(Main.random.NextFloat(-100, 100), 32, Main.random.NextFloat(-100, 100));
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
							EntityItem ent = new EntityItem(position.InWorldSpace(null), item);
							ent.Velocity = new Vector3(Main.random.NextFloat(-100, 100), 32, Main.random.NextFloat(-100, 100));
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
				if (chunkManager.IsInWorldBounds(pos) && chunkManager.GetRaw(pos) != 0)
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

			if (chunkManager.IsInWorldBounds(down))
				positions.Add(down);
			if (chunkManager.IsInWorldBounds(up))
				positions.Add(up);
			if (chunkManager.IsInWorldBounds(left))
				positions.Add(left);
			if (chunkManager.IsInWorldBounds(right))
				positions.Add(right);
			if (chunkManager.IsInWorldBounds(front))
				positions.Add(front);
			if (chunkManager.IsInWorldBounds(back))
				positions.Add(back);

			return positions;
		}

		public MeshHelper.CubeFace GetClearSides(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
				position = position.InCubeSpace(chunkManager.GetChunk(position));

			if (chunkManager.GetRaw(position) == 0)
				return MeshHelper.CubeFace.ALL;

			MeshHelper.CubeFace faces = MeshHelper.CubeFace.NONE;

			if (position.X == 0 || chunkManager.GetCubeInstance(position.X - 1, position.Y, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.LEFT;
			if (position.X == sizeInCubes - 1 || chunkManager.GetCubeInstance(position.X + 1, position.Y, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.RIGHT;

			if (position.Y == 0 || chunkManager.GetCubeInstance(position.X, position.Y - 1, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.DOWN;
			if (position.Y == sizeInCubes - 1 || chunkManager.GetCubeInstance(position.X, position.Y + 1, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.UP;

			if (position.Z == 0 || chunkManager.GetCubeInstance(position.X, position.Y, position.Z - 1).cubeId == 0)
				faces |= MeshHelper.CubeFace.FRONT;
			if (position.Z == sizeInCubes - 1 || chunkManager.GetCubeInstance(position.X, position.Y, position.Z + 1).cubeId == 0)
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

				Chunk chunk = chunkManager.GetChunk(pos);

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

			result.start = start;
			result.end = end;

			float x1 = start.X;
			float y1 = start.Y;
			float z1 = start.Z;
			float x2 = end.X;
			float y2 = end.Y;
			float z2 = end.Z;

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

			float minx = (int)x1, maxx = minx + 1.0f;
			float tx = ((x1 > x2) ? (x1 - minx) : (maxx - x1)) * deltatx;
			float miny = (int)y1, maxy = miny + 1.0f;
			float ty = ((y1 > y2) ? (y1 - miny) : (maxy - y1)) * deltaty;
			float minz = (int)z1, maxz = minz + 1.0f;
			float tz = ((z1 > z2) ? (z1 - minz) : (maxz - z1)) * deltatz;

			Vector3 hitPos = new Vector3(x1, y1, z1);

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

					if (di == 1) hitPos.X++;
					if (di == -1) hitPos.X--;

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

					if (dj == 1) hitPos.Y++;
					if (dj == -1) hitPos.Y--;

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

					if (dk == 1) hitPos.Z++;
					if (dk == -1) hitPos.Z--;

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
