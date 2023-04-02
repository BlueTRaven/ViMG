using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    //Spawns on world creation - always active.
    public class ManaStar : Entity
    {
        private enum State
        {
            InSky,
            DivingInSky,
            DivingInWorld,
            Finished,
        }
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

        private Vector2 pitchYaw;

        private Vector3 cameraPosition;

        private State state;
        private const float DIVINGINSKY_TIME = 8f;
        private const float DIVINGINWORLD_TIME = 4f;
        private float timer = DIVINGINSKY_TIME;

        private EntityHelper.DirectionalSourceRect directionalSourceRect = new EntityHelper.DirectionalSourceRect()
        {
            front = new RectangleF(0, 0, 4, 4),
            back = new RectangleF(0, 0, 4, 4),
            sides = new RectangleF(4, 0, 8, 4),
        };

        public ManaStar()
        {
        }

        public ManaStar(Vector2 pitchYaw)
        {
            this.pitchYaw = pitchYaw;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            timer -= (float)deltaTime;
            if (state == State.DivingInSky)
            {
                if (timer <= 0)
                {
                    timer = DIVINGINWORLD_TIME;
                    state = State.DivingInWorld;

                    //Set position to be the point where we end up being eventually.
                    
                    Vector2 startXZ = world.player.Position.XZ() + new Vector2(Main.random.Next(-32, 32) * Cube.CUBE_SCALE, Main.random.Next(-32, 32) * Cube.CUBE_SCALE);
                    CubePosition endPos = world.ChunkManager.InitializerView.GetFirstSolidDown(
                        CubePosition.FromWorldSpace(new Vector3(startXZ.X, world.sizeInCubes * Cube.CUBE_SCALE, startXZ.Y))).Get() +
                        new CubePosition(0, 1, 0);
                    Position = endPos.InWorldSpace();

                    cameraPosition = Main.camera.Position;
                }
            }
            else if (state == State.DivingInWorld)
            {
                if (timer <= 0)
                {
                    state = State.Finished;
                    timer = 8f;
                }    
            }
            else if (state == State.Finished)
            {
                world.ChunkManager.InitializerView.SetCube(CubePosition.FromWorldSpace(Position), 
                    Main.Registry.CubeRegistry.Get("mana_star").Id, true);
                //if (timer <= 0)
                    world.EntityManager.Remove(this);
            }

            AlwaysRender = world.IsNight();

            if (Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton))
            {
                timer = DIVINGINSKY_TIME;
                state = State.DivingInSky;
            }
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
                mesh = MeshHelper.MakeCenteredQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

            if (state == State.InSky || state == State.DivingInSky)
            {
                const float FAR_DISTANCE = 70;
                const float NEAR_DISTANCE = 32;
                float distance = FAR_DISTANCE;

                if (state == State.DivingInSky)
                    distance = MathHelper.Lerp(FAR_DISTANCE, NEAR_DISTANCE, Easings.EaseInCubic(1 - timer / DIVINGINSKY_TIME));

                Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(900,
                    Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
                    Matrix.CreateTranslation(Vector3.Up * Cube.CUBE_SCALE * distance) *
                    Matrix.CreateRotationX(MathHelper.ToRadians(pitchYaw.X)) *
                    Matrix.CreateRotationY(MathHelper.ToRadians(pitchYaw.Y)) *
                    Matrix.CreateTranslation(Main.camera.Position),
                    Main.assetsManager.GetAsset<Texture2D>("mana_star"), DrawHelper.WhitePixel,
                    mesh.VBO, mesh.IBO, directionalSourceRect.front, Color.White * world.GetTimeOfNight()));
            }
            else if (state == State.DivingInWorld)
            {
                const float FAR_DISTANCE = 32;

                float t = 1 - timer / DIVINGINWORLD_TIME;

                Matrix lerpStartRotMat = Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
                    Matrix.CreateRotationX(MathHelper.ToRadians(pitchYaw.X)) *
                    Matrix.CreateRotationY(MathHelper.ToRadians(pitchYaw.Y));

                Matrix lerpEndRotMat = Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                    Matrix.CreateRotationY(-Main.camera.Rotation.Y);

                Vector3 lerpStartPos = Vector3.Transform(Vector3.Zero, Matrix.CreateTranslation(Vector3.Up * Cube.CUBE_SCALE * FAR_DISTANCE) *
                    Matrix.CreateRotationX(MathHelper.ToRadians(pitchYaw.X)) *
                    Matrix.CreateRotationY(MathHelper.ToRadians(pitchYaw.Y)) *
                    Matrix.CreateTranslation(cameraPosition));
                Vector3 lerpEndPos = Position;

                RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(Vector3.Normalize(lerpEndPos - lerpStartPos), directionalSourceRect);
                float sx = float.Abs(sourceRect.width / 4f);
                Vector3 p = Vector3.Lerp(lerpStartPos, lerpEndPos, Easings.EaseInExpo(t));

                float sortVal = (Main.camera.Position - Position).Length();

                Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw(sortVal,
                    lerpStartRotMat * Matrix.CreateTranslation(p),
                    Main.assetsManager.GetAsset<Texture2D>("mana_star"), DrawHelper.WhitePixel,
                    mesh.VBO, mesh.IBO, directionalSourceRect.front, Color.White * world.GetTimeOfNight() * (1 - t)));

                Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw(sortVal,
                    Matrix.CreateScale(sx, 1, 1) *
                    lerpEndRotMat * Matrix.CreateTranslation(p),
                    Main.assetsManager.GetAsset<Texture2D>("mana_star"), DrawHelper.WhitePixel,
                    mesh.VBO, mesh.IBO, sourceRect, Color.White * world.GetTimeOfNight() * t));
            }
            else if (state == State.Finished)
            {
            }
        }
    }
}
