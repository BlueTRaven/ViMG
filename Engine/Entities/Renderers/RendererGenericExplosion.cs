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

        private static Type[] types = [ typeof(GenericExplosion) ];
        public override Type[] GetRenderedTypes()
        {
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> entities)
        {
            return;
            //var entities = entityManager.GetAll<GenericExplosion>();

            //foreach (GenericExplosion explosion in entities)
            var iter = new Iterator<GenericExplosion>(entities);
            while (iter.Next(out GenericExplosion explosion))
            {
                float radius = (1 - explosion.timer / GenericExplosion.EXPLOSION_TIME) * explosion.radius;
                float sort = (explosion.Position - Main.camera.Position).Length();
                Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(sort,
                    new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), mesh,
                    Matrix.CreateScale(radius) * Matrix.CreateTranslation(explosion.Position), null, Color.Red * 0.5f));
            }
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, string type)
        {
            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                // TODO get rid of str compare
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entCurr = client.Current().entities.GetById(reference.id);
                var entPrev = client.Previous(1).entities.GetById(reference.id);

                float radius = (1 - entPrev.GetInterpTimer(entCurr, 0) / GenericExplosion.EXPLOSION_TIME) * entPrev.GetInterpTimer(entCurr, 1);
                float sort = (entPrev.GetInterpPosition(entCurr) - Main.camera.Position).Length();
                Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(sort,
                    new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), mesh,
                    Matrix.CreateScale(radius) * Matrix.CreateTranslation(entPrev.GetInterpPosition(entCurr)), null, Color.Red * 0.5f));
            }
        }
    }
}
