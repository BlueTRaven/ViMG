using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class Skullhead : Entity
    {
        private static (VertexBuffer VBO, IndexBuffer IBO) meshHead;
        private static (VertexBuffer VBO, IndexBuffer IBO) meshVertibrae;

		private float alive;

        public Skullhead(Vector3 position)
        {
			this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			AlwaysRender = true;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			alive += (float)deltaTime;
        }

        public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (meshHead.VBO == null)
				meshHead = MeshHelper.MakeEnemyQuad(device, Cube.PIXEL_SCALE * 128, Cube.PIXEL_SCALE * 128);

			if (meshVertibrae.VBO == null)
				meshVertibrae = MeshHelper.MakeEnemyQuad(device, Cube.PIXEL_SCALE * 16 * 3, Cube.PIXEL_SCALE * 16);

			//Vector3 tintColor = invulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("skullhead"),
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshHead.VBO, meshHead.IBO,
				Matrix.CreateTranslation(-new Vector3(0, Cube.PIXEL_SCALE * 64, 0)) *
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), new RectangleF(0, 0, 128, 128), Color.White.ToVector3()));

			for (int i = 0; i < 12; i++)
			{
				float yoff = Cube.PIXEL_SCALE * i * 12;

				float ioff = (float)i * 0.63f;
				float t = ((alive + ioff) % 2f) / 2f;

				float s = MathF.Sin(MathF.PI * 2 * t) * MathHelper.Lerp(Cube.CUBE_SCALE / 8f, Cube.CUBE_SCALE / 2f, 1 - ((float)i / 12f));

				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("skullhead"),
					DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshVertibrae.VBO, meshVertibrae.IBO,
					Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
					Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
					Matrix.CreateTranslation(Position - new Vector3(0, Cube.PIXEL_SCALE * 80 + yoff, 0) + Main.camera.Right * s), new RectangleF(128, 0, 48, 16), Color.White.ToVector3()));
			}

			//if (Health < MaxHealth)
			//DrawHelper3D.DrawHealthbar(device, Health, MaxHealth, Position);
		}
	}
}
