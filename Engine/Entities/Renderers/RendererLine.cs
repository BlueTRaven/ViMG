using BrUtility;
using Engine.Clients;
using Microsoft.Xna.Framework;
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

        private int[] types = [0];
        public override int[] GetRenderedTypes()
        {
            if (types[0] == 0)
                types[0] = Main.Registry.EntityRegistry.Get<Line>().Id;
            return types;
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int type)
        {
            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entCurr = client.Current().entities.GetById(reference.id);
                var entPrev = client.Previous(1).entities.GetById(reference.id);

                var position = entPrev.GetInterpPosition(entCurr, client.TimeC);
                var endPosition = Vector3.Lerp(entPrev.rotation.ToVector4().ToVector3(), entCurr.rotation.ToVector4().ToVector3(), (float)Main.TimeP);
                var time = entPrev.GetInterpTimer(entCurr, 0, client.TimeC);
                var width = entPrev.GetInterpTimer(entCurr, 1, client.TimeC);
                var tileHeight = entPrev.GetInterpTimer(entCurr, 2, client.TimeC);
                var alive = entPrev.GetInterpTimer(entCurr, 3, client.TimeC);
                var colorPrev = new Color((uint)entPrev.counters[0]);
                var colorCurr = new Color((uint)entCurr.counters[0]);
                var color = Color.Lerp(colorPrev, colorCurr, (float)Main.TimeP);
                var materialSet = entCurr.counters[1];

                (RendererDeferred.DrawMaterial material, RectangleF sourceRectangle) = Line.GetMaterialFromSet(materialSet);
                if (tileHeight != -1)
                    DrawHelper3D.DrawLineTiled(position, endPosition, width, tileHeight, material, mesh, sourceRectangle, color);
                else DrawHelper3D.DrawLine(position, endPosition, width, material, mesh, sourceRectangle, color);
            }
        }
    }
}
