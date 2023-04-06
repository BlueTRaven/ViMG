using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Physics;
using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using ViMG.Rendering;

namespace ViMG.WorldLogics
{
    public class WeatherManager
    {
        private const int MAX_RAIN_PARTICLES = 8000;
        private const float RAIN_STAY_TIME = 0.25f;

        private static ParticleEmissionSettings emissionSettingsDrizzle = new ParticleEmissionSettings()
        {
            particlesPerEmit = new Point(8, 30),
            timeUntilNextEmit = new Vector2((float)Main.FIXED_STEP * 6, (float)Main.FIXED_STEP * 10)
        };

        private static ParticleEmissionSettings emissionSettingsNormal = new ParticleEmissionSettings()
        {
            particlesPerEmit = new Point(50, 100),
            timeUntilNextEmit = new Vector2((float)Main.FIXED_STEP * 6, (float)Main.FIXED_STEP * 10)
        };

        private static ParticleEmissionSettings emissionSettingsHeavy = new ParticleEmissionSettings()
        {
            particlesPerEmit = new Point(16, 32),
            timeUntilNextEmit = new Vector2((float)Main.FIXED_STEP * 1, (float)Main.FIXED_STEP * 2)
        };

        //only relevant during the day...
        private static Color[] rainingDLightColors =
        {
            new(78, 78, 78),
            new(178, 178, 178),
            new(178, 178, 178),
        };

        private static Color[] rainingSkyboxColors =
        {
            new(0.01f, 0.01f, 0.01f, 1f),
            Color.White,
            Color.White,
            Color.White,
            Color.White,
            Color.White,
        };

        private static Color[] lightningColors =
        {
            new(255, 255, 137),
            new(0, 0, 0)
        };

        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;

        public enum WeatherType
        {
            Clear,          //No clouds
            SparselyCloudy, //Some "floater" clouds
            Cloudy,         //Big puffy clouds that surround the island
            Overcast,       //Weather skybox
            Raining,        //Weather skybox w/ rain particle effect
            Storming,       //Weather skybox w/ rain particle effect and lightning
        }

        private record struct ParticleEmissionSettings
        {
            public Point particlesPerEmit;
            public Vector2 timeUntilNextEmit;
        }

        private struct RainParticle
        {
            public Vector3 position;
            public bool hasTouchedGround;
            public float timeRemaining;

            public bool inUse;
        }

        private StructuredBuffer drawInstanceBuffer;

        private RendererDeferred.InstancedDraw[] instancedData = new RendererDeferred.InstancedDraw[MAX_RAIN_PARTICLES];
        private RainParticle[] particles = new RainParticle[MAX_RAIN_PARTICLES];
        private CubePosition[] queryPositions = new CubePosition[MAX_RAIN_PARTICLES];
        private Cube[] touchedCubes = new Cube[MAX_RAIN_PARTICLES];

        private int min;
        private int max;

        public float WeatherTimer;  //How long the current weather has remaining
        public float WeatherTime;   //The overall time of the current weather

        private float timeUntilNextEmit;

        public WeatherManager(GraphicsDevice device)
        {
            drawInstanceBuffer = new StructuredBuffer(device, typeof(RendererDeferred.InstancedDraw), MAX_RAIN_PARTICLES, BufferUsage.WriteOnly, ShaderAccess.Read);
        }

        public void Update(double deltaTime, World world, DirectionalLight directionalLight, ref Color lightColor)
        {
            WeatherTimer -= (float)deltaTime;
            timeUntilNextEmit -= (float)deltaTime;

            Main.Renderer.FogExtents = new Vector2(Cube.CUBE_SCALE * 8, Cube.CUBE_SCALE * 32);

            lightColor = Utility.MultiLerp(1 - world.GetTimeOfDay(), Color.Lerp, rainingDLightColors);
            world.WeatherSkyboxAlpha = 1f;
            if (world.IsDay())
                world.WeatherSkyboxColor = Utility.MultiLerp(1 - world.GetTimeOfDay(), Color.Lerp, rainingSkyboxColors);
            else world.WeatherSkyboxColor = new Color(0.01f, 0.01f, 0.01f, 1f);

            min = MAX_RAIN_PARTICLES;
            max = 0;

            Matrix fallingMatrix = Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-Main.camera.Rotation.Y);
            Matrix onGroundMatrix = Matrix.CreateTranslation(0, Cube.CUBE_SCALE / 2f, 0) *
                Matrix.CreateRotationX(MathHelper.ToRadians(90));

            RendererDeferred.DrawSourceRectParameters fallingSR = new RendererDeferred.DrawSourceRectParameters(new RectangleF(0, 0, 16, 16));
            RendererDeferred.DrawSourceRectParameters onGroundSR1 = new RendererDeferred.DrawSourceRectParameters(new RectangleF(16, 0, 16, 16));
            RendererDeferred.DrawSourceRectParameters onGroundSR2 = new RendererDeferred.DrawSourceRectParameters(new RectangleF(32, 0, 16, 16));

            for (int i = 0; i < MAX_RAIN_PARTICLES; i++)
            {
                if (particles[i].inUse)
                {
                    min = int.Min(min, i);
                    max = int.Max(max, i);

                    if (!particles[i].hasTouchedGround)
                    {
                        particles[i].position -= new Vector3(0, Cube.CUBE_SCALE, 0);

                        queryPositions[i] = CubePosition.FromWorldSpace(particles[i].position);
                    }

                    particles[i].timeRemaining -= (float)deltaTime;

                    if (particles[i].hasTouchedGround && particles[i].timeRemaining <= 0)
                        particles[i] = new RainParticle();
                }
            }

            //If the above loop doesn't update anything then we'll be using placeholder values; this reinitializes them
            min = int.Min(min, max);
            max = int.Max(min, max);

            world.ChunkManager.InitializerView.GetCubes(queryPositions.AsSpan(), touchedCubes.AsSpan(), Main.Registry.CubeRegistry.Air, min, max - min);

            for (int i = min; i < max; i++)
            {
                if (particles[i].inUse && !particles[i].hasTouchedGround)
                {
                    if (touchedCubes[i].Solid)
                    {//put the particle on top of the block.
                        particles[i].position.Y = queryPositions[i].InWorldSpace().Y + Cube.CUBE_SCALE + Cube.CUBE_SCALE * 0.005f;
                        particles[i].hasTouchedGround = true;
                        particles[i].timeRemaining = RAIN_STAY_TIME;
                    }
                }

                RendererDeferred.DrawSourceRectParameters sr = fallingSR;

                if (particles[i].hasTouchedGround)
                {
                    if (particles[i].timeRemaining > RAIN_STAY_TIME / 2f)
                        sr = onGroundSR1;
                    else sr = onGroundSR2;
                }

                Matrix wm = (particles[i].hasTouchedGround ? onGroundMatrix : fallingMatrix) *
                        Matrix.CreateTranslation(particles[i].position);
                //THIS IS IMPORTANT! Matrices are TRANSPOSED by monogame when setting constant buffers!
                //Therefore we need to transpose our matrices the same way for our transform matrices.
                Matrix.Transpose(ref wm, out wm);
                instancedData[i] = new RendererDeferred.InstancedDraw()
                {
                    World = wm,
                    WorldNormal = Matrix.Transpose(Matrix.Invert(wm)),
                    TintColor = Color.White.ToVector3(),
                    SourceRect = sr
                };
            }

            if (max - min > 0)
                drawInstanceBuffer.SetData(instancedData, min, max - min);

            ParticleEmissionSettings settings = emissionSettingsHeavy;

            if (timeUntilNextEmit <= 0)
            {
                timeUntilNextEmit += Main.random.NextFloat(settings.timeUntilNextEmit.X, settings.timeUntilNextEmit.Y);

                int num = Main.random.Next(settings.particlesPerEmit.X, settings.particlesPerEmit.Y);

                const float MAX_RADIUS = Cube.CUBE_SCALE * 32;

                for (int i = 0; i < MAX_RAIN_PARTICLES; i++)
                {
                    if (!particles[i].inUse)
                    {
                        float r = Main.random.NextFloat(0, MAX_RADIUS);

                        Vector2 ang = Main.random.NextAngle();
                        particles[i].inUse = true;
                        particles[i].position = world.player.Position + new Vector3(ang.X * r, 0, ang.Y * r);
                        particles[i].position.Y = Cube.CUBE_SCALE * world.sizeInCubes;  //place at the top of the world for now

                        num--;

                        if (num <= 0)
                            break;
                    }
                }
            }
        }

        public void Draw(GraphicsDevice device)
        {
            if (mesh.VBO == null)
                mesh = MeshHelper.MakeCenteredQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

            Main.Renderer.DrawsPassGBufferInstanced.Add(new RendererDeferred.InstancedGBufferDraw(
                Main.assetsManager.GetAsset<Texture2D>("rain"), DrawHelper.WhitePixel, DrawHelper.BlackPixel, 
                mesh.VBO, mesh.IBO, drawInstanceBuffer, min, max - min));

            /*for (int i = min; i < max; i++)
            {
                if (particles[i].inUse)
                {
                    if (!particles[i].hasTouchedGround)
                    {
                        Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(
                            Main.assetsManager.GetAsset<Texture2D>("rain"), DrawHelper.WhitePixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                            Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                            Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                            Matrix.CreateTranslation(particles[i].position),
                            new RectangleF(0, 0, 16, 16)));
                    }
                    else
                    {
                        Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(
                            Main.assetsManager.GetAsset<Texture2D>("rain"), DrawHelper.WhitePixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
                            Matrix.CreateTranslation(0, Cube.CUBE_SCALE / 2f, 0) *
                            Matrix.CreateRotationX(MathHelper.ToRadians(90)) *
                            Matrix.CreateTranslation(particles[i].position),
                            new RectangleF(16, 0, 16, 16)));
                    }
                }
            }*/
        }
    }
}
