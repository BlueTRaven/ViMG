using BepuPhysics.Constraints;
using Engine.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Entities.Renderers
{
    public class RendererEntityItem : EntityRenderer
    {
        public RendererEntityItem(GraphicsDevice device) : base("entity_item", device)
        {
        }

        private static int[] types = [0];
        public override int[] GetRenderedTypes()
        {
            if (types[0] == 0)
                types[0] = Main.Registry.EntityRegistry.Get<EntityItem>().Id;
            return types;
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int type)
        {
            base.RenderClientEnt(device, deltaTime, client, type);

            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var ent = client.currInterpState.entities.GetByRef(ref reference);

                Vector3 origin = new Vector3(Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 16f);

                ItemInstance itemInstance = new ItemInstance(Main.Registry.ItemRegistry.Get(ent.counters[0]), ent.counters[1], ent.counters[2]);
                if (itemInstance.item is ItemCube)
                    origin.Z = Cube.CUBE_SCALE / 4f;

                if (itemInstance.item != null)
                {
                    itemInstance.item.Client.DrawInWorld(device, client.Renderer, itemInstance,
                        Matrix.CreateTranslation(-origin) *
                        Matrix.CreateFromQuaternion(ent.rotation) *
                        Matrix.CreateTranslation(ent.position)
                        );
                }
            }
        }
    }
}
