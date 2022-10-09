using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using ViMG.Buffs;

namespace ViMG.Entities
{
    public class Snake : Entity, IHasStats
    {
		private static (VertexBuffer VBO, IndexBuffer IBO) mesh2x1;
		private static (VertexBuffer VBO, IndexBuffer IBO) mesh1x1;
		private static (VertexBuffer VBO, IndexBuffer IBO) mesh2x2;

		private AIWalkerMelee<Snake> ai;
		private NoticeHandler<Player> noticeHandler;
		private BuffManager buffManager;

		private bool initializeThroughSnakeFlying;

		private float alive;

		public Snake()
        {

        }

		public Snake(Vector3 position)
        {
			this.Position = position;
        }

		public Snake(SnakeFlying snakeFlying, BuffManager buffManager, NoticeHandler<Player> noticeHandler)
        {
			this.Position = snakeFlying.Position;

			this.buffManager = buffManager;
			this.noticeHandler = noticeHandler;

			ai = new AIWalkerMelee<Snake>(this, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
					new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.7f)),
					new Rectangle3D(-new Vector3(Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f)),
					noticeHandler,
					buffManager,
					snakeFlying.GetStats().MaximumHP);

			ai.Health = snakeFlying.GetStats().HP;

			initializeThroughSnakeFlying = true;
        }

        public override void OnUnload()
        {
            base.OnUnload();

			ai.OnUnload();
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			if (!initializeThroughSnakeFlying)
			{
				buffManager = new BuffManager(this);
				noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);

				ai = new AIWalkerMelee<Snake>(this, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
					new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.7f)),
					new Rectangle3D(-new Vector3(Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f)),
					noticeHandler,
					buffManager,
					12);
			}
		}

		public override void Update(double deltaTime)
		{
			alive += (float)deltaTime;

			ai.Update(deltaTime);

			if ((world.player.Position - Position).Length() > 128 * Cube.CUBE_SCALE)
				world.EntityManager.Remove(this);
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh2x1.VBO == null)
			{
				mesh2x1 = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE);
				mesh1x1 = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);
				mesh2x2 = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2);
			}

			(VertexBuffer VBO, IndexBuffer IBO) useMesh = mesh2x1;

			Vector3 velXZ = new Vector3(ai.Velocity.X, 0, ai.Velocity.Z);
			velXZ.Normalize();

			float facingDotCamera = Vector3.Dot(velXZ, -Main.camera.Forward);

			//Facing within 45 degrees of the camera.
			bool isFacingCamera = facingDotCamera < MathHelper.ToRadians(45);

			RectangleF sourceRect = new RectangleF(0, 0, 32, 16);

			if (isFacingCamera)
			{
				sourceRect = new RectangleF(0, 16, 16, 16);
				useMesh = mesh1x1;
			}

			if (ai.GetState() == AIWalkerMelee<Snake>.State.Normal)
			{
				if (ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
				{
					float animP = (alive % 0.75f) / 0.75f;

					int frame = (int)(animP * 2f);

					sourceRect.x += sourceRect.width * frame;
				}
			}
			else if (ai.GetState() == AIWalkerMelee<Snake>.State.Attack)
            {
				useMesh = mesh2x2;
				sourceRect.y = 32;
				sourceRect.width = 32;
				sourceRect.height = 32;
				const int NUM_FRAMES = 4;

				int frame = (int)((1 - (ai.AttackTimer / ai.AttackLockTime)) * NUM_FRAMES);

				sourceRect.x = 32 * frame;
            }

			Vector3 tintColor = ai.InvulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("snake"),
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, useMesh.VBO, useMesh.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), sourceRect, tintColor));

			DrawHelper3D.DrawHealthbar(device, ai.Health, ai.MaxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
		}

        public Stats GetStats()
        {
			return new Stats()
			{
				HP = ai.Health,
				MaximumHP = ai.MaxHealth,
				AttackSpeed = ai.AttackCooldownTime,
			};
        }

        public void SetStats(Stats stats)
        {
			ai.Health = stats.HP;
			ai.MaxHealth = stats.MaximumHP;
        }
    }
}
