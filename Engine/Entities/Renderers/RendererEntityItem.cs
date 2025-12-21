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

        private static Type[] types = [typeof(EntityItem)];
        public override Type[] GetRenderedTypes()
        {
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> entities)
        {
            return;
            //var items = entityManager.GetAll<EntityItem>();

            //foreach (EntityItem itemEntity in items)
            var iter = new Iterator<EntityItem>(entities);
            while (iter.Next(out EntityItem itemEntity))
            {
                Vector3 origin = new Vector3(Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 16f);

                if (itemEntity.ItemInstance.item is ItemCube)
                    origin.Z = Cube.CUBE_SCALE / 4f;

                var reference = itemEntity.world.PhysicsInfo.Simulation.Bodies[itemEntity.physicsHandle];
                itemEntity.ItemInstance.item.DrawInWorld(device, itemEntity.world, itemEntity.ItemInstance,
                    Matrix.CreateTranslation(-origin) *
                    Matrix.CreateFromQuaternion(new Quaternion(reference.Pose.Orientation.X, reference.Pose.Orientation.Y, reference.Pose.Orientation.Z, reference.Pose.Orientation.W)) *
                    Matrix.CreateTranslation(reference.Pose.Position)
                    );
            }
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, string type)
        {
            base.RenderClientEnt(device, deltaTime, client, type);

            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                // TODO get rid of str compare
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entCurr = client.Current().entities.GetById(reference.id);
                var entPrev = client.Previous(1).entities.GetById(reference.id);

                Vector3 origin = new Vector3(Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE / 16f);

                ItemInstance itemInstance = new ItemInstance(Main.Registry.ItemRegistry.Get(entCurr.counters[0]), entCurr.counters[1], entCurr.counters[2]);
                if (itemInstance.item is ItemCube)
                    origin.Z = Cube.CUBE_SCALE / 4f;

                if (itemInstance.item != null)
                {
                    itemInstance.item.DrawInWorld(device, null, itemInstance,
                        Matrix.CreateTranslation(-origin) *
                        Matrix.CreateFromQuaternion(entPrev.GetInterpRotation(entCurr)) *
                        Matrix.CreateTranslation(entPrev.GetInterpPosition(entCurr))
                        );
                }
            }
        }
    }
}
