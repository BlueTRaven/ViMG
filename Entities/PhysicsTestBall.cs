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
using ViMG.Physics;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    public class PhysicsTestBall : Entity
    {
        private static (VertexBuffer vbo, IndexBuffer ibo) mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("cubes_textures");

        private TypedIndex physicsShapeIndex;
        private BodyHandle physicsHandle;

        private ContactChecker contactChecker;

        public PhysicsTestBall(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            Sphere colSphere = new Sphere(Cube.CUBE_SCALE / 2f);
            physicsShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(colSphere);
            physicsHandle = world.PhysicsInfo.Simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(this.Position.ToNumerics()), 
                new BodyInertia() { InverseMass = 1 }, physicsShapeIndex, 0.0001f));

            contactChecker = new ContactChecker();
        }

        public override void OnUnload()
        {
            base.OnUnload();

            world.PhysicsInfo.Simulation.Shapes.Remove(physicsShapeIndex);
            world.PhysicsInfo.Simulation.Bodies.Remove(physicsHandle);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            Position = world.PhysicsInfo.Simulation.Bodies.GetBodyReference(physicsHandle).Pose.Position;

            contactChecker.Update(world, physicsHandle);

            if (contactChecker.OnGround)
            {
                if (!world.PhysicsInfo.Simulation.Bodies[physicsHandle].Awake)
                    world.PhysicsInfo.Simulation.Awakener.AwakenBody(physicsHandle);

                contactChecker.OnGround = false;
                Vector3 jumpVector = contactChecker.GroundNormal * Cube.CUBE_SCALE * 6;
                world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = jumpVector.ToNumerics();
            }
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.vbo == null)
            {
                List<VertexCube> vertices = new List<VertexCube>();
                List<int> indices = new List<int>();
                DrawHelper3D.MakeUVSphereRaw(vertices, indices, new Vector3(Cube.CUBE_SCALE / 2f), BrUtility.RectangleF.Empty, Cube.CUBE_SCALE / 2f);

                mesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
            }

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(material, mesh.vbo, mesh.ibo,
                Matrix.CreateTranslation(Position - new Vector3(Cube.CUBE_SCALE / 2f)), sourceRect: new RectangleF(0, 0, 16, 16)));
        }
    }
}
