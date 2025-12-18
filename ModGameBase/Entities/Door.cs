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
using ViMG.Rendering;

namespace ViMG.Entities
{
    [EntityMeta(0)]
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
    public class Door : Entity, IMultiCubeTracker
    {
        private static VerySimpleMesh mountMesh;
        private static VerySimpleMesh doorMesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("cubes_textures");

        private TypedIndex mountShapeIndex;
        private TypedIndex doorShapeIndex;
        public BodyHandle mountHandle;
        public BodyHandle doorHandle;

        private ConstraintHandle hingeHandle;
        private MeshHelper.CubeFace facing;

        private bool closing;

        public IEnumerable<CubePosition> TrackedPositions { get; set; }

        public Door()
        {
        }

        public Door((CubePosition bottom, CubePosition top) positions, MeshHelper.CubeFace facing)
        {
            if (facing == MeshHelper.CubeFace.UP || facing == MeshHelper.CubeFace.DOWN)
            {
                Console.WriteLine("Can't facce up or down! Defaulting to LEFT");
                facing = MeshHelper.CubeFace.LEFT;
            }

            this.TrackedPositions = new CubePosition[] { positions.bottom, positions.top };

            this.Position = positions.bottom.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f);
            this.facing = facing;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            float rotation = GetFacingRotation();

            Matrix rot = Matrix.CreateRotationY(MathHelper.ToRadians(rotation));
            Vector3 dir = Vector3.Transform(new Vector3(0, 0, 1), rot);
            Vector3 offsetMount = new Vector3(Cube.CUBE_SCALE * 0.05f, 0, 0);
            Vector3 offsetDoor = new Vector3(-Cube.CUBE_SCALE * 0.55f, 0, 0);
            Vector3 mountPos = Vector3.Transform(new Vector3(-Cube.CUBE_SCALE * 0.6f, 0, 0), rot);

            var shapeMount = new Box(Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f);
            var shapeDoor = new Box(Cube.CUBE_SCALE * 0.92f, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 0.125f);
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

            HousingValidity validFront = world.HousingManager.DetermineIfValidHousing(world, TrackedPositions.ElementAt(0) + 
                new CubePosition((int)dir.X, (int)dir.Y, (int)dir.Z), out Housing housingFront);
            HousingValidity validBack = world.HousingManager.DetermineIfValidHousing(world, TrackedPositions.ElementAt(0) - 
                new CubePosition((int)dir.X, (int)dir.Y, (int)dir.Z), out Housing housingBack);

            if (validFront == HousingValidity.Valid)
                world.HousingManager.AddHousing(ref world.WorldInfo, ref housingFront);
            if (validBack == HousingValidity.Valid)
                world.HousingManager.AddHousing(ref world.WorldInfo, ref housingBack);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            world.PhysicsInfo.Simulation.Shapes.Remove(mountShapeIndex);
            world.PhysicsInfo.Simulation.Shapes.Remove(doorShapeIndex);
            world.PhysicsInfo.Simulation.Bodies.Remove(mountHandle);
            world.PhysicsInfo.Simulation.Bodies.Remove(doorHandle);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            world.PhysicsInfo.Simulation.Awakener.AwakenBody(mountHandle);
            world.PhysicsInfo.Simulation.Awakener.AwakenBody(doorHandle);

            Position = world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Position;

            if (closing)
            {
                Matrix rot = Matrix.CreateRotationY(MathHelper.ToRadians(GetFacingRotation()));

                var orientation = System.Numerics.Quaternion.CreateFromRotationMatrix(rot.ToNumerics());

                world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Orientation = orientation;

                closing = false;
            }
        }

        private float GetFacingRotation()
        {
            float rotation = 0;
            switch (facing)
            {
                case MeshHelper.CubeFace.LEFT:
                    rotation = 90;
                    break;
                case MeshHelper.CubeFace.FRONT:
                    rotation = 180;
                    break;
                case MeshHelper.CubeFace.RIGHT:
                    rotation = 270;
                    break;
                case MeshHelper.CubeFace.BACK:
                    rotation = 0;
                    break;
                default:
                    break;
            }

            return rotation;
        }

        //public override void Draw(GraphicsDevice device, Effect effect)
        //{
        //    base.Draw(device, effect);

        //    if (doorMesh.IBO == null)
        //    {
        //        mountMesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f, Enums.Alignment.Center);
        //        doorMesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2f, Enums.Alignment.Center);
        //        //mountMesh = MeshHelper.MakeCenteredQuad(device, Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f);
        //        //doorMesh = MeshHelper.MakeCenteredQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2);
        //    }

        //    var position = world.PhysicsInfo.Simulation.Bodies[mountHandle].Pose.Position;
        //    var orientation = world.PhysicsInfo.Simulation.Bodies[mountHandle].Pose.Orientation;

        //    Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mountMesh,
        //        Matrix.CreateFromQuaternion(new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W)) *
        //        Matrix.CreateTranslation(position)));

        //    position = world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Position;
        //    orientation = world.PhysicsInfo.Simulation.Bodies[doorHandle].Pose.Orientation;

        //    Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, doorMesh,
        //        Matrix.CreateFromQuaternion(new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W)) *
        //        Matrix.CreateTranslation(position), sourceRect: new RectangleF(0, 128, 16, 32)));

        //}

        public bool OnInteract(Player player)
        {
            closing = true;
            //TODO try to close door
            return true;
        }

        public void TrackingCubeUpdated(World world, ChunkManager cm, Player? player, CubePosition position, ushort updatedId, double timeUpdated)
        {
            if (timeUpdated > TimeInitialized)
            {
                for (int i = 0; i < TrackedPositions.Count(); i++)
                {
                    cm.CubeView.SetCube(TrackedPositions.ElementAt(i), 0);
                }

                world.EntityManager.Kill(this);
            }
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveCubePosition(saveBytes, TrackedPositions.ElementAt(0));
            SaveHelper.SaveCubePosition(saveBytes, TrackedPositions.ElementAt(1));

            SaveHelper.SaveInt32(saveBytes, (int)facing);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int offset = 0;

            TrackedPositions = new CubePosition[]
            {
                SaveHelper.LoadCubePosition(loadBytes, ref offset),
                SaveHelper.LoadCubePosition(loadBytes, ref offset)
            };

            facing = (MeshHelper.CubeFace)SaveHelper.LoadInt32(loadBytes, ref offset);

            Position = TrackedPositions.ElementAt(0).InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f);
        }
    }
}
