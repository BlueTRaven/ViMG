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
    public class StoneBeetle : Entity, IHasStats
    {
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

        private NoticeHandler<Player> noticeHandler;
        private BuffManager buffManager;

        private int maxHealth = 20;

        private float alive;

        private AIWalkerShooter<StoneBeetle> ai;

        public StoneBeetle()
        {
        }

        public StoneBeetle(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            ProjectileManager.ProjectileBatchStats batchStats = new ProjectileManager.ProjectileBatchStats(5, new Vector2(-180, 180), new Vector2(-15, 65));

            ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(
                HitboxManager.Group.ENEMYHOSTILE_BOTH, 1, 1f, Cube.CUBE_SCALE / 8, Cube.CUBE_SCALE, 1, true, 0.5f, true); 
            ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"),
                new RectangleF(0, 16, 16, 16), Cube.CUBE_SCALE);
            visStats.rollFollowsVelocity = true;

            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
            buffManager = new BuffManager(this);

            ai = new AIWalkerShooter<StoneBeetle>(world, this, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, maxHealth, batchStats, stats, visStats);
            ai.ShootSpeed = Cube.CUBE_SCALE * 8;
            ai.MoveTowardsTargetDistance = Cube.CUBE_SCALE * 5f;
            ai.AttackTargetDistance = Cube.CUBE_SCALE * 5f;
            ai.AttackCooldownTime = 0.75f;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            alive += (float)deltaTime;

            ai.Update(deltaTime);
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
            {
                mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);
            }

            RectangleF sourceRect = new RectangleF(0, 0, 16, 16);

            Vector3 velXZ = new Vector3(ai.Velocity.X, 0, ai.Velocity.Z);
            velXZ.Normalize();

            float facingDotCamera = Vector3.Dot(velXZ, -Main.camera.Forward);

            //Facing within 45 degrees of the camera.
            if (facingDotCamera < MathHelper.ToRadians(45))
            {
                sourceRect.y += 16;
            }

            if (ai.GetState() == AIWalkerShooter<StoneBeetle>.State.Normal)
            {
                if (ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                {
                    float animP = (alive % 0.75f) / 0.75f;

                    int frame = (int)(animP * 2f);

                    sourceRect.x += 16 * frame;
                }
            }
            
            if (ai.IsInRangeOfTarget)
            {
                sourceRect = new RectangleF(0, 32, 16, 16);
            }

            Vector3 tintColor = ai.InvulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("stone_beetle"),
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(Position), sourceRect, tintColor));

            DrawHelper3D.DrawHealthbar(device, ai.Health, maxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
        }

        public Stats GetStats()
        {
            return new Stats
            {
                HP = ai.Health,
                MaximumHP = ai.MaxHealth,
            };
        }

        public void SetStats(Stats stats)
        {
            ai.Health = stats.HP;
            ai.MaxHealth = stats.MaximumHP;
        }
    }
}
