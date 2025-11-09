using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public class RendererSkullhead : EntityRenderer
    {
        private static VerySimpleMesh meshHead;
        private static VerySimpleMesh meshVertibrae;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("skullhead");

        public RendererSkullhead(GraphicsDevice device) : base("skullhead", device)
        {
            meshHead = MeshHelper.MakeQuad(device, Cube.PIXEL_SCALE * 128, Cube.PIXEL_SCALE * 128, Enums.Alignment.Bottom);
            meshVertibrae = MeshHelper.MakeQuad(device, Cube.PIXEL_SCALE * 16 * 3, Cube.PIXEL_SCALE * 16, Enums.Alignment.Bottom);
        }

        private static Type[] types = [typeof(Skullhead)];
        public override Type[] GetRenderedTypes()
        {
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> entities)
        {
            // NOTE: only one skullhead at a time is supported?
            Skullhead skullhead = entities[0] as Skullhead;// entityManager.GetFirst<Skullhead>();
            if (skullhead == null) return;

            RectangleF sourceRect = new RectangleF(0, 0, 128, 128);
            Vector3 scale = Vector3.One;

            if (skullhead.state == Skullhead.State.Dash || (skullhead.state == Skullhead.State.Rotate && skullhead.stateTimer >= Skullhead.Constants.ROTATE_TIME - Main.FIXED_STEP * 10f))
            {
                sourceRect = new RectangleF(128, 0, 128, 160);
                scale = new Vector3(1, 160f / 128f, 1);
            }

            if (skullhead.transitioned)
                sourceRect.x += 256f;

            Vector3 tintColor = skullhead.invulnTimer > 0 ? Color.Red.ToVector3() : skullhead.tintColor.ToVector3();

            Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, meshHead,
                Matrix.CreateTranslation(-new Vector3(0, Cube.PIXEL_SCALE * 64, 0)) *
                Matrix.CreateScale(scale) *
                Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                Matrix.CreateTranslation(skullhead.Position), sourceRect, tintColor));

            for (int i = 0; i < skullhead.trainPositions.Length; i++)
            {
                float ioff = (float)i * 0.63f;
                float t = ((skullhead.Alive + ioff) % 2f) / 2f;

                float s = MathF.Sin(MathF.PI * 2 * t) * MathHelper.Lerp(Cube.CUBE_SCALE / 8f, Cube.CUBE_SCALE / 2f, 1 - ((float)i / 12f));

                Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, meshVertibrae,
                    Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                    Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                    Matrix.CreateTranslation(skullhead.trainPositions[i] + Main.camera.Right * s), new RectangleF(0, 128, 48, 16), Color.White.ToVector3()));
            }
        }
    }
}
