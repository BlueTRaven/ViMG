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

        private int[] lights;
        private bool[] shadowmapped;

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

            lights = new int[64];
            shadowmapped = new bool[64];

            Array.Fill(lights, -1);
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

                if (lights[i] == -1)
                {
                    Vector4 color = new Vector4(Main.random.NextFloat(), Main.random.NextFloat(), Main.random.NextFloat(), 50);
                    world.LightManager.AddShadowmapped(lightPos, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 3, color, out lights[i], out shadowmapped[i]);
                }
                else
                {
                    if (shadowmapped[i])
                    {
                        Vector4 color = world.LightManager.GetShadowmapped(lights[i]).color;
                        world.LightManager.UpdateShadowmapped(lights[i], lightPos, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 3, color, true);
                    }
                    else
                    {
                        Vector4 color = world.LightManager.Get(lights[i]).color;
                        world.LightManager.Update(lights[i], lightPos, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 3, color);
                    }
                }
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
