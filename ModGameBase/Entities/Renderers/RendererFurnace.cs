using Engine;
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

namespace ModGameBase.Entities.Renderers
{
    public class RendererFurnace : EntityRenderer
    {
        public RendererFurnace(GraphicsDevice device) : base("furnace", device)
        {
        }

        public override int[] GetRenderedTypes()
        {
            return [GlobalState.Registry.EntityRegistry.Get<EntityFurnace>().Id];
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int entityType)
        {
            base.RenderClientEnt(device, deltaTime, client, entityType);

            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != entityType) continue;

                var entity = GlobalState.Registry.EntityRegistry.Get(entityType).GetInterpolated(client, reference);

                if (entity.timers[0] > 0)
                {
                    client.LightManager.Add(new LightManager2.LightConfig
                    {
                        position = entity.position,
                        min = Cube.CUBE_SCALE * 4,
                        max = Cube.CUBE_SCALE * 8,
                        color = Color.OrangeRed.ToVector4(),
                    });
                }
            }

        }
    }
}
