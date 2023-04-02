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
    public class SkullheadEye : Entity, IHasStats
    {
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;
        private static (VertexBuffer VBO, IndexBuffer IBO) lineMesh;
        private AIFlierMelee<SkullheadEye> ai;

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
        private Skullhead parent;

        private int MaxHealth = 10;

        public SkullheadEye(Vector3 position, Skullhead parent)
        {
            this.Position = position;
            this.parent = parent;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
            buffManager = new BuffManager(this);

            ai = new AIFlierMelee<SkullheadEye>(world, this, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.7f)),
                new Rectangle3D(-new Vector3(Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f)),
                noticeHandler, buffManager, MaxHealth);
            ai.TurnSpeed = MathHelper.ToRadians(3f);
            ai.Facing = Vector3.Forward;

            anchor = Position + new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 8, Cube.CUBE_SCALE * 8), Main.random.NextFloat(-Cube.CUBE_SCALE * 8, Cube.CUBE_SCALE * 8), Main.random.NextFloat(-Cube.CUBE_SCALE * 8, Cube.CUBE_SCALE * 8));
        }

        public override void OnUnload()
        {
            base.OnUnload();

            ai.OnUnload();
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            Vector3 dir = Position - anchor;
            float dist = dir.Length();
            dir.Normalize();

            if (dist > Cube.CUBE_SCALE * 8f)
                Position = anchor + dir * Cube.CUBE_SCALE * 8f;

            ai.Update(deltaTime);
        }

        public void SetStats(Stats stats)
        {
            throw new NotImplementedException();
        }

        public Stats GetStats()
        {
            throw new NotImplementedException();
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
            {
                mesh = MeshHelper.MakeCenteredQuad(device, Cube.PIXEL_SCALE * 32, Cube.PIXEL_SCALE * 32);
                lineMesh = MeshHelper.MakeEnemyQuad(device, 1, 1);
            }

            RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(ai.Facing, dsr);

            Vector3 tintColor = ai.InvulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("skullhead_eye"),
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(Position), sourceRect, tintColor));

            if (ai.Health < MaxHealth)
                DrawHelper3D.DrawHealthbar(device, ai.Health, MaxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));

            //Offset it slightly so we don't see the line poking through the billboard
            Vector3 offset = Vector3.Normalize(anchor - Position) * Cube.CUBE_SCALE / 10f;
            DrawHelper3D.DrawLineTiled(Position + offset, anchor, Cube.PIXEL_SCALE * 2f, Cube.CUBE_SCALE, lineMesh,
                Main.assetsManager.GetAsset<Texture2D>("skullhead_eye"), new RectangleF(52, 0, 4, 16), Color.White);
        }
    }
}
