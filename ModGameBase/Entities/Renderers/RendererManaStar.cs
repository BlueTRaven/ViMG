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
using Engine.Clients;

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
            mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE, Enums.Alignment.Bottom);
        }

        private static int[]? types = null;
        public override int[] GetRenderedTypes()
        {
            if (types == null)
                types = [Main.Registry.EntityRegistry.Get<ManaStar>().Id];
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> entities)
        {
            return;

            var iter = new Iterator<ManaStar>(entities);
            while(iter.Next(out ManaStar manaStar))
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
                        Matrix.CreateRotationX(MathHelper.ToRadians(manaStar.yawPitch.X)) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(manaStar.yawPitch.Y)) *
                    Matrix.CreateTranslation(Main.camera.Position),
                        directionalSourceRect.front, Color.White * manaStar.world.GetTimeOfNight()));
                }
                else if (manaStar.state == ManaStar.State.DivingInWorld)
                {
                    const float FAR_DISTANCE = 32;

                    float t = 1 - manaStar.timer / ManaStar.DIVINGINWORLD_TIME;

                    Matrix lerpStartRotMat = Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
                        Matrix.CreateRotationX(MathHelper.ToRadians(manaStar.yawPitch.X)) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(manaStar.yawPitch.Y));

                    Matrix lerpEndRotMat = Matrix.CreateRotationX(Math.Clamp(-Main.camera.RotationEuler.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                        Matrix.CreateRotationY(-Main.camera.RotationEuler.Y);

                    Vector3 lerpStartPos = Vector3.Transform(Vector3.Zero, Matrix.CreateTranslation(Vector3.Up * Cube.CUBE_SCALE * FAR_DISTANCE) *
                        Matrix.CreateRotationX(MathHelper.ToRadians(manaStar.yawPitch.X)) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(manaStar.yawPitch.Y)) *
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

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int type)
        {
            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entCurr = client.Current().entities.GetById(reference.id);
                var entPrev = client.Previous(1).entities.GetById(reference.id);

                var position = entPrev.GetInterpPosition(entCurr);
                var timer = entPrev.GetInterpTimer(entCurr, 0);

                var yawPitch = new Vector2(entCurr.rotation.X, entCurr.rotation.Y);

                var state = (ManaStar.State)entCurr.state; //.GetInterpCounter(entCurr, 0);

                if (state == ManaStar.State.InSky || state == ManaStar.State.DivingInSky)
                {
                    const float FAR_DISTANCE = 70;
                    const float NEAR_DISTANCE = 32;
                    float distance = FAR_DISTANCE;

                    if (state == ManaStar.State.DivingInSky)
                        distance = MathHelper.Lerp(FAR_DISTANCE, NEAR_DISTANCE, Easings.EaseInCubic(1 - timer / ManaStar.DIVINGINSKY_TIME));

                    Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(900,
                        material, mesh,
                        Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
                        Matrix.CreateTranslation(Vector3.Up * Cube.CUBE_SCALE * distance) *
                        Matrix.CreateRotationX(MathHelper.ToRadians(yawPitch.X)) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(yawPitch.Y)) *
                    Matrix.CreateTranslation(Main.camera.Position),
                        // TODO mult by time
                        directionalSourceRect.front, Color.White /** world.GetTimeOfNight()*/));
                }
                else if (state == ManaStar.State.DivingInWorld)
                {
                    const float FAR_DISTANCE = 32;

                    float t = 1 - timer / ManaStar.DIVINGINWORLD_TIME;

                    Matrix lerpStartRotMat = Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
                        Matrix.CreateRotationX(MathHelper.ToRadians(yawPitch.X)) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(yawPitch.Y));

                    Matrix lerpEndRotMat = Matrix.CreateRotationX(Math.Clamp(-Main.camera.RotationEuler.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                        Matrix.CreateRotationY(-Main.camera.RotationEuler.Y);

                    Vector3 lerpStartPos = Vector3.Transform(Vector3.Zero, Matrix.CreateTranslation(Vector3.Up * Cube.CUBE_SCALE * FAR_DISTANCE) *
                        Matrix.CreateRotationX(MathHelper.ToRadians(yawPitch.X)) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(yawPitch.Y)) *
                        Matrix.CreateTranslation(Main.camera.Position));
                    Vector3 lerpEndPos = position;

                    RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(Vector3.Normalize(lerpEndPos - lerpStartPos), directionalSourceRect);
                    float sx = float.Abs(sourceRect.width / 4f);
                    Vector3 p = Vector3.Lerp(lerpStartPos, lerpEndPos, Easings.EaseInExpo(t));

                    float sortVal = (Main.camera.Position - position).Length();

                    Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(sortVal,
                    material, mesh, lerpStartRotMat * Matrix.CreateTranslation(p),
                        // TODO mult by time
                        directionalSourceRect.front, Color.White /** manaStar.world.GetTimeOfNight() * (1 - t)*/));

                    Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(sortVal,
                        material, mesh,
                    Matrix.CreateScale(sx, 1, 1) *
                        lerpEndRotMat * Matrix.CreateTranslation(p),
                        // TODO mult by time
                        sourceRect, Color.White /** manaStar.world.GetTimeOfNight() * t)*/));
                }
            }
        }
    }
}
