using BepuUtilities.Collections;
using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG
{
    public class DirectionalLight : IDisposable
    {
		public const int RT_SIZE = 512;

		public CameraCSM[] cameras;

		public readonly float width;
		public readonly float height;

		private RenderTarget2D target;
		private RenderTarget2D targetsArr;

		private Matrix[] lightViewProjections;
		private Vector3 lightDirection;
		private Vector4 lightColor;

		private Vector4[] cascadeOffsets;
		private Vector4[] cascadeScales;

		private float[] splitDistances;
		private float[] farPlanes;

		private RasterizerState rs;

		public Texture2D WorldheightMap;

		private int version;
		private int lastUpdatedVersion;

		private FastList<ChunkPosition>[] cameraCachedChunks;

		public DirectionalLight(GraphicsDevice device, Camera mainCamera, float[] splitDistances)
        {
			int num = splitDistances.Length + 1;

			cameraCachedChunks = new FastList<ChunkPosition>[num];
			for (int i = 0; i < num; i++)
				cameraCachedChunks[i] = new FastList<ChunkPosition>();

            this.splitDistances = new float[num];
			for (int i = 0; i < num - 1; i++)
				this.splitDistances[i] = splitDistances[i];
			this.splitDistances[num - 1] = 1.0f;

			farPlanes = new float[num];

			for (int i = 0; i < num; i++)
            {
				if (i < num - 1)
					farPlanes[i] = mainCamera.Far * splitDistances[i];
				else farPlanes[i] = mainCamera.Far;
            }

			cameras = new CameraCSM[num];
			lightViewProjections = new Matrix[num];
			cascadeOffsets = new Vector4[num];
			cascadeScales = new Vector4[num];
			targetsArr = new RenderTarget2D(device, RT_SIZE, RT_SIZE, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 
				1, RenderTargetUsage.PreserveContents, false, num);
			for (int i = 0; i < num; i++)
            {
				if (i == 0)
					cameras[i] = new CameraCSM(mainCamera, mainCamera.Near, farPlanes[i], 0, splitDistances[i]);
				else if (i < num - 1)
					cameras[i] = new CameraCSM(mainCamera, farPlanes[i - 1], farPlanes[i], splitDistances[i - 1], splitDistances[i]);
				else cameras[i] = new CameraCSM(mainCamera, farPlanes[i - 1], mainCamera.Far, splitDistances[i - 1], 1f);
			}

			target = new RenderTarget2D(device, RT_SIZE, RT_SIZE, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);

			//Main.CubeLitEffect.Parameters["CascadePlaneDistances"].SetValue(farPlanes);
			//Main.CubeLitEffect.Parameters["FarPlane"].SetValue(Main.camera.Far);
			(Main.Registry.ItemRegistry.Get("debug_depth_target") as Items.ItemDebugDepthTarget).DepthTarget = target;

			rs = new RasterizerState()
			{
				FillMode = FillMode.Solid,
				CullMode = CullMode.None,
				DepthClipEnable = false,
			};
		}

		public void SetPipelineState(GraphicsDevice device)
        {
			device.DepthStencilState = Main.genericDSS;
			device.RasterizerState = rs;
			device.SamplerStates[3] = Main.shadowBorderClampSS;
			device.SamplerStates[4] = Main.shadowBorderClampSS;
			device.SamplerStates[5] = Main.shadowBorderClampSS;
		}

		public void UpdateCameras(World world, Vector3 direction, Vector4 color, float clampY = -1)
        {
			this.lightDirection = Vector3.Normalize(direction);
			this.lightColor = color;

			ChunkPosition cameraPos = ChunkPosition.WorldSpaceChunk(Main.camera.Position);

			for (int i = 0; i < cameras.Length; i++)
            {
				cameras[i].Update(Vector3.Normalize(direction), clampY - (0.001f * i));

				cameraCachedChunks[i].Clear();

                for (int z = (int)Math.Max(0, cameraPos.Z - world.DrawDistanceHoriz); 
					z <= (int)Math.Min(world.sizeInChunks, cameraPos.Z + world.DrawDistanceHoriz); z++)
				{
					for (int y = (int)Math.Max(0, cameraPos.Y - world.DrawDistanceVert); 
						y <= (int)Math.Min(world.sizeInChunks, cameraPos.Y + world.DrawDistanceVert); y++)
					{
						for (int x = (int)Math.Max(0, cameraPos.X - world.DrawDistanceHoriz); 
							x <= (int)Math.Min(world.sizeInChunks, cameraPos.X + world.DrawDistanceHoriz); x++)
						{
							ChunkPosition chunkPos = new ChunkPosition(x, y, z);

							if (world.ChunkManager.IsInWorldBounds(chunkPos) && world.ChunkLoadManager.IsLoaded(chunkPos))
							{
								/*if (cameras[i].GetFrustum().Contains(new BoundingBox(chunkPos.InWorldSpace(),
									chunkPos.InWorldSpace() + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE))) == ContainmentType.Intersects)*/
									cameraCachedChunks[i].Add(chunkPos);
							}
						}
					}
                }
            }
			
			version++;
        }

		//Try to test for intersection by using the precomputed points in CameraCSM instead of using BoundingFrustum (which appears to be incorrect)
		private void CorrectTestFor(CameraCSM camera)
		{
			Vector3[] corners = camera.GetCorners();


		}

        public void DrawShadowmap(GraphicsDevice device, World world)
		{
			if (lastUpdatedVersion == version)
				return;

			lastUpdatedVersion = version;

			if (!Main.ENABLE_SHADOWS)
			{
				return;
			}

			Matrix globalShadowMatrix = MakeGlobalShadowMatrix(Main.camera, lightDirection);
			Matrix texScaleBias = Matrix.CreateScale(0.5f, -0.5f, 1.0f)
				   * Matrix.CreateTranslation(0.5f, 0.5f, 0.0f);

			SetPipelineState(device);
			//device.SetRenderTarget(target);
			//device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, device.Viewport.MaxDepth, 0);

			//DrawOneCamera(device, camera, world);

			for (int i = 0; i < cameras.Length; i++)
			{
				CameraCSM camera = cameras[i];

				device.SetRenderTarget(targetsArr, i);
				device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, device.Viewport.MaxDepth, 0);

				DrawOneCamera(device, camera, cameraCachedChunks[i], world);

				//lightViewProjections[i] = camera.GetViewMatrix() * camera.GetProjectionMatrix();

				var shadowMatrix = camera.GetViewMatrix() * camera.GetProjectionMatrix();
				shadowMatrix = shadowMatrix * texScaleBias;

				// Store the split distance in terms of view space depth
				var clipDist = Main.camera.Far - Main.camera.Near;

				//shadowCamera: lightViewProjections/CameraCSM
				//camera: Main.camera
				farPlanes[i] = Main.camera.Near + splitDistances[i] * clipDist;

				// Calculate the position of the lower corner of the cascade partition, in the UV space
				// of the first cascade partition
				var invCascadeMat = Matrix.Invert(shadowMatrix);
				var cascadeCorner = Vector4.Transform(Vector3.Zero, invCascadeMat).ToVector3();
				cascadeCorner = Vector4.Transform(cascadeCorner, globalShadowMatrix).ToVector3();

				// Do the same for the upper corner
				var otherCorner = Vector4.Transform(Vector3.One, invCascadeMat).ToVector3();
				otherCorner = Vector4.Transform(otherCorner, globalShadowMatrix).ToVector3();

				// Calculate the scale and offset
				var cascadeScale = Vector3.One / (otherCorner - cascadeCorner);
				cascadeOffsets[i] = new Vector4(-cascadeCorner, 0.0f);
				cascadeScales[i] = new Vector4(cascadeScale, 1.0f);
			}

			//Main.WVP.SetProjection(Main.camera.GetProjectionMatrix());
			//Main.WVP.SetView(Main.camera.GetViewMatrix());
		}

		public void Bind(Effect effect)
		{
			Matrix globalShadowMatrix = MakeGlobalShadowMatrix(Main.camera, lightDirection);

			effect.Parameters["NumCascades"].SetValue(farPlanes.Length);
			effect.Parameters["LightResolution"].SetValue(new Vector2(RT_SIZE));

			effect.Parameters["CascadePlaneDistances"].SetValue(farPlanes);
			effect.Parameters["LightViewProjection"].SetValue(globalShadowMatrix);
			//effect.Parameters["LightPos"].SetValue(camera.Position);
			effect.Parameters["LightDirection"].SetValue(lightDirection);
			//effect.Parameters["LightViewProjections"].SetValue(lightViewProjections);
			effect.Parameters["LightColor"].SetValue(lightColor);
			effect.Parameters["CascadeOffsets"].SetValue(cascadeOffsets);
			effect.Parameters["CascadeScales"].SetValue(cascadeScales);
			//effect.Parameters["LightDirections"].SetValue(lightDirections);

			effect.Parameters["LightDepthTextures"].SetValue(GetShadowmapBuffers());
			effect.Parameters["WorldheightMap"].SetValue(WorldheightMap);
		}

		private Vector3[] corners = new Vector3[8];
		private Matrix MakeGlobalShadowMatrix(Camera camera, Vector3 direction)
		{
			// Get the 8 points of the view frustum in world space
			corners[0] = new Vector3(-1.0f, 1.0f, 0.0f);
			corners[1] = new Vector3(1.0f, 1.0f, 0.0f);
			corners[2] = new Vector3(1.0f, -1.0f, 0.0f);
			corners[3] = new Vector3(-1.0f, -1.0f, 0.0f);
			corners[4] = new Vector3(-1.0f, 1.0f, 1.0f);
			corners[5] = new Vector3(1.0f, 1.0f, 1.0f);
			corners[6] = new Vector3(1.0f, -1.0f, 1.0f);
			corners[7] = new Vector3(-1.0f, -1.0f, 1.0f);

			var invViewProj = Matrix.Invert(camera.GetViewMatrix() * camera.GetProjectionMatrix());
			var frustumCenter = Vector3.Zero;
			for (var i = 0; i < 8; i++)
			{
				corners[i] = Vector4.Transform(corners[i], invViewProj).ToVector3();
				frustumCenter += corners[i];
			}

			frustumCenter /= 8.0f;

			// Pick the up vector to use for the light camera
			var upDir = Vector3.Up;

			// Get position of the shadow camera
			var shadowCameraPos = frustumCenter + direction * -0.5f;

			// Come up with a new orthographic camera for the shadow caster
			Matrix shadowProj = Matrix.CreateOrthographicOffCenter(-0.5f, 0.5f, -0.5f, 0.5f, 0, 1.0f);
			Matrix shadowView = Matrix.CreateLookAt(shadowCameraPos, frustumCenter, upDir);

			var texScaleBias = Matrix.CreateScale(0.5f, -0.5f, 1.0f);
			texScaleBias.Translation = new Vector3(0.5f, 0.5f, 0.0f);
			return (shadowView * shadowProj) * texScaleBias;
		}

		private void DrawOneCamera(GraphicsDevice device, CameraCSM camera, FastList<ChunkPosition> cachedChunkPositions, World world)
        {
			//Main.WVP.SetProjection(camera.GetProjectionMatrix());
			//Main.WVP.SetView(camera.GetViewMatrix());

			Matrix viewProj = camera.GetViewMatrix() * camera.GetProjectionMatrix();
			Effect effectDepth = Main.assetsManager.GetAsset<Effect>("depth");
			effectDepth.Parameters["WorldViewProjection"].SetValue(viewProj);
			//effectDepth.Parameters["World"].SetValue(Matrix.Identity);

			for (int i = 0; i < cachedChunkPositions.Length; i++)
			{
                VerySimpleMesh mesh = world.ChunkManager.RenderMesher.GetMesh(cachedChunkPositions[i], Cube.RenderPass.DepthOnly);
                //Matrix transform = world.ChunkManager2.GetTransform(chunkPos);

				if (mesh.VBOPosition != null && mesh.VBOTexCoord != null)
				{
					device.SetVertexBuffers(new VertexBufferBinding(mesh.VBOPosition, 0), new VertexBufferBinding(mesh.VBOTexCoord, 0));
					device.Indices = mesh.IBO;
                    
					foreach (var pass in effectDepth.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.IBO.IndexCount / 3);
                    }
                }
                //if (mesh.VBO != null)
                //{
                //    device.SetVertexBuffer(mesh.VBO);
                //    device.Indices = mesh.IBO;

                //    foreach (var pass in effectDepth.CurrentTechnique.Passes)
                //    {
                //        pass.Apply();
                //        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.IBO.IndexCount / 3);
                //    }

                //    //mesh.DrawDepth(device, Main.assetsManager.GetAsset<Effect>("depth"), Matrix.Identity, viewProj);
                //}
            }
			/*for (int x = -world.DrawDistanceHoriz; x <= world.DrawDistanceHoriz; x++)
			{
				for (int y = -world.DrawDistanceVert; y <= world.DrawDistanceVert; y++)
				{
					for (int z = -world.DrawDistanceHoriz; z <= world.DrawDistanceHoriz; z++)
					{
						ChunkPosition chunkPos = ChunkPosition.WorldSpaceChunk(world.player.Position);
						chunkPos.X += x;
						chunkPos.Y += y;
						chunkPos.Z += z;

						if (world.ChunkManager.IsInWorldBounds(chunkPos) && world.ChunkLoadManager.IsLoaded(chunkPos))
						{
							(VertexBuffer VBO, IndexBuffer IBO) mesh = world.ChunkManager.GetMesh(chunkPos, Cubes.Cube.RenderPass.DepthOnly);
							//Matrix transform = world.ChunkManager2.GetTransform(chunkPos);

							if (mesh.VBO != null)
							{
								device.SetVertexBuffer(mesh.VBO);
								device.Indices = mesh.IBO;

								foreach (var pass in effectDepth.CurrentTechnique.Passes)
								{
									pass.Apply();
									device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.IBO.IndexCount / 3);
								}

								//mesh.DrawDepth(device, Main.assetsManager.GetAsset<Effect>("depth"), Matrix.Identity, viewProj);
							}
						}
					}
				}
			}*/
		}

		public RenderTarget2D GetShadowmapBuffer()
        {
			return target;
        }

		public RenderTarget2D GetShadowmapBuffers()
        {
			return targetsArr;
        }

        public void Dispose()
        {
			target?.Dispose();
			targetsArr?.Dispose();

			Main.Renderer.DoCSMLight = false;
		}
    }
}
