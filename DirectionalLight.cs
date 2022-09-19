using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public class DirectionalLight
    {
        //public CameraOrthographic camera;
		public CameraCSM camera;
		public CameraCSM[] cameras;

		public readonly float width;
		public readonly float height;

		private RenderTarget2D target;
		private RenderTarget2D targetsArr;

		private Matrix[] lightViewProjections;
		private Vector3 lightDirection;
		private Vector3 lightColor;

		private Vector4[] cascadeOffsets;
		private Vector4[] cascadeScales;

		private float[] splitDistances;
		private float[] farPlanes;

		private RasterizerState rs;

		public DirectionalLight(GraphicsDevice device, Camera mainCamera, float near, float far, float[] splitDistances)
        {
			int num = splitDistances.Length + 1;
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

			camera = new CameraCSM(mainCamera, near, far, -1, -1);

			cameras = new CameraCSM[num];
			lightViewProjections = new Matrix[num];
			cascadeOffsets = new Vector4[num];
			cascadeScales = new Vector4[num];
			targetsArr = new RenderTarget2D(device, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 
				1, RenderTargetUsage.PreserveContents, false, num);
			for (int i = 0; i < num; i++)
            {
				if (i == 0)
					cameras[i] = new CameraCSM(mainCamera, mainCamera.Near, farPlanes[i], 0, splitDistances[i]);
				else if (i < num - 1)
					cameras[i] = new CameraCSM(mainCamera, farPlanes[i - 1], farPlanes[i], splitDistances[i - 1], splitDistances[i]);
				else cameras[i] = new CameraCSM(mainCamera, farPlanes[i - 1], mainCamera.Far, splitDistances[i - 1], 1f);
			}

			target = new RenderTarget2D(device, 1024, 1024, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);

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

		public void UpdateCameras(Vector3 direction, Color color)
        {
			this.lightDirection = direction;
			this.lightColor = color.ToVector3();

			for (int i = 0; i < cameras.Length; i++)
            {
				cameras[i].Update(direction);
            }

			camera.Update(direction);
        }

        public void DrawShadowmap(GraphicsDevice device, World world)
		{
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

				DrawOneCamera(device, camera, world);

				lightViewProjections[i] = camera.GetViewMatrix() * camera.GetProjectionMatrix();

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

			Main.WVP.SetProjection(Main.camera.GetProjectionMatrix());
			Main.WVP.SetView(Main.camera.GetViewMatrix());
		}

		public void Bind(Effect effect)
		{
			Matrix globalShadowMatrix = MakeGlobalShadowMatrix(Main.camera, lightDirection);

			effect.Parameters["NumCascades"].SetValue(farPlanes.Length);
			effect.Parameters["LightResolution"].SetValue(new Vector2(1024));

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

		private void DrawOneCamera(GraphicsDevice device, CameraCSM camera, World world)
        {
			Main.WVP.SetProjection(camera.GetProjectionMatrix());
			Main.WVP.SetView(camera.GetViewMatrix());

			for (int x = -world.DrawDistanceHoriz; x <= world.DrawDistanceHoriz; x++)
			{
				for (int y = -world.DrawDistanceVert; y <= world.DrawDistanceVert; y++)
				{
					for (int z = -world.DrawDistanceHoriz; z <= world.DrawDistanceHoriz; z++)
					{
						ChunkPosition chunkPos = ChunkPosition.WorldSpaceChunk(world.player.Position);
						chunkPos.X += x;
						chunkPos.Y += y;
						chunkPos.Z += z;

						if (world.ChunkManager.IsInWorldBounds(chunkPos))
						{
							ChunkMesh mesh = world.ChunkManager.GetMesh(chunkPos, 0);
							Matrix transform = world.ChunkManager.GetTransform(chunkPos);

							if (mesh != null)
							{
								mesh.DrawDepth(device, Main.assetsManager.GetAsset<Effect>("depth"), transform);
							}
						}
					}
				}
			}

			world.EntityManager.Draw(device, Main.assetsManager.GetAsset<Effect>("depth"));

			world.DrawShadowmap(device, Main.assetsManager.GetAsset<Effect>("depth"));
		}

		public RenderTarget2D GetShadowmapBuffer()
        {
			return target;
        }

		public RenderTarget2D GetShadowmapBuffers()
        {
			return targetsArr;
        }
    }
}
