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
		private CameraOrthographic csmCameras;

		public readonly float width;
		public readonly float height;

        public DirectionalLight(GraphicsDevice device, Vector3 startPosition, Vector3 startRotation, Vector3 startScale, float left, float right, float top, float bottom, float near, float far)
        {
            //camera = new CameraOrthographic(startPosition, startRotation, startScale, left, right, top, bottom, near, far);

			width = Math.Max(left, right) - Math.Min(left, right);
			height = Math.Max(top, bottom) - Math.Min(top, bottom);
        }

		public DirectionalLight(Camera mainCamera, float near, float far)
        {
			camera = new CameraCSM(mainCamera, near, far);
        }

		public void UpdateDirection(Vector3 position, Vector3 direction, Vector3 angles)
        {
			Vector3[] corners = Main.camera.GetFrustum().GetCorners();

			Vector3 center = Vector3.Zero;

			foreach (Vector3 corner in corners)
			{
				center += corner;
			}
			center /= corners.Length;

			Matrix viewMatrix = 
				Matrix.CreateTranslation(-center) *
				Matrix.CreateRotationZ(angles.Z) *
				Matrix.CreateRotationY(angles.Y) *
				Matrix.CreateRotationX(angles.X) *
				Matrix.CreateScale(Vector3.One);

			//Matrix viewMatrix = Matrix.CreateLookAt(position, position - direction, new Vector3(0, 1, 0));

			float minX = float.MaxValue;
			float maxX = float.MinValue;
			float minY = float.MaxValue;
			float maxY = float.MinValue;
			float minZ = float.MaxValue;
			float maxZ = float.MinValue;

			foreach (Vector3 corner in corners)
			{
				Vector3 transformed = Vector3.Transform(corner, viewMatrix);
				minX = MathHelper.Min(minX, transformed.X);
				maxX = MathHelper.Max(maxX, transformed.X);
				minY = MathHelper.Min(minY, transformed.Y);
				maxY = MathHelper.Max(maxY, transformed.Y);
				minZ = MathHelper.Min(minZ, transformed.Z);
				maxZ = MathHelper.Max(maxZ, transformed.Z);
			}

			float zRange = 10;

			if (minZ < 0)
				minZ *= zRange;
			else minZ /= zRange;

			if (maxZ < 0)
				maxZ /= zRange;
			else maxZ *= zRange;

			//Matrix ortho = Matrix.CreateOrthographicOffCenter(minX, maxX, minY, maxY, minZ, maxZ);

			//camera = new CameraOrthographic(center, angles, Vector3.One, minX, maxX, maxY, minY, minZ, maxZ);
			//camera = new CameraOrthographic(center + direction, direction, minZ, maxZ, viewMatrix, ortho);
		}

		public void SetPipelineState(GraphicsDevice device)
        {
			device.DepthStencilState = Main.genericDSS;
			//device.RasterizerState = Main.reverseRS;
			device.SamplerStates[3] = Main.shadowBorderClampSS;
		}

        public void DrawShadowmap(World world, GraphicsDevice device)
		{
			//if (buffer == null)
			//buffer = new StructuredBuffer(device, typeof(DirectionalLightGPU), 1, BufferUsage.WriteOnly, ShaderAccess.Read);

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

			device.SetRenderTarget(Main.DepthTarget);

			SetPipelineState(device);

			//device.Clear(Color.White);
			device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, device.Viewport.MaxDepth, 0);

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

			Main.WVP.SetProjection(Main.camera.GetProjectionMatrix());
			Main.WVP.SetView(Main.camera.GetViewMatrix());

			/*DirectionalLightGPU dld = new DirectionalLightGPU()
			{
				ViewProjection = camera.GetViewMatrix() * camera.GetProjectionMatrix(),
				Position = new Vector4(camera.Position, 1f),
				Color = Color.White.ToVector4(),
			};*/

			//data[0] = dld;
			//buffer.SetData(data);
			//Main.CubeEffect.Parameters["DirectionalLights"].SetValue(buffer);
			Main.CubeEffect.Parameters["LightViewProjection"].SetValue(camera.GetViewMatrix() * camera.GetProjectionMatrix());
			Main.CubeEffect.Parameters["LightPos"].SetValue(camera.Position);
			Main.CubeEffect.Parameters["LightDirection"].SetValue(-camera.Forward);
		}
    }
}
