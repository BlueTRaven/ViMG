using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using ViMG.Rendering;
using ViMG.Buffs;
using Engine.Networking;
using Engine.Common;
using Engine;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.Server)]
    [EntityMeta(1)]
    public class Imp : Entity, IHasStats, ISyncedEntity
    {
		private const int MAX_HEALTH = 8;
		private const float FIRE_TIME = 2.25f;		
		private static Vector3 MAX_VELOCITY = new Vector3(3.2f * Cube.CUBE_SCALE, 17 * Cube.CUBE_SCALE, 3.2f * Cube.CUBE_SCALE);

        private ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.ENEMYHOSTILE_BOTH, 
			1, 1f, Cube.CUBE_SCALE / 4, Cube.CUBE_SCALE);
        private NoticeHandler<Player> noticeHandlerDay;
        private NoticeHandler<Player> noticeHandlerNight;
        private BuffManager buffManager;
        private AIWalkerShooter aiDay;
        private AIWalkerShooter aiNight;
		private AIWalkerShooter ai;

		private float alive;

        public Imp() : this(Vector3.Zero)
        {
        }

        public Imp(Vector3 position)
        {
            this.Position = position;

            noticeHandlerNight = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
            noticeHandlerDay = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6, false, noticeFalloffTime: 5);
            buffManager = new BuffManager(this);

            aiDay = new AIWalkerShooter(new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f), new Vector3(Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.70f)), noticeHandlerDay, buffManager, MAX_HEALTH, stats, GlobalState.Registry.ProjectileRegistry.Get("imp_fireball").Id);
            aiDay.ShootSpeed = FIRE_TIME;
            aiDay.MaxVelocity = MAX_VELOCITY;

            aiNight = new AIWalkerShooter(new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f), new Vector3(Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.70f)), noticeHandlerNight, buffManager, MAX_HEALTH, stats, GlobalState.Registry.ProjectileRegistry.Get("imp_fireball").Id);
            aiNight.ShootSpeed = FIRE_TIME;
            aiNight.MaxVelocity = MAX_VELOCITY;

            ai = aiNight;
        }

		public override void Initialize(World world)
		{
			base.Initialize(world);
        }

		public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			alive += (float)deltaTime;

			NoticeHandler<Player> realNoticeHandler = noticeHandlerNight;

			if (!world.IsNight())
				ai = aiDay;
			else ai = aiNight;

			float p0 = (alive % 0.65f) / 0.65f;
			//float p1 = ((alive + 0.3f) % 0.45f) / 0.45f;
			float s0 = MathF.Sin(MathF.PI * 2 * p0) * Cube.CUBE_SCALE * 1.25f;
			//float s1 = MathF.Sin(MathF.PI * 2 * p1) * Cube.CUBE_SCALE * 1.25f;

			BoundingSphere sphere = new BoundingSphere(Position, Cube.CUBE_SCALE * 8);

			world.LightManager2.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
            {
                position = Position + new Vector3(Cube.CUBE_SCALE / 2f),
                min = Cube.CUBE_SCALE * 4f + s0,
                max = Cube.CUBE_SCALE * 8f,
                color = Color.OrangeRed.ToVector4(),
            });

            AIWalkerShooter.Funcs<Imp> funcs = new() { world = world, ai = ai, entity = this };
            funcs.Update(deltaTime);
		}

		public override void OnUnload()
		{
			base.OnUnload();

            AIWalkerShooter.Funcs<Imp> funcs = new() { world = world, ai = ai, entity = this };
            funcs.OnUnload();
        }

        public Stats GetStats()
        {
            return new Stats
            {
                HP = ai.Health,
                MaximumHP = MAX_HEALTH,
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

            if (version > 0)
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
