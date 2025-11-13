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
using ViMG.Rendering;

namespace ViMG.Entities
{
    public class CaveSalamander : Entity
    {
        private static VerySimpleMesh mesh;
		private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("salamander");

		public Vector3 MaxVelocity = new Vector3(1.6f * Cube.CUBE_SCALE);
		public Vector3 MaxVelocityFalling = new Vector3(1.6f * Cube.CUBE_SCALE, 17 * Cube.CUBE_SCALE, 1.6f * Cube.CUBE_SCALE);
		//public Vector3 Velocity;
		private Vector3 moveDir;

		private float alive;
		private float invulnTimer;

		private int hitbox;
		private NoticeHandler<Player> noticeHandler;

		//private Vector3 gravityDir = new Vector3(0, 1, 0);

		private TypedIndex physicsShapeIndex;
		private BodyHandle physicsHandle;
		private Physics.ContactChecker contactChecker;

		private Vector3 target = Vector3.Zero;

		private float wanderTimer;

		public CaveSalamander()
		{
		}

		public CaveSalamander(Vector3 position)
        {
			this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 12, false);
            contactChecker = new Physics.ContactChecker();

            target = Position + new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f),
                Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f),
                Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f));

            var physicsShape = new Sphere(Cube.CUBE_SCALE / 2f);
			physicsShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(physicsShape);
			physicsHandle = world.PhysicsInfo.Simulation.Bodies.Add(BodyDescription.CreateDynamic(
				new RigidPose(Position.ToNumerics()), new BodyInertia() { InverseMass = 1f }, physicsShapeIndex, 0.001f));

			world.PhysicsInfo.Properties[physicsHandle] = new Physics.PhysicsProperties(new Physics.SubgroupCollisionFilter(Physics.FilterGroups.GROUP_ENEMY), 0);
		}

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			alive += (float)deltaTime;

			contactChecker.Update(world, physicsHandle);

			Vector3 actualMaxVel = MaxVelocity;

			Vector3 velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear;

			wanderTimer -= (float)deltaTime;
			invulnTimer -= (float)deltaTime;
			noticeHandler.Update(deltaTime);

			if (invulnTimer <= 0)
			{
				if (!noticeHandler.Noticed)
				{
					if (wanderTimer <= 0)
					{
						target = Position + new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f),
							Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f),
							Main.random.NextFloat(-Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 4f));

						wanderTimer = Main.random.NextFloat(0.65f, 2.5f);
					}
				}
				// TODO MULTIPLAYER REFACTOR
				else target = world.player[0].Position;

				if (contactChecker.OnGround)
				{
					Vector3 distance = (target - new Vector3(0, Cube.CUBE_SCALE, 0)) - Position;

					Vector3 dir = Vector3.Normalize(distance) * Cube.CUBE_SCALE;

					float stopDist = noticeHandler.Noticed ? Cube.CUBE_SCALE * 1.5f : Cube.CUBE_SCALE * 3.5f;

					if (distance.Length() > stopDist)
					{
						Vector3 tangent = Vector3.Normalize(Vector3.Cross(-contactChecker.Normal, dir));

						Matrix mat = Matrix.CreateFromAxisAngle(-contactChecker.Normal, MathHelper.ToRadians(-90));

						moveDir = Vector3.Transform(tangent, mat);
						velocity += moveDir * Cube.CUBE_SCALE;// * (Cube.CUBE_SCALE / 4f);
					}
                    else
                    {
						velocity *= 0.95f;
                    }

                    if (velocity.Length() > actualMaxVel.Length())
                    {
                        velocity.Normalize();
                        velocity *= actualMaxVel.Length();
                    }
                }
                else
                {
					//add gravity
					//this will double gravity, but we remove gravity later
					velocity.Y += (Physics.PhysicsInfo.SIM_GRAVITY * (float)deltaTime);

					Vector2 clampXY = new Vector2(actualMaxVel.X, actualMaxVel.Z);
					Vector2 velXY = new Vector2(velocity.X, velocity.Z);

					if (velXY.Length() > clampXY.Length())
					{
						velXY.Normalize();
						velXY *= clampXY.Length();
					}

					velocity = new Vector3(velXY.X, velocity.Y, velXY.Y);
				}

				if (contactChecker.Touching)
                {
					//try to force to the ground
					velocity += -contactChecker.Normal * Cube.CUBE_SCALE * 8;
                }
			}

			//negate gravity
			velocity.Y -= (Physics.PhysicsInfo.SIM_GRAVITY * (float)deltaTime);

			world.PhysicsInfo.Simulation.Awakener.AwakenBody(physicsHandle);
			world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = velocity.ToNumerics();
			Position = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position;
			//Position += Velocity * (float)deltaTime;
		}

		//public override void Draw(GraphicsDevice device, Effect effect)
  //      {
  //          base.Draw(device, effect);

		//	if (mesh.IBO == null)
		//		mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE, Enums.Alignment.Bottom);
  //              //mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

  //          Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
  //              Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
  //              Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
  //              Matrix.CreateTranslation(Position), new RectangleF(0, 0, 16, 16)));

  //          DrawHelper3D.DrawHealthbar(device, 4, 4, Position);
  //      }
    }
}
