using BepuPhysics;
using BepuPhysics.Collidables;
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

namespace ViMG.Entities
{
    public class DoorWood : Entity
    {
        private (VertexBuffer VBO, IndexBuffer IBO) mesh;

        private TypedIndex mountShapeIndex;
        private TypedIndex doorShapeIndex;
        private BodyHandle mountHandle;
        private BodyHandle doorHandle;

        private ConstraintHandle hingeHandle;

        public DoorWood()
        {
        }

        public DoorWood(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            var shapeMount = new Box(Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f);
            var shapeDoor = new Box(Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 0.25f);
            var hinge = new AngularHinge() 
            {
                LocalHingeAxisA = Vector3.Up.ToNumerics(),  //not sure what to put here actually
                LocalHingeAxisB = Vector3.Up.ToNumerics(), 
                SpringSettings = new SpringSettings(30, 1) 
            };

            mountShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(shapeMount);
            doorShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(shapeDoor);
            mountHandle = world.PhysicsInfo.Simulation.Bodies.Add(BodyDescription.CreateKinematic(new RigidPose((Position - new Vector3(Cube.CUBE_SCALE * 1.1f, 0, 0)).ToNumerics()),
                mountShapeIndex, 0.001f));
            doorHandle = world.PhysicsInfo.Simulation.Bodies.Add(BodyDescription.CreateDynamic(Position.ToNumerics(), 
                shapeDoor.ComputeInertia(1), doorShapeIndex, 0.001f));
         
            hingeHandle = world.PhysicsInfo.Simulation.Solver.Add(mountHandle, doorHandle, hinge);

            var mountFilter = new Physics.SubgroupCollisionFilter(Physics.FilterGroups.GROUP_PLAYER, 0);
            var doorFilter = new Physics.SubgroupCollisionFilter(Physics.FilterGroups.GROUP_PLAYER, 1);
            mountFilter.DisableCollision(1);
            doorFilter.DisableCollision(0);
            world.PhysicsInfo.Properties[mountHandle] = new Physics.PhysicsProperties(mountFilter);
            world.PhysicsInfo.Properties[doorHandle] = new Physics.PhysicsProperties(doorFilter);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            world.PhysicsInfo.Simulation.Awakener.AwakenBody(mountHandle);
            world.PhysicsInfo.Simulation.Awakener.AwakenBody(doorHandle);

            Position = world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Position;
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
                mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2);

            var orientation = world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Orientation;

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(DrawHelper.WhitePixel,
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                Matrix.CreateTranslation(-new Vector3(0, Cube.CUBE_SCALE, 0)) *
                Matrix.CreateFromQuaternion(new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W)) *
                Matrix.CreateTranslation(Position)));

        }
    }
}
