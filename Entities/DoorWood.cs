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
    public class DoorWood : Entity, ICubeTracker
    {
        private static (VertexBuffer VBO, IndexBuffer IBO) mountMesh;
        private static (VertexBuffer VBO, IndexBuffer IBO) doorMesh;

        private TypedIndex mountShapeIndex;
        private TypedIndex doorShapeIndex;
        private BodyHandle mountHandle;
        private BodyHandle doorHandle;

        private ConstraintHandle hingeHandle;
        private readonly MeshHelper.CubeFace facing;

        public CubePosition TrackedPosition { get; set; }

        public DoorWood()
        {
        }

        public DoorWood(CubePosition position, MeshHelper.CubeFace facing)
        {
            if (facing == MeshHelper.CubeFace.UP || facing == MeshHelper.CubeFace.DOWN)
            {
                Console.WriteLine("Can't facce up or down! Defaulting to LEFT");
                facing = MeshHelper.CubeFace.LEFT;
            }

            this.TrackedPosition = position;
            this.Position = position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f);
            this.facing = facing;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            float rotation = 0;
            switch (facing)
            {
                case MeshHelper.CubeFace.LEFT:
                    rotation = 270;
                    break;
                case MeshHelper.CubeFace.FRONT:
                    rotation = 180;
                    break;
                case MeshHelper.CubeFace.RIGHT:
                    rotation = 90;
                    break;
                case MeshHelper.CubeFace.BACK:
                    rotation = 0;
                    break;
                default:
                    break;
            }

            Matrix rot = Matrix.CreateRotationY(MathHelper.ToRadians(rotation));
            Vector3 offsetMount = new Vector3(Cube.CUBE_SCALE * 0.05f, 0, 0);
            Vector3 offsetDoor = new Vector3(-Cube.CUBE_SCALE / 2f, 0, 0);
            Vector3 mountPos = Vector3.Transform(new Vector3(-Cube.CUBE_SCALE * 0.6f, 0, 0), rot);

            var shapeMount = new Box(Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f);
            var shapeDoor = new Box(Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 0.25f);
            var hinge = new Hinge()
            {
                LocalOffsetA = offsetMount.ToNumerics(),
                LocalOffsetB = offsetDoor.ToNumerics(),
                LocalHingeAxisA = Vector3.Up.ToNumerics(),  
                LocalHingeAxisB = Vector3.Up.ToNumerics(), 
                SpringSettings = new SpringSettings(30, 1) 
            };

            mountShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(shapeMount);
            doorShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(shapeDoor);
            mountHandle = world.PhysicsInfo.Simulation.Bodies.Add(BodyDescription.CreateKinematic(new RigidPose((Position + mountPos).ToNumerics(), 
                System.Numerics.Quaternion.CreateFromRotationMatrix(rot.ToNumerics())), mountShapeIndex, 0.001f));
            doorHandle = world.PhysicsInfo.Simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(Position.ToNumerics(), 
                System.Numerics.Quaternion.CreateFromRotationMatrix(rot.ToNumerics())), shapeDoor.ComputeInertia(1), doorShapeIndex, 0.001f));
         
            hingeHandle = world.PhysicsInfo.Simulation.Solver.Add(mountHandle, doorHandle, hinge);

            var mountFilter = new Physics.SubgroupCollisionFilter(Physics.FilterGroups.GROUP_PLAYER, 1);
            var doorFilter = new Physics.SubgroupCollisionFilter(Physics.FilterGroups.GROUP_PLAYER, 2);
            mountFilter.DisableCollision(2);
            doorFilter.DisableCollision(1);
            world.PhysicsInfo.Properties[mountHandle] = new Physics.PhysicsProperties(mountFilter);
            world.PhysicsInfo.Properties[doorHandle] = new Physics.PhysicsProperties(doorFilter);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            world.PhysicsInfo.Simulation.Awakener.AwakenBody(mountHandle);
            world.PhysicsInfo.Simulation.Awakener.AwakenBody(doorHandle);

            //Disallow movement
            //world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Position = Position.ToNumerics();
            Position = world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Position;
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (doorMesh.VBO == null)
            {
                mountMesh = MeshHelper.MakeCenteredQuad(device, Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f);
                doorMesh = MeshHelper.MakeCenteredQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2);
            }

            var position = world.PhysicsInfo.Simulation.Bodies[mountHandle].Pose.Position;
            var orientation = world.PhysicsInfo.Simulation.Bodies[mountHandle].Pose.Orientation;

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(DrawHelper.WhitePixel,
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, mountMesh.VBO, mountMesh.IBO,
                Matrix.CreateFromQuaternion(new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W)) *
                Matrix.CreateTranslation(position)));

            position = world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Position;
            orientation = world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Orientation;

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(DrawHelper.WhitePixel,
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, doorMesh.VBO, doorMesh.IBO,
                Matrix.CreateFromQuaternion(new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W)) *
                Matrix.CreateTranslation(position)));

        }

        public bool OnInteract(Player player)
        {
            throw new NotImplementedException();
        }

        public void TrackingCubeUpdated(World world, ChunkManager cm, ushort updatedId)
        {
            throw new NotImplementedException();
        }
    }
}
