using BrUtility;
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
    }
}
