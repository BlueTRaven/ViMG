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
        public CameraOrthographic camera;

		public struct DirectionalLightGPU
        {
			public Matrix ViewProjection;
			public Vector4 Position;
			public Vector4 Color;
        }

		public readonly float width;
		public readonly float height;

		//private DirectionalLightGPU[] data = new DirectionalLightGPU[1];
		//private StructuredBuffer buffer;

        public DirectionalLight(Vector3 startPosition, Vector3 startRotation, Vector3 startScale, float left, float right, float top, float bottom, float near, float far)
        {
            camera = new CameraOrthographic(startPosition, startRotation, startScale, left, right, top, bottom, near, far);
			camera.Scale = Main.camera.Scale;

			width = Math.Max(left, right) - Math.Min(left, right);
			height = Math.Max(top, bottom) - Math.Min(top, bottom);
        }

		public void SetPipelineState(GraphicsDevice device)
        {
			device.DepthStencilState = Main.genericDSS;
			device.RasterizerState = Main.reverseRS;
			device.SamplerStates[3] = Main.shadowBorderClampSS;
		}

        public void DrawShadowmap(World world, GraphicsDevice device)
		{
			//if (buffer == null)
				//buffer = new StructuredBuffer(device, typeof(DirectionalLightGPU), 1, BufferUsage.WriteOnly, ShaderAccess.Read);

			if (!Main.ENABLE_SHADOWS)
			{
				/*DirectionalLightGPU disabledData = new DirectionalLightGPU()
				{
					ViewProjection = Matrix.Identity,
					Position = new Vector4(camera.Position, 1f),
					Color = Color.White.ToVector4(),
				};

				data[0] = disabledData;
				buffer.SetData(data);
				Main.CubeEffect.Parameters["DirectionalLights"].SetValue(buffer);*/

				Main.CubeEffect.Parameters["EnableShadows"].SetValue(false);
				return;
			}
			else Main.CubeEffect.Parameters["EnableShadows"].SetValue(true);

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
		}
    }
}
