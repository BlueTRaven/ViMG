using BrUtility;
using Engine.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public class RendererGenericExplosion : EntityRenderer
    {
        private VerySimpleMesh mesh;

        public RendererGenericExplosion(GraphicsDevice device) : base("generic_explosion", device)
        {
            mesh = MeshHelper.MakeUVSphere(device, 1f);
        }

        private static int[] types = [0];
        public override int[] GetRenderedTypes()
        {
            if (types[0] == 0)
                types[0] = Main.Registry.EntityRegistry.Get<GenericExplosion>().Id;
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

                float radius = (1 - entPrev.GetInterpTimer(entCurr, 0, client.TimeC) / GenericExplosion.EXPLOSION_TIME) * entPrev.GetInterpTimer(entCurr, 1, client.TimeC);
                float sort = (entPrev.GetInterpPosition(entCurr, client.TimeC) - Main.camera.Position).Length();
                Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(sort,
                    new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), mesh,
                    Matrix.CreateScale(radius) * Matrix.CreateTranslation(entPrev.GetInterpPosition(entCurr, client.TimeC)), null, Color.Red * 0.5f));
            }
        }
    }
}
