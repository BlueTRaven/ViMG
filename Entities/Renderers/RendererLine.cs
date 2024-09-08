using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public class RendererLine : EntityRenderer
    {
        private VerySimpleMesh mesh;

        public RendererLine(GraphicsDevice device) : base("line", device)
        {
            mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Bottom);
        }

        private Type[] types = [typeof(Line)];
        public override Type[] GetRenderedTypes()
        {
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex)
        {
            var lines = entityManager.GetAll<Line>();

            foreach (Line line in lines)
            {
                if (line.tileHeight != -1)
                    DrawHelper3D.DrawLineTiled(line.Position, line.endPosition, line.width, line.tileHeight, line.material, mesh, line.sourceRectangle, line.GetColor());
                else DrawHelper3D.DrawLine(line.Position, line.endPosition, line.width, line.material, mesh, line.sourceRectangle, line.GetColor());
            }
        }
    }
}
