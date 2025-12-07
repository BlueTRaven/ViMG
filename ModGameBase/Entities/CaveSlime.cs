using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using ViMG.Buffs;
using ViMG.Rendering;
using Engine.Networking;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.Server)]
    [EntityMeta(0)]
    public class CaveSlime : Entity, IHasStats, ISyncBasicState
    {
		private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("slime");
        private int maxHealth = 12;
		private Color tintColor = Color.White;

		public float alive;

		private BuffManager buffManager;
		public NoticeHandler<Player> noticeHandler;
		public AISlime ai;

		public CaveSlime() : this(Vector3.Zero) { }

		public CaveSlime(Vector3 position)
		{
			this.Position = position;

            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);
            buffManager = new BuffManager(this);

            ai = new AISlime(new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.35f, 0, -Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, maxHealth);
        }

		public override void Initialize(World world)
		{
			base.Initialize(world);
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			alive += (float)deltaTime;

			AISlime.Funcs<CaveSlime> funcs = new AISlime.Funcs<CaveSlime> { ai = ai, entity = this };
			funcs.Update(deltaTime);

            //Kill self if too far away
            if (world.player.All(x => x == null || (x.Position - Position).Length() > 128 * Cube.CUBE_SCALE))
                world.EntityManager.Remove(this);
		}

		public override void OnDelete()
		{
			base.OnDelete();

			EntityItem ent = new EntityItem(Position,
				new Vector3(Main.random.NextFloat(-5 * Cube.CUBE_SCALE, 5 * Cube.CUBE_SCALE),
					6.4f * Cube.CUBE_SCALE, Main.random.NextFloat(-5 * Cube.CUBE_SCALE, 5 * Cube.CUBE_SCALE)),
				new Items.ItemInstance(Main.Registry.ItemRegistry.Get("slime_chunk"), 1, 1));
			world.EntityManager.Add(ent);
		}

		public override void OnUnload()
		{
			base.OnUnload();

            AISlime.Funcs<CaveSlime> funcs = new AISlime.Funcs<CaveSlime> { ai = ai, entity = this };
            funcs.OnUnload();
		}

		//public override void Draw(GraphicsDevice device, Effect effect)
		//{
		//	if (mesh.IBO == null)
		//		mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE, Enums.Alignment.Bottom);// MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

		//	int ysrc = 0;

		//	const float minInterval = 0.65f;
		//	const float maxInterval = 0.85f;

		//	float interval = MathHelper.Lerp(minInterval, maxInterval, ai.JumpTimer / ai.JumpTime) * 2;

		//	if (ai.OnGround && (alive % interval) / interval < 0.5f)
		//		ysrc = 16;

		//	Color tintColor = this.tintColor;
		//	if (ai.InvulnTimer > 0)
		//		tintColor = Color.Red;

		//	Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
		//		Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
		//		Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
		//		Matrix.CreateTranslation(Position),
		//		noticeHandler.Noticed ? new RectangleF(48, ysrc, 16, 16) : new RectangleF(32, ysrc, 16, 16), tintColor.ToVector3()));

		//	if (ai.Health < maxHealth)
		//		DrawHelper3D.DrawHealthbar(device, ai.Health, maxHealth, Position);
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

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            Get(out var state);
            state.OnSave(saveBytes);

			ai?.OnSave(saveBytes);
			SaveHelper.SaveInt32(saveBytes, maxHealth);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int index = 0;
            var bs = new BasicState();
            bs.OnLoad(loadBytes, ref index);
            Set(ref bs);

			ai?.OnLoad(loadBytes, ref index);
            maxHealth = SaveHelper.LoadInt32(loadBytes, ref index);
        }

        public void Get(out BasicState state)
        {
            BasicState aiState = new BasicState();
            ai?.Get(out aiState);
            aiState.position = Position;
            aiState.rotation = Quaternion.Identity;
            state = aiState;
        }

        public void Set(ref readonly BasicState state)
        {
            Position = state.position;

            ai?.Set(in state);
        }
    }
}
