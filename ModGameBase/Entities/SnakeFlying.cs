using BrUtility;
using Engine.Networking;
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
    [EntitySerializable(EntitySerializableAttribute.SerializationType.Server)]
    [EntityMeta(0)]
    public class SnakeFlying : Entity, IHasStats, ISyncBasicState
    {
		public int MaxHealth = 10;

		public AIFlierMelee ai;

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

			ai = new AIFlierMelee(world, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
				new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.7f)),
				new Rectangle3D(-new Vector3(Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f)),
				noticeHandler, buffManager, MaxHealth);
		}

		public override void OnUnload()
		{
			base.OnUnload();

            AIFlierMelee.Funcs<SnakeFlying> funcsFlying = new AIFlierMelee.Funcs<SnakeFlying> { ai = ai, entity = this };
            funcsFlying.OnUnload();
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

            AIFlierMelee.Funcs<SnakeFlying> funcsFlying = new AIFlierMelee.Funcs<SnakeFlying> { ai = ai, entity = this };
            funcsFlying.Update(deltaTime);

			if (ai.Health <= ai.MaxHealth / 2f)
            {
				world.EntityManager.Unload(this);
				world.EntityManager.Add(new Snake(this, buffManager, noticeHandler));
            }
		}

        public Stats GetStats()
        {
			return new Stats()
			{
				HP = ai.Health,
				MaximumHP = ai.MaxHealth,
				AttackSpeed = ai.AttackCooldownTime
			};
        }

        public void SetStats(Stats stats)
        {
			ai.Health = stats.HP;
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            Get(out var state);
            state.OnSave(saveBytes);

            ai?.OnSave(saveBytes);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int index = 0;
            var bs = new BasicState();
            bs.OnLoad(loadBytes, ref index);
            Set(ref bs);

            ai?.OnLoad(loadBytes, ref index);
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
