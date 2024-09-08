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

namespace ViMG.Entities.Renderers
{
    public class RendererLightning : EntityRenderer
    {
        private static Color LightningColor = new Color(255, 253, 141);
        private VerySimpleMesh mesh;
        public RendererLightning(GraphicsDevice device) : base("lightning", device)
        {
            mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Bottom);
        }

        private static Type[] types = [
            typeof(Lightning),
            typeof(AimedLightning),
        ];
        public override Type[] GetRenderedTypes()
        {
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex)
        {
            var lightnings = entityManager.GetAll<Lightning>();

            foreach (Lightning lightning in lightnings)
            {
                for (int i = 0; i < lightning.positions.Length; i++)
                {
                    Vector3 prev;
                    if (i == 0)
                        prev = lightning.Position;
                    else prev = lightning.positions[i - 1];

                    Vector3 current = lightning.positions[i];

                    DrawHelper3D.DrawLine(prev, current, Cube.CUBE_SCALE / 4f, new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), mesh, RectangleF.Empty, LightningColor);
                }
            }

            var aimedLightnings = entityManager.GetAll<AimedLightning>();

            foreach (AimedLightning lightning in aimedLightnings)
            {
                for (int i = 0; i < lightning.positions.Length; i++)
                {
                    Vector3 prev;
                    if (i == 0)
                        prev = lightning.Position;
                    else prev = lightning.positions[i - 1];

                    Vector3 current = lightning.positions[i];

                    DrawHelper3D.DrawLine(prev, current, Cube.CUBE_SCALE / 4f, new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                        mesh, RectangleF.Empty, LightningColor);
                }
            }
        }
    }
}
