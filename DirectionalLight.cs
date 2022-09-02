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
		//TODO: array of cameras
		private CameraCSM[] cameras;

		public readonly float width;
		public readonly float height;

		private RenderTarget2D target;
		private RenderTarget3D targets;
		private RenderTarget2D targetsArr;

		private Matrix[] lightViewProjections;
		private Vector3 lightDirection;
		private Vector3 lightColor;

		public DirectionalLight(GraphicsDevice device, Camera mainCamera, float near, float far, float[] farPlanes)
        {
			camera = new CameraCSM(mainCamera, near, far);

			cameras = new CameraCSM[farPlanes.Length];
			lightViewProjections = new Matrix[farPlanes.Length];
			targets = new RenderTarget3D(device, 1024, 1024, farPlanes.Length, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
			targetsArr = new RenderTarget2D(device, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.PreserveContents, false, farPlanes.Length);
			for (int i = 0; i < farPlanes.Length; i++)
            {
				if (i == 0)
					cameras[i] = new CameraCSM(mainCamera, mainCamera.Near, farPlanes[i]);
				else cameras[i] = new CameraCSM(mainCamera, farPlanes[i - 1], farPlanes[i]);
			}

			target = new RenderTarget2D(device, 1024, 1024, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);

			Main.CubeEffect.Parameters["CascadePlanesUsed"].SetValue(farPlanes.Length);
			Main.CubeEffect.Parameters["CascadePlaneDistances"].SetValue(farPlanes);
			Main.CubeEffect.Parameters["FarPlane"].SetValue(Main.camera.Far);
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
			this.lightDirection = -direction;
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
				Main.CubeEffect.Parameters["EnableShadows"].SetValue(false);
				return;
			}
			else
			{
				Main.CubeEffect.Parameters["EnableShadows"].SetValue(true);
				Main.CubeEffect.Parameters["EnablePCF"].SetValue(Main.ENABLE_PCF);
				Main.CubeEffect.Parameters["LightResolution"].SetValue(new Vector2(1024));
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
			}

			Main.WVP.SetProjection(Main.camera.GetProjectionMatrix());
			Main.WVP.SetView(Main.camera.GetViewMatrix());

			Main.CubeEffect.Parameters["LightViewProjection"].SetValue(camera.GetViewMatrix() * camera.GetProjectionMatrix());
			Main.CubeEffect.Parameters["LightPos"].SetValue(camera.Position);
			Main.CubeEffect.Parameters["LightDirection"].SetValue(lightDirection);
			Main.CubeEffect.Parameters["LightViewProjections"].SetValue(lightViewProjections);
			Main.CubeEffect.Parameters["LightColor"].SetValue(lightColor);
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
