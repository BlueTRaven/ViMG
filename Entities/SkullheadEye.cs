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
using ViMG.Rendering;

namespace ViMG.Entities
{
    public class SkullheadEye : Entity, IHasStats
    {
        private const float CLAMP_DIST = Cube.CUBE_SCALE * 4f;
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;
        private static (VertexBuffer VBO, IndexBuffer IBO) lineMesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("skullhead_eye");

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
        private Entity parent;

        private int MaxHealth = 10;

        public SkullheadEye(Vector3 position, Entity parent)
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
            ai.CollidesWithWorld = false;

            anchor = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 12, Cube.CUBE_SCALE * 12), Main.random.NextFloat(-Cube.CUBE_SCALE * 12, Cube.CUBE_SCALE * 12), Main.random.NextFloat(-Cube.CUBE_SCALE * 8, Cube.CUBE_SCALE * 8));
        }

        public override void OnUnload()
        {
            base.OnUnload();

            ai.OnUnload();
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

            ai.Update(deltaTime);

            if (parent.Dead)
                world.EntityManager.Remove(this);
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

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(material, mesh.VBO, mesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(Position), sourceRect, tintColor));

            if (ai.Health < MaxHealth)
                DrawHelper3D.DrawHealthbar(device, ai.Health, MaxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));

            Vector3 offsetAnchor = parent.Position;
            //Offset it slightly so we don't see the line poking through the billboard
            Vector3 offset = Vector3.Normalize(offsetAnchor - Position) * Cube.CUBE_SCALE / 10f;
            DrawHelper3D.DrawLineTiled(Position + offset, offsetAnchor - offset, Cube.PIXEL_SCALE * 2f, Cube.CUBE_SCALE, material, 
                lineMesh, new RectangleF(52, 0, 4, 16), Color.White);
        }
    }
}
