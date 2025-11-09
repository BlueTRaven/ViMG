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

namespace ViMG.Entities
{
    public class Line : Entity
    {
        private static VerySimpleMesh mesh;
        public readonly Vector3 endPosition;
        public readonly float width;
        public readonly float tileHeight;
        public readonly RendererDeferred.DrawMaterial material;
        public readonly RectangleF sourceRectangle;
        public readonly Color color;

		private float alive;
		private float time;

        public Line()
        {
        }

        public Line(Vector3 position, Vector3 endPosition, float width, float tileHeight, RendererDeferred.DrawMaterial material, RectangleF sourceRectangle, Color color, float time)
        {
            this.Position = position;
            this.endPosition = endPosition;
            this.width = width;
            this.tileHeight = tileHeight;
            this.material = material;
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

        public Color GetColor()
        {
            if (Alive > time - 2)
            {
                float p = (Alive - (time - 2f)) / 2f;
                return color * p;
            }
            else return color;
        }

        //public override void Draw(GraphicsDevice device, Effect effect)
        //{
        //    base.Draw(device, effect);

        //    if (mesh.IBO == null)
        //        mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Bottom);
        //    //mesh = MeshHelper.MakeEnemyQuad(device, 1, 1);

        //    if (tileHeight != -1)
        //        DrawHelper3D.DrawLineTiled(Position, endPosition, width, tileHeight, material, mesh, sourceRectangle, color);
        //    else DrawHelper3D.DrawLine(Position, endPosition, width, material, mesh, sourceRectangle, color);
        //}
    }
}
