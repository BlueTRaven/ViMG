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
using ViMG.Rendering;

namespace ViMG.Entities
{
    public class Cultist : Entity, IHasStats
    {
		private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("cultist");

        private NoticeHandler<Player> noticeHandler;
		private BuffManager buffManager;

		private int maxHealth = 140;

		private float alive;

		private AIWalkerShooter<Cultist> ai;

		public Cultist()
        {
        }

		public Cultist(Vector3 position)
        {
			this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(
									HitboxManager.Group.ENEMYHOSTILE_BOTH, 1, 1, Cube.CUBE_SCALE / 4, Cube.CUBE_SCALE, 1, false, 0, true); ;
			ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(new RectangleF(32, 0, 16, 16), Cube.CUBE_SCALE,
				Color.Red.ToVector4(), new Vector2(Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 4));

			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
			buffManager = new BuffManager(this);

			ai = new AIWalkerShooter<Cultist>(world, this, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
				new Vector3(Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, maxHealth, stats, visStats);
			ai.ShootSpeed = Cube.CUBE_SCALE * 4;
		}

        public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			alive += (float)deltaTime;

			ai.Update(deltaTime);
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh.IBO == null)
			{
				float pixelsPerCube = Cube.CUBE_SCALE / 16f;
                mesh = MeshHelper.MakeQuad(device, pixelsPerCube * 19, pixelsPerCube * 19, Enums.Alignment.Bottom);
                //mesh = MeshHelper.MakeEnemyQuad(device, pixelsPerCube * 19, pixelsPerCube * 32);
			}

			RectangleF sourceRect = new RectangleF(0, 0, 19, 32);

			if (ai.GetState() == AIWalkerShooter<Cultist>.State.Normal)
			{
				if (ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
				{
					float animP = (alive % 0.75f) / 0.75f;

					int frame = (int)(animP * 2f);

					sourceRect = new RectangleF(22 + frame * 22, 0, 19, 32);
				}
			}
			else if (ai.GetState() == AIWalkerShooter<Cultist>.State.Attack)
            {
				sourceRect = new RectangleF(65, 0, 19, 32);
			}

			Vector3 tintColor = ai.InvulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

			Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), sourceRect, tintColor));

			DrawHelper3D.DrawHealthbar(device, ai.Health, maxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
		}

        public Stats GetStats()
        {
			return new Stats
			{
				HP = ai.Health,
				MaximumHP = ai.MaxHealth
			};
        }

        public void SetStats(Stats stats)
        {
			ai.Health = stats.HP;
			ai.MaxHealth = stats.MaximumHP;
        }
    }
}
