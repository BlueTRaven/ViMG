using BepuPhysics.Constraints;
using Engine.Clients;
using Engine.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Entities.Renderers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace ModGameBase.Entities.Renderers
{
    public class RendererFlame : EntityRenderer
    {
        public RendererFlame(GraphicsDevice device) : base("cube_flame", device)
        {
        }

        public override int[] GetRenderedTypes()
        {
            return [Main.Registry.EntityRegistry.Get<EntityCubeFlame>().Id];
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int entityType)
        {
            base.RenderClientEnt(device, deltaTime, client, entityType);

            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != entityType) continue;

                var entity = Main.Registry.EntityRegistry.Get(entityType).GetInterpolated(client, reference);

                float p0 = (((float)client.CurrentTime + entity.timers[0]) % 0.65f) / 0.65f;
                float s0 = MathF.Sin(MathF.PI * 2 * p0) * Cube.CUBE_SCALE * 0.25f;

                client.LightManager.AddShadowmapped(new LightManager2.LightConfig
                {
                    position = entity.position + new Vector3(Cube.CUBE_SCALE / 2f),
                    min = Cube.CUBE_SCALE * 4 + s0,
                    max = Cube.CUBE_SCALE * 8,
                    color = Color.OrangeRed,
                });
            }
        }
    }
}
