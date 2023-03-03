using BepuPhysics;
using BepuPhysics.Collidables;
using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class PhysicsTestBall : Entity
    {
        private (VertexBuffer vbo, IndexBuffer ibo) mesh;

        private TypedIndex physicsShapeIndex;
        private BodyHandle physicsHandle;

        public PhysicsTestBall(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            Sphere colSphere = new Sphere(Cube.CUBE_SCALE / 2f);
            physicsShapeIndex = world.PhysicsSimulation.Shapes.Add(colSphere);
            physicsHandle = world.PhysicsSimulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(this.Position.ToNumerics()), 
                colSphere.ComputeInertia(1), physicsShapeIndex, 0.0001f));
        }

        public override void OnUnload()
        {
            base.OnUnload();

            world.PhysicsSimulation.Shapes.Remove(physicsShapeIndex);
            world.PhysicsSimulation.Bodies.Remove(physicsHandle);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            Position = world.PhysicsSimulation.Bodies.GetBodyReference(physicsHandle).Pose.Position;
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.vbo == null)
            {
                List<VertexCube> vertices = new List<VertexCube>();
                List<int> indices = new List<int>();
                DrawHelper3D.MakeUVSphereRaw(vertices, indices, new Vector3(Cube.CUBE_SCALE / 2f), BrUtility.RectangleF.Empty, Cube.CUBE_SCALE / 2f);

                mesh = MeshHelper.MakeSimplerMesh(device, vertices, indices);
            }

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(DrawHelper.WhitePixel,
                DrawHelper.BlackPixel, DrawHelper.WhitePixel, mesh.vbo, mesh.ibo,
                Matrix.CreateTranslation(Position - new Vector3(Cube.CUBE_SCALE / 2f)), tintColor: Color.Red.ToVector3()));
        }
    }
}
