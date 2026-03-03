using Engine;
using Engine.Networking;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.IMGUIImpl;

namespace ModGameBase.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.Server)]
    [EntityMeta(0)]
    public class BonePillar : Entity, IHasStats, ISyncedEntity
    {
        private NoticeHandler<Player> noticeHandler;
        private BuffManager buffManager;

        private int maxHealth = 20;

        private AiFlierShooter ai;

        public BonePillar() { }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            ProjectileManager.ProjectileStats stats = new()
            {
                damage = 1,
                knockback = 0.25f,
                pierce = 1,
                gravityScale = 0.45f,
                dieOnCollision = false,
                collides = true,
                gravity = true,

                group = HitboxManager.Group.ENEMYHOSTILE_DEAL,
                collisionRadius = Cube.CUBE_SCALE / 4f,
                size = Cube.CUBE_SCALE / 4f,
            };

            ProjectileManager.ProjectileBatchStats bstats = new ProjectileManager.ProjectileBatchStats(5, new Vector2(-45, 45),
                new Vector2(0, 360));

            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
            buffManager = new BuffManager(this);

            ai = new AiFlierShooter(world, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, maxHealth, new AiFlierShooter.ShootConfig()
                {
                    shootsBatch = true,
                    batchStats = bstats,
                    stats = stats,
                    visStatsId = GlobalState.Registry.ProjectileRegistry.Get("bone").Id,
                });
            ai.Acceleration = Cube.CUBE_SCALE / 16f;
            ai.MaxVelocity = Cube.CUBE_SCALE;
            ai.ShootSpeed = Cube.CUBE_SCALE * 4;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            AiFlierShooter.Funcs<BonePillar> funcs = new() { ai = ai, entity = this };
            funcs.OnUnload();
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            ai.AttackTargetDistance = Cube.CUBE_SCALE * 5f;
            ai.MoveTowardsTargetDistance = Cube.CUBE_SCALE * 4f;

            AiFlierShooter.Funcs<BonePillar> funcs = new() { ai = ai, entity = this };
            funcs.Update(deltaTime);
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

        public void GetSyncedEntity(out SyncedEntity state)
        {
            ai.Get(out state);
            state = state with
            {
                position = Position,
            };
        }
    }
}
