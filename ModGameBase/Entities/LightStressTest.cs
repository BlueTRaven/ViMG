using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using ViMG.Rendering;
using ViMG.IMGUIImpl;
using Engine;

namespace ViMG.Entities
{
    public class LightStressTest : Entity
    {
        [ConsoleCommandVar("light_stress_test_speed")]
        public static float Speed = 1;

        [ConsoleCommandVar("light_stress_test_radius_xz")]
        public static float RADIUS_XZ = Cube.CUBE_SCALE * 16;
        [ConsoleCommandVar("light_stress_test_radius_y")]
        public const float RADIUS_Y = Cube.CUBE_SCALE * 2f;
        //private static VerySimpleMesh mesh;
        //private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("glow_node");

        public LightStressTest()
        {

        }

        public LightStressTest(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            for (int i = 0; i < 64; i++)
            {
                var worldTime = world.GetTime() * Speed;
                float t = ((worldTime + 0.03f * i) % 2f) / 2f;
                float zt = ((worldTime + 0.03f * i + 0.3f) % 2f) / 2f;

                float x = MathF.Cos(MathF.PI * 2 * t) * RADIUS_XZ;
                float y = MathF.Sin(MathF.PI * 2 * t) * RADIUS_Y;
                float z = -MathF.Sin(MathF.PI * 2 * zt) * RADIUS_XZ;

                Vector3 lightPos = Position + new Vector3(x, y, z);

                Vector4 color = new Vector4(GlobalState.random.NextFloat(), GlobalState.random.NextFloat(), GlobalState.random.NextFloat(), 50);
                world.LightManager2.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
                {
                    position = lightPos,
                    min = Cube.CUBE_SCALE * 2,
                    max = Cube.CUBE_SCALE * 3,
                    color = color,
                });
            }
        }

        //public override void Draw(GraphicsDevice device, Effect effect)
        //{
        //    base.Draw(device, effect);

        //    if (mesh.IBO == null)
        //        mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2, Enums.Alignment.Bottom);
        //    //mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

        //    for (int i = 0; i < 64; i++)
        //    {
        //        float t = ((world.GetTime() + 0.03f * i) % 2f) / 2f;
        //        float zt = ((world.GetTime() + 0.03f * i + 0.3f) % 2f) / 2f;

        //        float x = MathF.Cos(MathF.PI * 2 * t) * RADIUS_XZ;
        //        float y = MathF.Sin(MathF.PI * 2 * t) * RADIUS_Y;
        //        float z = -MathF.Sin(MathF.PI * 2 * zt) * RADIUS_XZ;

        //        Vector3 lightPos = Position + new Vector3(x, y, z);

        //        Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
        //            Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
        //            Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
        //            Matrix.CreateTranslation(lightPos), null));
        //    }
        //}
    }
}
