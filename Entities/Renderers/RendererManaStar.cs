using BepuPhysics.Constraints;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ViMG.Entities.EntityHelper;
using ViMG.Cubes;
using BrUtility;
using ViMG.Rendering;
using Microsoft.Xna.Framework;

namespace ViMG.Entities.Renderers
{
    public class RendererManaStar : EntityRenderer
    {
        private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("mana_star");
        private static DirectionalSourceRect directionalSourceRect = new DirectionalSourceRect()
        {
            front = new RectangleF(0, 0, 4, 4),
            back = new RectangleF(0, 0, 4, 4),
            sideLeft = new RectangleF(4, 0, 8, 4),
        };

        public RendererManaStar(GraphicsDevice device) : base("mana_star", device)
        {
        }

        private static Type[] types = [typeof(ManaStar)];
        public override Type[] GetRenderedTypes()
        {
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex)
        {
            if (mesh.IBO == null)
                mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE, Enums.Alignment.Bottom);

            var entities = entityManager.GetAll<ManaStar>();

            foreach (ManaStar manaStar in entities)
            {
                if (manaStar.state == ManaStar.State.InSky || manaStar.state == ManaStar.State.DivingInSky)
                {
                    const float FAR_DISTANCE = 70;
                    const float NEAR_DISTANCE = 32;
                    float distance = FAR_DISTANCE;

                    if (manaStar.state == ManaStar.State.DivingInSky)
                        distance = MathHelper.Lerp(FAR_DISTANCE, NEAR_DISTANCE, Easings.EaseInCubic(1 - manaStar.timer / ManaStar.DIVINGINSKY_TIME));

                    Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(900,
                        material, mesh,
                        Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
                        Matrix.CreateTranslation(Vector3.Up * Cube.CUBE_SCALE * distance) *
                        Matrix.CreateRotationX(MathHelper.ToRadians(manaStar.pitchYaw.X)) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(manaStar.pitchYaw.Y)) *
                    Matrix.CreateTranslation(Main.camera.Position),
                        directionalSourceRect.front, Color.White * manaStar.world.GetTimeOfNight()));
                }
                else if (manaStar.state == ManaStar.State.DivingInWorld)
                {
                    const float FAR_DISTANCE = 32;

                    float t = 1 - manaStar.timer / ManaStar.DIVINGINWORLD_TIME;

                    Matrix lerpStartRotMat = Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
                        Matrix.CreateRotationX(MathHelper.ToRadians(manaStar.pitchYaw.X)) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(manaStar.pitchYaw.Y));

                    Matrix lerpEndRotMat = Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                        Matrix.CreateRotationY(-Main.camera.Rotation.Y);

                    Vector3 lerpStartPos = Vector3.Transform(Vector3.Zero, Matrix.CreateTranslation(Vector3.Up * Cube.CUBE_SCALE * FAR_DISTANCE) *
                        Matrix.CreateRotationX(MathHelper.ToRadians(manaStar.pitchYaw.X)) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(manaStar.pitchYaw.Y)) *
                    Matrix.CreateTranslation(manaStar.cameraPosition));
                    Vector3 lerpEndPos = manaStar.Position;

                    RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(Vector3.Normalize(lerpEndPos - lerpStartPos), directionalSourceRect);
                    float sx = float.Abs(sourceRect.width / 4f);
                    Vector3 p = Vector3.Lerp(lerpStartPos, lerpEndPos, Easings.EaseInExpo(t));

                    float sortVal = (Main.camera.Position - manaStar.Position).Length();

                    Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(sortVal,
                    material, mesh, lerpStartRotMat * Matrix.CreateTranslation(p),
                        directionalSourceRect.front, Color.White * manaStar.world.GetTimeOfNight() * (1 - t)));

                    Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(sortVal,
                        material, mesh,
                    Matrix.CreateScale(sx, 1, 1) *
                        lerpEndRotMat * Matrix.CreateTranslation(p),
                        sourceRect, Color.White * manaStar.world.GetTimeOfNight() * t));
                }
            }
        }
    }
}
