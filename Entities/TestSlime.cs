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
    public class TestSlime : Entity, IHasStats
    {
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

        private BuffManager buffManager;
        private NoticeHandler<Player> noticeHandler;
        private AISlime<TestSlime> ai;

        public TestSlime(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            buffManager = new BuffManager(this);
            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);

            ai = new AISlime<TestSlime>(this, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
                new Vector3(Cube.CUBE_SCALE * 0.70f)), noticeHandler, buffManager, 4);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            ai.Update(deltaTime);
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
                mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("slime"),
                DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(Position), new RectangleF(0, 0, 16, 16)));

            DrawHelper3D.DrawHealthbar(device, ai.Health, ai.MaxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
        }

        public Stats GetStats()
        {
            throw new NotImplementedException();
        }

        public void SetStats(Stats stats)
        {
            throw new NotImplementedException();
        }
    }
}
