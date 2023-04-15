using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities
{
    public class GenericExplosion : Entity, IHitboxOwner
    {
        private (VertexBuffer VBO, IndexBuffer IBO) mesh;

        private const float EXPLOSION_TIME = 10f / 60f;
        private const float HITBOX_TIME = 4f / 60f;
        
        private readonly HitboxManager.Group group;
        private readonly int damage;
        private readonly float knockback;
        private readonly float radius;

        private float hitboxTimer;
        private float timer;

        private int hitbox = -1;

        public GenericExplosion(Vector3 position, HitboxManager.Group group, int damage, float knockback, float radius)
        {
            this.Position = position;

            timer = EXPLOSION_TIME;
            hitboxTimer = HITBOX_TIME;
            this.group = group;
            this.damage = damage;
            this.knockback = knockback;
            this.radius = radius;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            timer -= (float)deltaTime;
            hitboxTimer -= (float)deltaTime;

            if (hitboxTimer <= 0)
            {
                if (hitbox != -1)
                {
                    world.HitboxManager.Remove(hitbox);
                    hitbox = -1;
                }
            }
            else
            {
                if (hitbox == -1)
                    hitbox = world.HitboxManager.Add(this, new Rectangle3D(Position, Vector3.Zero), Vector3.Up, group, damage, knockback);

                float hitboxSize = (1 - hitboxTimer / HITBOX_TIME) * radius;

                world.HitboxManager.Update(hitbox, new Rectangle3D(Position - new Vector3(hitboxSize / 2f), new Vector3(hitboxSize)));
            }

            if (timer <= 0)
                world.EntityManager.Remove(this);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            if (hitbox != -1)
                world.HitboxManager.Remove(hitbox);
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
                mesh = DrawHelper3D.MakeUVSphere(device, 1f);

            float radius = (1 - timer / EXPLOSION_TIME) * this.radius;
            float sort = (Position - Main.camera.Position).Length();
            Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(sort,
                new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), mesh.VBO, mesh.IBO,
                Matrix.CreateScale(radius) * Matrix.CreateTranslation(Position), null, Color.Red * 0.5f));
        }

        public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
        }
    }
}
