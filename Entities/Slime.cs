using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG.Entities
{
	public class Slime : Entity, IHasStats
	{
		private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

		private int maxHealth = 4;
		private Color tintColor = Color.White;

		private float alive;

		private NoticeHandler<Player> noticeHandler;
		private BuffManager buffManager;
		private AISlime<Slime> ai;

		public Slime(Vector3 position)
		{
			this.Position = position;
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);
			buffManager = new BuffManager(this);

			ai = new AISlime<Slime>(this, new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.35f, 0, -Cube.CUBE_SCALE * 0.35f),
				new Vector3(Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, maxHealth);
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			alive += (float)deltaTime;

			ai.Update(deltaTime);

			ai.ShouldJumpAwayFromPlayer = world.IsNight();

			//Kill self if too far away
			if ((world.player.Position - Position).Length() > 128 * Cube.CUBE_SCALE)
				world.EntityManager.Remove(this);
		}

        public override void OnDelete()
        {
            base.OnDelete();

			EntityItem ent = new EntityItem(Position, new Items.ItemInstance(Main.Registry.ItemRegistry.Get("slime_chunk"), 1, 1));
			ent.Velocity = new Vector3(Main.random.NextFloat(-5 * Cube.CUBE_SCALE, 5 * Cube.CUBE_SCALE),
				6.4f * Cube.CUBE_SCALE, Main.random.NextFloat(-5 * Cube.CUBE_SCALE, 5 * Cube.CUBE_SCALE));
			world.EntityManager.Add(ent);
		}

        public override void OnUnload()
		{
			base.OnUnload();

			ai.OnUnload();
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			if (mesh.VBO == null)
				mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

			int ysrc = 0;

			const float minInterval = 0.65f;
			const float maxInterval = 0.85f;

			float interval = MathHelper.Lerp(minInterval, maxInterval, ai.JumpTimer / ai.JumpTime) * 2;
			
			if (ai.OnGround && (alive % interval) / interval < 0.5f)
				ysrc = 16;

			Color tintColor = this.tintColor;
			if (ai.InvulnTimer > 0)
				tintColor = Color.Red;

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("slime"), 
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position),
				noticeHandler.Noticed ? new RectangleF(16, ysrc, 16, 16) : new RectangleF(0, ysrc, 16, 16), tintColor.ToVector3()));

			if (ai.Health < maxHealth)
				DrawHelper3D.DrawHealthbar(device, ai.Health, maxHealth, Position);
		}

        public Stats GetStats()
        {
			return new Stats()
			{
				HP = ai.Health,
				MaximumHP = maxHealth,

				TintColor = tintColor,
			};
        }

        public void SetStats(Stats stats)
        {
			ai.Health = stats.HP;
			ai.MaxHealth = stats.MaximumHP;
			tintColor = stats.TintColor;

			if (stats.HP <= 0 || stats.MaximumHP <= 0)
				world.EntityManager.Remove(this);
        }
    }
}
