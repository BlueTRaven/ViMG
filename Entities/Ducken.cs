using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class Ducken : Entity, IHasStats, IHitboxOwner
    {
        private const int MAX_HEALTH = 8;
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

        private NoticeHandler<Player> noticeHandler;
        private BuffManager buffManager;
        private AIPassive<Ducken> ai;

        private float alive;

        public Ducken(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);
            buffManager = new BuffManager(this);

            ai = new AIPassive<Ducken>(this, new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.35f, 0, -Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, MAX_HEALTH);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            alive += (float)deltaTime;

            ai.Update(deltaTime);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            ai.OnUnload();
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
            {
                mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);
            }

            RectangleF sourceRect = new RectangleF(0, 0, 32, 32);

            Vector3 velXZ = new Vector3(ai.Facing.X, 0, ai.Facing.Z);
            velXZ.Normalize();

            int direction = 0;
            float facingDotCamera = Vector3.Dot(velXZ, Main.camera.ForwardYawOnly);
            bool flipX = false;

            if (facingDotCamera < -0.3f)
            {
                direction = 2;
                sourceRect.y = 64;
            }
            else if (facingDotCamera < 0.2f)
            {
                direction = 1;
                sourceRect.y = 32;

                float facing = velXZ.X * Main.camera.ForwardYawOnly.Z - velXZ.Z * Main.camera.ForwardYawOnly.X;

                if (facing < 0)
                {
                    flipX = true;
                }
            }

            if (ai.GetState() == AIPassive<Ducken>.State.Normal)
            {
                if (ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                {
                    int numFrames;

                    if (direction == 0 || direction == 2)
                        numFrames = 4;
                    else if (direction == 1)
                        numFrames = 2;
                    else numFrames = 0;

                    float animP = (alive % 0.75f) / 0.75f;

                    int frame = (int)(animP * numFrames);

                    sourceRect.x += 32 * frame;

                    if (flipX)
                    {
                        sourceRect.x += 32;
                        sourceRect.width = -32;
                    }
                }
            }

            Vector3 tintColor = ai.InvulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("ducken"),
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(Position), sourceRect, tintColor));

            if (ai.Health < MAX_HEALTH)
                DrawHelper3D.DrawHealthbar(device, ai.Health, MAX_HEALTH, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
        }

        public Stats GetStats()
        {
            throw new NotImplementedException();
        }

        public void SetStats(Stats stats)
        {
            throw new NotImplementedException();
        }

        public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
        }
    }
}
