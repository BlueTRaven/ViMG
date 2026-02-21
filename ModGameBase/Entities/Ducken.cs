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
    public class Ducken : Entity, IHasStats, ISyncBasicState
    {
        private const int MAX_HEALTH = 8;
        private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("ducken");

        private NoticeHandler<Player> noticeHandler;
        private BuffManager buffManager;
        public AIPassive ai;

        private float alive;

        public Ducken() { }

        public Ducken(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);
            buffManager = new BuffManager(this);

            ai = new AIPassive(new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.35f, 0, -Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, MAX_HEALTH);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            alive += (float)deltaTime;

            AIPassive.Funcs<Ducken> funcs = new AIPassive.Funcs<Ducken> { ai = ai, entity = this };
            funcs.Update(deltaTime);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            AIPassive.Funcs<Ducken> funcs = new AIPassive.Funcs<Ducken> { ai = ai, entity = this };
            funcs.OnUnload();
        }

        public Stats GetStats()
        {
            return new Stats
            {
                MaximumHP = MAX_HEALTH,
                HP = ai.Health,
            };
        }

        public void SetStats(Stats stats)
        {
            ai.MaxHealth = stats.MaximumHP;
            ai.Health = stats.HP;
        }

        public void Get(out BasicState state)
        {
            BasicState aiState = new BasicState();
            ai?.Get(out aiState);
            aiState.position = Position;
            state = aiState;
        }

        public void Set(ref readonly BasicState state)
        {
            throw new NotImplementedException();
        }
    }
}
