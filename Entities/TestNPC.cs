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
    [EntityMeta(0)]
    [EntitySerializable(EntitySerializableAttribute.SerializationType.World)]
    public class TestNPC : Entity
    {
        private (VertexBuffer VBO, IndexBuffer IBO) mesh;

        private TypedIndex physicsShapeIndex;
        private BodyHandle physicsHandle;

        public TestNPC()
        {
        }

        public TestNPC(Vector3 position)
        {
            this.Position = position + new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE / 2f);
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            var shape = new Sphere(Cube.CUBE_SCALE / 2f);
            physicsShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(shape);
            physicsHandle = world.PhysicsInfo.Simulation.Bodies.Add(
                BodyDescription.CreateDynamic(new RigidPose(Position.ToNumerics()), 
                new BodyInertia() { InverseMass = 1f / 20f }, physicsShapeIndex, 0.001f));
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

            if (!world.ChunkLoadManager.IsLoaded(ChunkPosition.WorldSpaceChunk(Position)) ||
                !world.ChunkManager.CollisionMesher.IsMeshed(ChunkPosition.WorldSpaceChunk(Position)))
            {
                //don't update position - freeze in place
                world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position = Position.ToNumerics();
                return;
            }
            else world.PhysicsInfo.Simulation.Awakener.AwakenBody(physicsHandle);

            Position = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position;

            if (world.ChunkManager.ThreadedView.GetCube(CubePosition.FromWorldSpace(Position))
                .GetOrDefault(Main.Registry.CubeRegistry.Air) == Main.Registry.CubeRegistry.Get("water"))
            {
                Vector3 velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear;
                velocity -= new Vector3(0, Physics.PhysicsInfo.SIM_GRAVITY * 1.01f, 0);

                if (velocity.Y > -Physics.PhysicsInfo.SIM_GRAVITY * 1.01f)
                    velocity.Y = -Physics.PhysicsInfo.SIM_GRAVITY * 1.01f;

                world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = velocity.ToNumerics();
            }
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
                mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2);

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(DrawHelper.WhitePixel,
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(Position - new Vector3(0, Cube.CUBE_SCALE / 2f, 0))));
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveVector3(saveBytes, Position + new Vector3(0, Cube.CUBE_SCALE * 4, 0));
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int index = 0;
            Position = SaveHelper.LoadVector3(loadBytes, ref index);
        }
    }
}
