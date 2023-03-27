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
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;
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
				mesh = MeshHelper.MakeEnemyQuad(device, 1, 1);

            if (tileHeight != -1)
                DrawHelper3D.DrawLineTiled(Position, endPosition, width, tileHeight, mesh, texture, sourceRectangle, color);
            else DrawHelper3D.DrawLine(Position, endPosition, width, mesh, texture, sourceRectangle, color);
        }
	}
}
