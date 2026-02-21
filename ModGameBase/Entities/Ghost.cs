using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
    [EntityMeta(1, 0)]
    public class Ghost : Entity, Buffs.IHasStats, ISyncBasicState
    {
        private const int MAX_HEALTH = 30;
        //private static VerySimpleMesh mesh;
        //private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("grave_ghost");

        public float alive;
        private float hurtTimer;

        public  AIFlierMelee ai;
        private NoticeHandler<Player> noticeHandler;
        private BuffManager buffManager;

        private Color tintColor;

        private float despawnTimer = 20;

        public Ghost()
        {
        }

        public Ghost(Vector3 position)
        {
            this.Position = position;
        }

        public override void OnUnload()
        {
            base.OnUnload();

            var funcs = new AIFlierMelee.Funcs<Ghost> { ai = ai, entity = this };
            funcs.OnUnload();
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            buffManager = new BuffManager(this);
            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);

            ai = new AIFlierMelee(world, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.5f), new Vector3(Cube.CUBE_SCALE)),
                new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.75f), new Vector3(Cube.CUBE_SCALE * 0.75f * 2f)),
                noticeHandler, buffManager, 30);

            ai.MaxVelocity = Cube.CUBE_SCALE * 1.25f;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            alive += (float)deltaTime;
            var funcs = new AIFlierMelee.Funcs<Ghost> { ai = ai, entity = this };
            funcs.Update(deltaTime);

            AncientAltar nearest = null;
            Vector3 nearestDir = Vector3.Zero;
            IReadOnlyList<Entity> altars = world.EntityManager.GetAll<AncientAltar>();

            if (noticeHandler.Noticed)
            {
                despawnTimer = 20f;

                if (noticeHandler.Target.Dead)
                    despawnTimer = -1;
            }

            for (int i = 0; i < altars.Count; i++)
            {
                Vector3 dir = altars[i].Position - Position;

                if (nearest == null || dir.Length() < nearestDir.Length())
                {
                    nearest = altars[i] as AncientAltar;
                    nearestDir = dir;
                }
            }

            ai.Invulnerable = false;
            if (nearest != null && nearestDir.Length() < Cube.CUBE_SCALE * 4)
            {
                despawnTimer = 20f;

                //unset target
                noticeHandler.Target = null;

                ai.Velocity = Vector3.Normalize(nearestDir) * Cube.CUBE_SCALE * 0.5f;

                hurtTimer -= (float)deltaTime;
                
                if (hurtTimer <= 0)
                {
                    hurtTimer = 1f;

                    funcs.Hurt(2);
                }
            }
            else
            {
                //make invulnerable
                ai.Invulnerable = true;
            }

            if (despawnTimer <= 0)
                world.EntityManager.Kill(this);
            else despawnTimer -= (float)deltaTime;
        }

        public Stats GetStats()
        {
            return new Stats()
            {
                HP = ai.Health,
                MaximumHP = MAX_HEALTH,

                TintColor = tintColor,
            };
        }

        public void SetStats(Stats stats)
        {
            ai.Health = stats.HP;
            ai.MaxHealth = stats.MaximumHP;
            tintColor = stats.TintColor;

            if (stats.HP <= 0 || stats.MaximumHP <= 0)
                world.EntityManager.Kill(this);
        }

        public void Get(out BasicState state)
        {
            BasicState aiState = new();
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
