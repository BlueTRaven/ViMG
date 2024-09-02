using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
    public class SnakeFlying : Entity, IHasStats
    {
		private static VerySimpleMesh mesh2x2;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("snake");

        public Vector3 Velocity;

		public int MaxHealth = 10;

		private float alive;

		public AIFlierMelee<SnakeFlying> aiFlying;

		private BuffManager buffManager;
		private NoticeHandler<Player> noticeHandler;

		public SnakeFlying()
        {
        }

        public SnakeFlying(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
			buffManager = new BuffManager(this);

			aiFlying = new AIFlierMelee<SnakeFlying>(world, this, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
				new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.7f)),
				new Rectangle3D(-new Vector3(Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f)),
				noticeHandler, buffManager, MaxHealth);
		}

		public override void OnUnload()
		{
			base.OnUnload();

			aiFlying.OnUnload();
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			alive += (float)deltaTime;

			aiFlying.Update(deltaTime);

			if (aiFlying.Health <= aiFlying.MaxHealth / 2f)
            {
				world.EntityManager.Remove(this);
				world.EntityManager.Add(new Snake(this, buffManager, noticeHandler));
            }
		}

		//public override void Draw(GraphicsDevice device, Effect effect)
		//{
		//	base.Draw(device, effect);

		//	if (mesh2x2.IBO == null)
		//	{
		//		mesh2x2 = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2, Enums.Alignment.Bottom);
		//		//mesh2x2 = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2);
		//	}

		//	RectangleF sourceRectSnake = new RectangleF(0, 34, 32, 32);

		//	if (aiFlying.GetState() == AIFlierMelee<SnakeFlying>.State.Attack)
  //          {
		//		const int ATT_NUM_FRAMES = 4;
		//		int frame = (int)((1 - (aiFlying.AttackTimer / aiFlying.AttackLockTime)) * ATT_NUM_FRAMES);
		//		sourceRectSnake = new RectangleF(32 * frame, 34, 32, 32);
		//	}

		//	RectangleF sourceRectWings = new RectangleF(0, 64, 32, 32);

		//	const int WINGS_NUM_FRAMES = 3;
		//	int wingFrame = (int)((1 - ((alive % 0.25f) / 0.25f)) * WINGS_NUM_FRAMES);
		//	sourceRectWings.x = 32 * wingFrame;

		//	Vector3 tintColor = aiFlying.InvulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

		//	Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh2x2,
		//		Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
		//		Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
		//		Matrix.CreateTranslation(Position), sourceRectWings, tintColor));

		//	Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh2x2,
		//		Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
		//		Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
		//		Matrix.CreateTranslation(Position), sourceRectSnake, tintColor));

		//	DrawHelper3D.DrawHealthbar(device, aiFlying.Health, aiFlying.MaxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
		//}

        public Stats GetStats()
        {
			return new Stats()
			{
				HP = aiFlying.Health,
				MaximumHP = aiFlying.MaxHealth,
				AttackSpeed = aiFlying.AttackCooldownTime
			};
        }

        public void SetStats(Stats stats)
        {
			aiFlying.Health = stats.HP;
        }
    }
}
