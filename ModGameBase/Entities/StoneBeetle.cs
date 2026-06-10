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
    public class StoneBeetle : Entity, IHasStats, ISyncedEntity
    {
        public const float MOVE_TOWARDS_TARGET_DIST = Cube.CUBE_SCALE * 5f;
        private NoticeHandler<Player> noticeHandler;
        private BuffManager buffManager;

        private int maxHealth = 20;

        public AIWalkerShooter ai;

        public StoneBeetle() : this(Vector3.Zero)
        {
        }

        public StoneBeetle(Vector3 position)
        {
            this.Position = position;

            ProjectileManager.ProjectileBatchStats batchStats = new ProjectileManager.ProjectileBatchStats(5, new Vector2(-180, 180), new Vector2(-15, 65));

            ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(
                HitboxManager.Group.ENEMYHOSTILE_BOTH, 1, 1f, Cube.CUBE_SCALE / 8, Cube.CUBE_SCALE, 1, true, 0.5f, true);
            ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(new RectangleF(0, 16, 16, 16), Cube.CUBE_SCALE);
            visStats.rollFollowsVelocity = true;

            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
            buffManager = new BuffManager(this);

            ai = new AIWalkerShooter(new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, maxHealth, batchStats, stats, GlobalState.Registry.ProjectileRegistry.Get("shard").Id);
            ai.ShootSpeed = Cube.CUBE_SCALE * 8;
            ai.MoveTowardsTargetDistance = MOVE_TOWARDS_TARGET_DIST;
            ai.AttackTargetDistance = MOVE_TOWARDS_TARGET_DIST;
            ai.AttackCooldownTime = 0.75f;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            AIWalkerShooter.Funcs<StoneBeetle> funcs = new AIWalkerShooter.Funcs<StoneBeetle> { world = world, ai = ai, entity = this };
            funcs.OnUnload();
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            AIWalkerShooter.Funcs<StoneBeetle> funcs = new AIWalkerShooter.Funcs<StoneBeetle> { world = world, ai = ai, entity = this };
            funcs.Update(deltaTime);
        }

        public Stats GetStats()
        {
            return new Stats
            {
                HP = ai.Health,
                MaximumHP = ai.MaxHealth,
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
            if (noticeHandler.Noticed)
            {
                float distance = world.DistanceFromPlayer(noticeHandler.Target, Position);
                aiState.counters[0] = distance < MOVE_TOWARDS_TARGET_DIST ? 1 : 0;
            }
            else aiState.counters[0] = 0;

            state = aiState;
        }
    }
}
