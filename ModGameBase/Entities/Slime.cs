using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Rendering;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.Server)]
	[EntityMeta(0)]
    public class Slime : Entity, IHasStats, ISyncBasicState
	{
		private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("slime");

        private int maxHealth = 4;
		private Color tintColor = Color.White;

		public float alive;

		private BuffManager buffManager;
		public NoticeHandler<Player> noticeHandler;
		public AISlime? ai;

		public Slime()
		{
		}

		public Slime(Vector3 position)
		{
			this.Position = position; 
        }

        public override void Initialize(World world)
		{
			base.Initialize(world);

			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);
			buffManager = new BuffManager(this);

			ai = new AISlime(new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.35f, 0, -Cube.CUBE_SCALE * 0.35f),
				new Vector3(Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, maxHealth);
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			alive += (float)deltaTime;

            AISlime.Funcs<Slime> funcs = new AISlime.Funcs<Slime> { ai = ai, entity = this };
            funcs.Update(deltaTime);

			ai.ShouldJumpAwayFromPlayer = world.IsNight();

            //Kill self if too far away
            if (world.player.All(x => x == null || (x.Position - Position).Length() > 128 * Cube.CUBE_SCALE))
                world.EntityManager.Remove(this);
		}

        public override void OnDelete()
        {
            base.OnDelete();

			EntityItem ent = new EntityItem(Position,
				new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
					Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)),
				new Items.ItemInstance(Main.Registry.ItemRegistry.Get("slime_chunk"), 1, 1));
			world.EntityManager.Add(ent);
		}

        public override void OnUnload()
		{
			base.OnUnload();

            AISlime.Funcs<Slime> funcs = new AISlime.Funcs<Slime> { ai = ai, entity = this };
            funcs.OnUnload();
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
			state = new BasicState
			{
				position = Position,
				rotation = Quaternion.Identity,
				velocity = ai?.Velocity ?? Vector3.Zero,
				health = ai?.Health ?? 0,
				state = 0,
				timers = { [0] = ai?.JumpTimer ?? 0 },
			};
        }

        public void Set(ref readonly BasicState state)
        {
			Position = state.position;
			if (ai != null)
			{
				ai.Velocity = state.velocity;
				ai.Health = state.health;
				ai.jumpTimer = state.timers[0];
			}
        }
    }
}
