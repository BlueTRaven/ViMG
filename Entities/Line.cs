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
    public class Line : Entity
    {
		private const float PIXEL = Cube.CUBE_SCALE / 16;
        private readonly Vector3 endPosition;
        private readonly Texture2D texture;
        private readonly RectangleF sourceRectangle;
        private readonly Color color;
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

		private float alive;
		private float time;

        public Line()
        {
        }

        public Line(Vector3 position, Vector3 endPosition, Texture2D texture, RectangleF sourceRectangle, Color color, float time)
        {
            this.Position = position;
            this.endPosition = endPosition;
            this.texture = texture;
            this.sourceRectangle = sourceRectangle;
            this.color = color;
            this.time = time;
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

			if (alive >= time)
				world.EntityManager.Remove(this);
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh.VBO == null)
			{
				mesh = MeshHelper.MakeEnemyQuad(device, 1, 1);
			}

			//Matrix mat = Matrix.CreateBillboard(Position, Main.camera.Position, Main.camera.Up, Main.camera.Forward);

			Vector3 direction = Position - endPosition;
			float distance = direction.Length();
			direction.Normalize();

			float cameraDistance = (Position - Main.camera.Position).Length();
			float size = cameraDistance * PIXEL;

            float pitch = MathF.Asin(-direction.Y) - MathHelper.ToRadians(90);
			float yaw = MathF.Atan2(direction.X, direction.Z);

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(texture,
                DrawHelper.BlackPixel, DrawHelper.WhitePixel, mesh.VBO, mesh.IBO,
				Matrix.CreateScale(size, distance, size) *
				Matrix.CreateFromYawPitchRoll(yaw, pitch, 0) *
				Matrix.CreateTranslation(Position), sourceRectangle, color.ToVector3()));
        }
	}
}
