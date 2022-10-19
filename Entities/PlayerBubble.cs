using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class PlayerBubble : Entity, IHitboxOwner
    {
        private (VertexBuffer VBO, IndexBuffer IBO) mesh;

        private float alive;

        private int hitbox = -1;
        private bool exploding;

        private Rectangle3D bounds = new Rectangle3D(new Vector3(-Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f));
        private float explodingTime;

        public PlayerBubble(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            hitbox = world.HitboxManager.Add(this, bounds.Offset(Position), Vector3.Zero, HitboxManager.Group.PLAYER_DEAL, 8, 8);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            alive += (float)deltaTime;

            if (hitbox != -1)
                world.HitboxManager.Update(hitbox, bounds.Offset(Position));

            if (exploding && alive >= explodingTime)
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
                mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2f);

            //Don't draw while exploding
            //TODO: instead of not drawing, draw some "bubble pop" sprite
            if (exploding)
                return;

            float t0 = (alive % 1.75f) / 1.75f;
            float t1 = ((alive + 0.45f) % 2.05f) / 2.05f;
            float s0 = MathF.Sin(MathF.PI * 2 * t0) * 0.5f + 0.5f;
            float s1 = MathF.Sin(MathF.PI * 2 * t1) * 0.5f + 0.5f;

            float scaleX = MathHelper.Lerp(1f, 1.15f, s0);
            float scaleY = MathHelper.Lerp(0.95f, 1.15f, s1);

            Matrix mat = Matrix.CreateTranslation(0, -Cube.CUBE_SCALE, 0) *
                Matrix.CreateScale(scaleX, scaleY, 1) *
                Matrix.CreateBillboard(Position, Main.camera.Position, Main.camera.Up, Main.camera.Forward);

            Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw((Main.camera.Position - Position).Length(),
                mat, Main.assetsManager.GetAsset<Texture2D>("bubble"), DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                new RectangleF(0, 0, 64, 64), Color.White * 0.85f));
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
    }
}
