using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.Server)]
    [EntityMeta(0)]
    public class PlayerBubble : Entity, IHitboxOwner, ISyncBasicState
    {
        private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("bubble");

        private float alive;

        private int hitbox = -1;
        public bool exploding;

        private Rectangle3D bounds = new Rectangle3D(new Vector3(-Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f));
        private float explodingTime;
        
        private readonly int damage;
        private readonly float knockback;
        private readonly int inventorySlot;

        public PlayerBubble(Vector3 position, int damage, float knockback, int inventorySlot)
        {
            this.Position = position;
            this.damage = damage;
            this.knockback = knockback;
            this.inventorySlot = inventorySlot;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            hitbox = world.HitboxManager.Add(this, bounds.Offset(Position), Vector3.Zero, HitboxManager.Group.PLAYER_DEAL, damage, knockback);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            alive += (float)deltaTime;

            if (hitbox != -1)
            {
                //if (exploding) world.HitboxManager.Update(hitbox, Engine.Physics.OrientedBoundingBox.Empty);
                world.HitboxManager.Update(hitbox, bounds.Offset(Position).ToOBB());
            }

            if (exploding && alive >= explodingTime)
                world.EntityManager.Kill(this);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            if (hitbox != -1)
                world.HitboxManager.Remove(hitbox);
        }

        public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
            if (!exploding)
            {
                if ((other.group & HitboxManager.Group.ENEMYHOSTILE_TAKE) == HitboxManager.Group.ENEMYHOSTILE_TAKE)
                {
                    exploding = true;
                    explodingTime = alive + 6f / Main.FIXED_FPS;
                    bounds = new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 2.5f), new Vector3(Cube.CUBE_SCALE * 5f));
                }
            }
        }

        public void Get(out BasicState state)
        {
            state = new BasicState
            {
                position = Position,
                timers = { [0] = explodingTime },
                counters = { [0] = exploding ? 1 : 0 }
            };
        }

        public void Set(ref readonly BasicState state)
        {
            throw new NotImplementedException();
        }
    }
}
