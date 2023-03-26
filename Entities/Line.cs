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
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;
        private static (VertexBuffer VBO, IndexBuffer IBO) debugMesh;
        private readonly Vector3 endPosition;
        private readonly float width;
        private readonly float tileHeight;
        private readonly Texture2D texture;
        private readonly RectangleF sourceRectangle;
        private readonly Color color;

		private float alive;
		private float time;

        public Line()
        {
        }

        public Line(Vector3 position, Vector3 endPosition, float width, float tileHeight, Texture2D texture, RectangleF sourceRectangle, Color color, float time)
        {
            this.Position = position;
            this.endPosition = endPosition;
            this.width = width;
            this.tileHeight = tileHeight;
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
                debugMesh = MeshHelper.MakeCenteredQuad(device, Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 4f);
			}

            Vector3 axis = endPosition - Position;
            float distance = axis.Length();
            axis.Normalize();

            Matrix mat = Matrix.CreateConstrainedBillboard(Position, Main.camera.Position, axis, -Main.camera.Forward, Vector3.Forward);

            if (tileHeight != -1)
            {
                int tileTimes = (int)(distance / tileHeight);
                float tileLastBit = distance % tileHeight;

                for (int i = 0; i < tileTimes; i++)
                {
                    Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(texture,
                        DrawHelper.BlackPixel, DrawHelper.WhitePixel, mesh.VBO, mesh.IBO,
                        Matrix.CreateScale(width, tileHeight, width) * mat * Matrix.CreateTranslation(axis * tileHeight * i),
                        sourceRectangle, color.ToVector3()));
                }

                //The "last bit" is the part that can't be tiled.
                //In order for this not to be squished, we have to fix the source rectangle.
                //Its height needs to be calculated, and then we need to offset its y position. This is due to the fact that Y is up in world space, but down in texture space.
                float fixedHeight = (tileLastBit / tileHeight) * sourceRectangle.height;
                Vector2 fixedPosition = sourceRectangle.Position;
                fixedPosition.Y += sourceRectangle.height - fixedHeight;

                Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(texture,
                    DrawHelper.BlackPixel, DrawHelper.WhitePixel, mesh.VBO, mesh.IBO,
                    Matrix.CreateScale(width, tileLastBit, width) * mat * Matrix.CreateTranslation(axis * tileHeight * tileTimes),
                    new RectangleF(fixedPosition, sourceRectangle.width, fixedHeight),
                    color.ToVector3()));
            }
            else
            {
                //-1 tile height means don't try tiling.
                Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(texture,
                    DrawHelper.BlackPixel, DrawHelper.WhitePixel, mesh.VBO, mesh.IBO,
                    Matrix.CreateScale(width, distance, width) * mat,
                    sourceRectangle, color.ToVector3()));
            }
            //Displays start and end points as red squares. For debugging purposes.
            /*Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(DrawHelper.WhitePixel,
                DrawHelper.BlackPixel, DrawHelper.WhitePixel, debugMesh.VBO, debugMesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(Position), RectangleF.Empty, Color.Red.ToVector3()));
            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(DrawHelper.WhitePixel,
                DrawHelper.BlackPixel, DrawHelper.WhitePixel, debugMesh.VBO, debugMesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(endPosition), RectangleF.Empty, Color.Red.ToVector3()));*/
        }
	}
}
