using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModGameBase.Entities;
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
    public class Skeleton2 : Entity, IHasStats, ISyncBasicState
    {
        // TODO re-implement bone whistle behavior
        public AIWalkerMelee ai;
        private NoticeHandler<Player> noticeHandler;
        private BuffManager buffManager;

        private float alive;

        public Skeleton2()
        {

        }

        public Skeleton2(Vector3 position)
        {
            this.Position = position;
        }

        public override void OnDelete()
        {
            base.OnDelete();

            world.EntityManager.Add(new SkeletonBonePile(Position));
        }

        public override void OnUnload()
        {
            base.OnUnload();

            var funcs = new AIWalkerMelee.Funcs<Skeleton2> { ai = ai, entity = this };
            funcs.OnUnload();
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            buffManager = new BuffManager(this);
            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);

            ai = new AIWalkerMelee(new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.7f)),
                new Rectangle3D(-new Vector3(Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f)),
                noticeHandler,
                buffManager,
                12);

            ai.InvulnTimer = 0.5f;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);
            alive += (float)deltaTime;

            var funcs = new AIWalkerMelee.Funcs<Skeleton2> { ai = ai, entity = this };
            funcs.Update(deltaTime);
        }

        public Stats GetStats()
        {
            return new Stats()
            {
                HP = ai.Health,
                MaximumHP = ai.MaxHealth,
                AttackSpeed = ai.AttackCooldownTime,
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
