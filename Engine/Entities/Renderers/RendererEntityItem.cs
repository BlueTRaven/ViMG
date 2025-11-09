using BepuPhysics.Constraints;
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
    }
}
