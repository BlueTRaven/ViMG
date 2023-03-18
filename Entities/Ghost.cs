using BrUtility;
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

namespace ViMG.Entities
{
    public class Ghost : Entity, Buffs.IHasStats
    {
        private const int MAX_HEALTH = 30;

        private float alive;
        private float hurtTimer;

        private AIFlierMelee<Ghost> ai;
        private NoticeHandler<Player> noticeHandler;
        private BuffManager buffManager;

        private Color tintColor;
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

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

            ai.OnUnload();
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            buffManager = new BuffManager(this);
            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);

            ai = new AIFlierMelee<Ghost>(world, this, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.5f), new Vector3(Cube.CUBE_SCALE)),
                new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.75f), new Vector3(Cube.CUBE_SCALE * 0.75f * 2f)),
                noticeHandler, buffManager, 30);

            ai.MaxVelocity = Cube.CUBE_SCALE * 1.25f;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            alive += (float)deltaTime;
            ai.Update(deltaTime);

            AncientAltar nearest = null;
            Vector3 nearestDir = Vector3.Zero;
            IReadOnlyList<Entity> altars = world.EntityManager.GetAll<AncientAltar>();

            for (int i = 0; i < altars.Count; i++)
            {
                Vector3 dir = altars[i].Position - Position;

                if (nearest == null || dir.Length() < nearestDir.Length())
                {
                    nearest = altars[i] as AncientAltar;
                    nearestDir = dir;
                }
            }

            if (nearest != null && nearestDir.Length() < Cube.CUBE_SCALE * 4)
            {
                //unset target
                noticeHandler.Target = null;

                ai.Velocity = Vector3.Normalize(nearestDir) * Cube.CUBE_SCALE * 0.5f;

                hurtTimer -= (float)deltaTime;
                
                if (hurtTimer <= 0)
                {
                    hurtTimer = 1f;

                    ai.Hurt(Vector3.Normalize(nearestDir), 1f, 2);
                }
            }
            else
            {
                //make invulnerable
                ai.InvulnTimer = 1f;
            }
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
                mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2f);

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

            if (flipX)
            {
                sourceRect.x += 32;
                sourceRect.width = -32;
            }

            if (ai.GetState() == AIFlierMelee<Ghost>.State.Attack)
                sourceRect = new RectangleF(0, 96, 32, 32);
            else if (ai.GetState() == AIFlierMelee<Ghost>.State.AttackStun)
                sourceRect = new RectangleF(32, 96, 32, 32);

            Vector3 offset = Vector3.Zero;

            offset.Y = MathF.Sin(MathF.PI * 2 * (alive % 4f) / 4f) * Cube.CUBE_SCALE;

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("grave_ghost"),
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(Position + offset), sourceRect, tintColor.ToVector3()));

            if (ai.Health < MAX_HEALTH)
                DrawHelper3D.DrawHealthbar(device, ai.Health, MAX_HEALTH, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
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
                world.EntityManager.Remove(this);
        }
    }
}
