using BepuPhysics.Constraints;
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
    public class RendererDoor : EntityRenderer
    {
        private static VerySimpleMesh mountMesh;
        private static VerySimpleMesh doorMesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("cubes_textures");

        public RendererDoor(GraphicsDevice device) : base("door", device)
        {
        }

        public override Type[] GetRenderedTypes()
        {
            return [typeof(Door)];
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> entities)
        {
            if (doorMesh.IBO == null)
            {
                mountMesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f, Enums.Alignment.Center);
                doorMesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2f, Enums.Alignment.Center);
            }

            //var doors = entityManager.GetAll<Door>();

            //foreach (Door door in doors)

            var iter = new Iterator<Door>(entities);
            while (iter.Next(out Door door))
            {
                var position = door.world.PhysicsInfo.Simulation.Bodies[door.mountHandle].Pose.Position;
                var orientation = door.world.PhysicsInfo.Simulation.Bodies[door.mountHandle].Pose.Orientation;

                Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mountMesh,
                    Matrix.CreateFromQuaternion(new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W)) *
                    Matrix.CreateTranslation(position), sourceRect: new RectangleF(0, 128, 16, 32)));

                position = door.world.PhysicsInfo.Simulation.Bodies[door.doorHandle].Pose.Position;
                orientation = door.world.PhysicsInfo.Simulation.Bodies[door.doorHandle].Pose.Orientation;

                Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, doorMesh,
                    Matrix.CreateFromQuaternion(new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W)) *
                    Matrix.CreateTranslation(position), sourceRect: new RectangleF(0, 128, 16, 32)));
            }
        }
    }
}
