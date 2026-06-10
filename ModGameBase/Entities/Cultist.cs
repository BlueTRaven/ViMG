using BrUtility;
using Engine;
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
    public class Cultist : Entity, IHasStats, ISyncedEntity
    {
        private const int MaxHealth = 20;
        private NoticeHandler<Player> noticeHandler;
		private BuffManager buffManager;

		public AIWalkerShooter ai;

		public Cultist() : this(Vector3.Zero)
        {
        }

		public Cultist(Vector3 position)
        {
			this.Position = position;

            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
            buffManager = new BuffManager(this);
            
            ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(
                                      HitboxManager.Group.ENEMYHOSTILE_BOTH, 1, 1, Cube.CUBE_SCALE / 4, Cube.CUBE_SCALE, 1, false, 0, true);

            ai = new AIWalkerShooter(new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, MaxHealth, stats, GlobalState.Registry.ProjectileRegistry.Get("cultist_ball").Id);
            ai.ShootSpeed = Cube.CUBE_SCALE * 4;
        }

        public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			AIWalkerShooter.Funcs<Cultist> funcs = new() { world = world, ai = ai, entity = this };
			funcs.Update(deltaTime);
		}

        public override void OnUnload()
        {
            base.OnUnload();

            AIWalkerShooter.Funcs<Cultist> funcs = new() { world = world, ai = ai, entity = this };
            funcs.OnUnload();
        }

        public Stats GetStats()
        {
			return new Stats
			{
				HP = ai.Health,
				MaximumHP = MaxHealth,
			};
        }

        public void SetStats(Stats stats)
        {
			ai.Health = stats.HP;
			ai.MaxHealth = stats.MaximumHP;
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            GetSyncedEntity(out var state);
            state.OnSave(saveBytes);

            ai?.OnSave(saveBytes);
        }

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
            var bs = new SyncedEntity();
            bs.OnLoad(loadBytes, ref index);
            Position = bs.position;
            ai.Velocity = bs.velocity;
            ai.Health = bs.health;

            ai?.OnLoad(loadBytes, ref index);
        }

        public void GetSyncedEntity(out SyncedEntity state)
        {
            SyncedEntity aiState = new SyncedEntity();
            ai?.Get(out aiState);
            aiState.position = Position;
            state = aiState;
        }
    }
}
