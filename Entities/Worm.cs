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
    public class Worm : Entity
    {
		//private static VerySimpleMesh mesh;
  //      private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("worm");

        public int health;
		public int maxHealth = 8;

        public Vector3 Velocity;

        private Vector3 maxVelFalling = new Vector3(Cube.CUBE_SCALE * 8, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 8);
        private Vector3 maxVelTunneling = new Vector3(Cube.CUBE_SCALE * 8);

        private float TRAIN_RADIUS = Cube.CUBE_SCALE / 2f;
        public Vector3[] trainPositions = new Vector3[8];

		public Worm()
        {
        }

        public Worm(Vector3 position)
        {
            this.Position = position;

            Array.Fill(trainPositions, Position);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            Cube c = world.ChunkManager.ThreadedView.GetCube(CubePosition.FromWorldSpace(Position)).GetOrDefault(Main.Registry.CubeRegistry.Air);

            Rectangle3D cubeBounds;

            if (c == null || !c.Solid)
                cubeBounds = new Rectangle3D();
            else cubeBounds = CubePosition.BoundsWorldSpace(CubePosition.FromWorldSpace(Position));

            if (cubeBounds.Contains(Position))
            {
                Vector3 playerDir = world.player.Position - Position;
                playerDir.Normalize();

                if (Velocity.Length() > 0)
                {
                    Vector3 velocityDir = Vector3.Normalize(Velocity);
                    float velocityLen = Velocity.Length();

                    Vector3 cross = Vector3.Cross(velocityDir, playerDir);
                    Matrix mat = Matrix.CreateFromAxisAngle(cross, MathHelper.ToRadians(5));

                    Vector3 rotated = Vector3.Normalize(Vector3.Transform(velocityDir, mat));
                    Velocity = rotated * (velocityLen + Cube.CUBE_SCALE);
                }
                else
                {
                    //Initial first acceleration (we start with 0 velocity, and that results in NaNs, so we kinda have to seed it)
                    Velocity += playerDir * Cube.CUBE_SCALE;
                }
                
                Vector3 realMaxVel = maxVelTunneling;

                if (Velocity.Length() > realMaxVel.Length())
                    Velocity = Vector3.Normalize(Velocity) * realMaxVel.Length();
            }
            else
            {
                Velocity.X *= 0.95f;
                Velocity.Z *= 0.95f;
                Velocity.Y += World.GRAVITY;

                if (Velocity.Length() > maxVelFalling.Length())
                    Velocity = Vector3.Normalize(Velocity) * maxVelFalling.Length();
            }

            for (int i = 0; i < trainPositions.Length; i++)
            {
                ref Vector3 trainPos = ref trainPositions[i];
                Vector3 lastPosition = i == 0 ? Position : trainPositions[i - 1];

                Vector3 dist = trainPos - lastPosition;

                if (dist.Length() > TRAIN_RADIUS)
                {
                    trainPos = lastPosition + Vector3.Normalize(dist) * TRAIN_RADIUS;
                }
            }

            Position += Velocity * (float)deltaTime;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			health = maxHealth;
        }

  //      public override void Draw(GraphicsDevice device, Effect effect)
		//{
		//	if (mesh.IBO == null)
  //              mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE, Enums.Alignment.Bottom);
  //          //mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

  //          Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
		//		Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
		//		Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
		//		Matrix.CreateTranslation(Position), new RectangleF(0, 0, 16, 16)));

  //          for (int i = 0; i < trainPositions.Length; i++)
  //          {
  //              Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
  //                  Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
  //                  Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
  //                  Matrix.CreateTranslation(trainPositions[i]), new RectangleF(16, 0, 16, 16)));
  //          }

  //          DrawHelper3D.DrawHealthbar(device, health, maxHealth, Position);
		//}
	}
}
