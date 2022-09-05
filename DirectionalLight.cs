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

		private float[] farPlanes;

		public DirectionalLight(GraphicsDevice device, Camera mainCamera, float near, float far, float[] farPlanes)
        {
			camera = new CameraCSM(mainCamera, near, far);

			cameras = new CameraCSM[farPlanes.Length + 1];
			lightViewProjections = new Matrix[farPlanes.Length + 1];
			targetsArr = new RenderTarget2D(device, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 
				1, RenderTargetUsage.PreserveContents, false, farPlanes.Length + 1);
			for (int i = 0; i < farPlanes.Length + 1; i++)
            {
				if (i == 0)
					cameras[i] = new CameraCSM(mainCamera, mainCamera.Near, farPlanes[i]);
				else if (i < farPlanes.Length)
					cameras[i] = new CameraCSM(mainCamera, farPlanes[i - 1], farPlanes[i]);
				else cameras[i] = new CameraCSM(mainCamera, farPlanes[i - 1], mainCamera.Far);
			}

			target = new RenderTarget2D(device, 1024, 1024, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);

			this.farPlanes = farPlanes;

			Main.CubeLitEffect.Parameters["NumCascades"].SetValue(farPlanes.Length);
			Main.CubeLitEffect.Parameters["CascadePlaneDistances"].SetValue(farPlanes);
			Main.CubeLitEffect.Parameters["FarPlane"].SetValue(Main.camera.Far);
			(Main.Registry.ItemRegistry.Get("debug_depth_target") as Items.ItemDebugDepthTarget).DepthTarget = target;
		}

		public void SetPipelineState(GraphicsDevice device)
        {
			device.DepthStencilState = Main.genericDSS;
			//device.RasterizerState = Main.reverseRS;
			device.SamplerStates[3] = Main.shadowBorderClampSS;
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

        public void DrawShadowmap(World world, GraphicsDevice device)
		{
			if (!Main.ENABLE_SHADOWS)
			{
				Main.CubeLitEffect.Parameters["EnableShadows"].SetValue(false);
				return;
			}
			else
			{
				Main.CubeLitEffect.Parameters["EnableShadows"].SetValue(true);
				Main.CubeLitEffect.Parameters["EnablePCF"].SetValue(Main.ENABLE_PCF);
				Main.CubeLitEffect.Parameters["LightResolution"].SetValue(new Vector2(1024));
			}

			SetPipelineState(device);
			//device.SetRenderTarget(target);
			//device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, device.Viewport.MaxDepth, 0);

			//DrawOneCamera(device, camera, world);

			for (int i = 0; i < cameras.Length; i++)
			{
				CameraCSM camera = cameras[i];

				//device.SetRenderTarget(targets, i);
				device.SetRenderTarget(targetsArr, i);
				device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, device.Viewport.MaxDepth, 0);

				DrawOneCamera(device, camera, world);

				lightViewProjections[i] = camera.GetViewMatrix() * camera.GetProjectionMatrix();

				/*var texScaleBias = Matrix.CreateScale(0.5f, -0.5f, 1.0f)
				   * Matrix.CreateTranslation(0.5f, 0.5f, 0.0f);
				var shadowMatrix = camera.GetViewMatrix() * camera.GetProjectionMatrix();
				shadowMatrix = shadowMatrix * texScaleBias;

				// Store the split distance in terms of view space depth
				var clipDist = Main.camera.Far - Main.camera.Near;

				//shadowCamera: lightViewProjections/CameraCSM
				//camera: Main.camera
				farPlanes[i] = Main.camera.Near + splitDist * clipDist;

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
				_meshEffect.CascadeOffsets[cascadeIdx] = new Vector4(-cascadeCorner, 0.0f);
				_meshEffect.CascadeScales[cascadeIdx] = new Vector4(cascadeScale, 1.0f);*/
			}

			Main.WVP.SetProjection(Main.camera.GetProjectionMatrix());
			Main.WVP.SetView(Main.camera.GetViewMatrix());

			Main.CubeLitEffect.Parameters["LightViewProjection"].SetValue(camera.GetViewMatrix() * camera.GetProjectionMatrix());
			Main.CubeLitEffect.Parameters["LightPos"].SetValue(camera.Position);
			Main.CubeLitEffect.Parameters["LightDirection"].SetValue(lightDirection);
			Main.CubeLitEffect.Parameters["LightViewProjections"].SetValue(lightViewProjections);
			Main.CubeLitEffect.Parameters["LightColor"].SetValue(lightColor);
			//Main.CubeEffect.Parameters["LightDirections"].SetValue(lightDirections);
		}

		private void DrawOneCamera(GraphicsDevice device, CameraCSM camera, World world)
        {
			Main.WVP.SetProjection(camera.GetProjectionMatrix());
			Main.WVP.SetView(camera.GetViewMatrix());

			for (int x = -world.DrawDistanceHoriz; x <= world.DrawDistanceHoriz; x++)
			{
				for (int y = -world.DrawDistanceVert; y <= world.DrawDistanceVert; y++)
				{
					for (int z = -world.DrawDistanceHoriz; z < world.DrawDistanceHoriz; z++)
					{
						ChunkPosition chunkPos = ChunkPosition.WorldSpaceChunk(world.player.Position);
						chunkPos.X += x;
						chunkPos.Y += y;
						chunkPos.Z += z;

						if (world.ChunkManager.IsInWorldBounds(chunkPos))
						{
							ChunkMesh mesh = world.ChunkManager.GetMesh(chunkPos);
							Matrix transform = world.ChunkManager.GetTransform(chunkPos);

							if (mesh != null)
							{
								mesh.DrawDepth(device, Main.assetsManager.GetAsset<Effect>("depth"), transform);
							}
						}
					}
				}
			}

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
