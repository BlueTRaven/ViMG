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
    public class SlimeBig : Entity, IHasStats
    {
		private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("slime");

		private BuffManager buffManager;
        public NoticeHandler<Player> noticeHandler;
		public  AISlime ai;

		private int maxHealth = 16;

		private float alive;
		private Color tintColor;

        public SlimeBig()
        {
            
        }

		public SlimeBig(Vector3 position)
        {
			this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			buffManager = new BuffManager(this);
			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);

			ai = new AISlime(new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.75f, 0, -Cube.CUBE_SCALE * 0.75f),
				new Vector3(Cube.CUBE_SCALE * 1.5f)), noticeHandler, buffManager, maxHealth);
		}

		public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			alive += (float)deltaTime;

            AISlime.Funcs<SlimeBig> funcs = new AISlime.Funcs<SlimeBig> { ai = ai, entity = this };
            funcs.Update(deltaTime);
        }
		
		public override void OnDelete()
		{
			base.OnDelete();

			EntityItem ent = new EntityItem(Position,
				new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
					Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)),
				new Items.ItemInstance(Main.Registry.ItemRegistry.Get("slime_chunk"), Main.random.Next(2, 8), 1));
			world.EntityManager.Add(ent);
		}

		public override void OnUnload()
		{
			base.OnUnload();

            AISlime.Funcs<SlimeBig> funcs = new AISlime.Funcs<SlimeBig> { ai = ai, entity = this };
            funcs.OnUnload();
        }

		//public override void Draw(GraphicsDevice device, Effect effect)
		//{
		//	base.Draw(device, effect);

		//	if (mesh.IBO == null)
  //              mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2, Enums.Alignment.Bottom);
  //          //mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2);

		//	int ysrc = 32;

		//	const float minInterval = 0.65f;
		//	const float maxInterval = 0.85f;

		//	float interval = MathHelper.Lerp(minInterval, maxInterval, ai.JumpTimer / ai.JumpTime) * 2;

		//	if (ai.OnGround && (alive % interval) / interval < 0.5f)
		//		ysrc = 64;

		//	Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
		//		Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
		//		Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
		//		Matrix.CreateTranslation(Position),
		//		noticeHandler.Noticed ? new RectangleF(32, ysrc, 32, 32) : new RectangleF(0, ysrc, 32, 32)));

		//	if (ai.Health < maxHealth)
		//		DrawHelper3D.DrawHealthbar(device, ai.Health, ai.MaxHealth, Position);
		//}

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
