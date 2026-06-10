using BrUtility;
using Engine;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
    [EntityMeta(0)]
    [EntitySerializable(EntitySerializableAttribute.SerializationType.Server)]
    public class SkullheadEye : Entity, IHasStats, ISyncedEntity
    {
        private const float CLAMP_DIST = Cube.CUBE_SCALE * 4f;

        public AIFlierMelee ai;

        private BuffManager buffManager;
        private NoticeHandler<Player> noticeHandler;

        private EntityHelper.DirectionalSourceRect dsr = new EntityHelper.DirectionalSourceRect()
        {
            above = new RectangleF(0, 104, 52, 52),
            below = new RectangleF(0, 104, 52, 52),
            back = new RectangleF(0, 52, 52, 52),
            front = new RectangleF(0, 0, 52, 52),
            sideLeft = new RectangleF(0, 156, 52, 52),
            sideRight = new RectangleF(0, 104, 52, 52),
        };

        private Vector3 anchor;
        public Entity? parent = null;

        public int MaxHealth = 10;

        private float newAnchorTimer = 5;

        public SkullheadEye() 
        {
            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
            buffManager = new BuffManager(this);

            ai = new AIFlierMelee(world, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.7f)),
                new Rectangle3D(-new Vector3(Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f)),
                noticeHandler, buffManager, MaxHealth);
            ai.TurnSpeed = MathHelper.ToRadians(3f);
            ai.CollidesWithWorld = false;

            anchor = new Vector3(GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 12, Cube.CUBE_SCALE * 12), GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 12, Cube.CUBE_SCALE * 12), GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 8, Cube.CUBE_SCALE * 8));
        }

        public SkullheadEye(Vector3 position, Entity parent) : this()
        {
            this.Position = position;
            this.parent = parent;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);
            
            if (parent == null)
            {
                //This is really just in case we spawned this via the console.
                // Just choose the nearest entity. Probably the player.
                float nearestDistance = float.MaxValue;
                Entity? nearest = null;

                var entities = world.EntityManager.GetEntities();

                foreach (Entity entity in entities)
                {
                    float dist = (entity.Position - Position).Length();
                    if (dist < nearestDistance) 
                    {
                        nearestDistance = dist;
                        nearest = entity;
                    }
                }

                Debug.Assert(nearest != null);
                parent = nearest;
            }
        }

        public override void OnUnload()
        {
            base.OnUnload();

            var funcs = new AIFlierMelee.Funcs<SkullheadEye> { ai = ai, entity = this };
            funcs.OnUnload();
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            AlwaysRender = true;

            Vector3 offsetAnchor = parent.Position + anchor;
            Vector3 dir = Position - offsetAnchor;
            float dist = dir.Length();
            dir.Normalize();

            if (dist > Cube.CUBE_SCALE * CLAMP_DIST)
            {
                ai.Velocity -= dir * Cube.CUBE_SCALE * 1.5f;
            }

            var funcs = new AIFlierMelee.Funcs<SkullheadEye> { ai = ai, entity = this };
            funcs.Update(deltaTime);

            if (newAnchorTimer > 0)
                newAnchorTimer -= (float)deltaTime;
            else
            {
                anchor = new Vector3(
                    GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 12, Cube.CUBE_SCALE * 12), 
                    GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 12, Cube.CUBE_SCALE * 12), 
                    GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 8, Cube.CUBE_SCALE * 8));

                newAnchorTimer = 5;
            }

            if (parent.Dead)
                world.EntityManager.Kill(this);
        }

        public void SetStats(Stats stats)
        {
            throw new NotImplementedException();
        }

        public Stats GetStats()
        {
            throw new NotImplementedException();
        }

        public void GetSyncedEntity(out SyncedEntity state)
        {
            var aiEntity = new SyncedEntity();
            ai.Get(out aiEntity);

            state = aiEntity;
        }
    }
}
