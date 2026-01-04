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
using ViMG.VertexDeclarations;
using System.Reflection.Metadata;
using LiteNetLib.Utils;
using Engine.Common;
using Engine.ChunkStuff;

namespace ViMG.WorldLogics
{
    public class WeatherManager
    {
        private const int MAX_RAIN_PARTICLES = 10000;
        private const float RAIN_STAY_TIME = 0.25f;
        private const float LIGHTNING_TIME = (float)Main.FIXED_STEP * 16f;

        private static Vector2 nextLightningRange = new Vector2(0.25f, 16f);

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

        private static ParticleEmissionSettings emissionSettingsVeryHeavy = new ParticleEmissionSettings()
        {
            particlesPerEmit = new Point(20, 40),
            timeUntilNextEmit = new Vector2((float)Main.FIXED_STEP * 1, (float)Main.FIXED_STEP * 2)
        };

        //only relevant during the day...
        private static Color[] rainingDLightColors =
        {
            new(0, 0, 0),
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

        private static Color[] stormingDLightColors =
        {
            new(0, 0, 0),
            new(16, 16, 16, 255),
            new(16, 16, 16, 255),
        };

        private static Color[] stormingSkyboxColors =
        {
            new(0f, 0f, 0f, 1f),
            new(16, 16, 16, 255),
            new(16, 16, 16, 255),
            new(16, 16, 16, 255),
            new(16, 16, 16, 255),
            new(16, 16, 16, 255),
        };

        private static Color[] lightningColors =
        {
            new(255, 255, 137),
            new(0, 0, 0)
        };

        private static VerySimpleMesh skyboxCloudsMesh;
        private static VerySimpleMesh rainMesh;
        private static RendererDeferred.DrawMaterial materialRain = new RendererDeferred.DrawMaterial("rain");
        private static RendererDeferred.DrawMaterial materialSparselyCloudy = new RendererDeferred.DrawMaterial("skybox_sparseclouds");
        private static RendererDeferred.DrawMaterial materialCloudy = new RendererDeferred.DrawMaterial("skybox_clouds");
        private static RendererDeferred.DrawMaterial materialFog = new RendererDeferred.DrawMaterial("skybox_fog");

        public enum WeatherType
        {
            Clear,          //No clouds
            SparselyCloudy, //Some "floater" clouds
            Cloudy,         //Big puffy clouds that surround the island
            Overcast,       //Weather skybox
            Raining,        //Weather skybox w/ rain particle effect
            Storming,       //Weather skybox w/ rain particle effect and lightning
        }

        public static WeatherType[] PassiveWeatherTypes =
        {
            WeatherType.Clear,
            WeatherType.Cloudy,
            WeatherType.SparselyCloudy,
        };

        public static WeatherType[] ActiveWeatherTypes =
        {
            WeatherType.Overcast,
            WeatherType.Raining,
            WeatherType.Storming
        };

        public enum WeatherSeverity
        {
            Low,
            Medium,
            High
        }
        
        private record struct Transition
        {
            public WeatherStats A;
            public WeatherStats B;
            public float Time;
        }

        private struct WeatherStats
        {
            public WeatherType WType;

            public Vector3 DirLightFacing;
            public Color DirLightColor;
            public float SkyboxAlpha;
            public Color SkyboxColor;
            public Vector2 FogExtents;
        }

        public record struct TransitionInfo
        {
            public WeatherType A;
            public WeatherType B;
            public float TimeLeft;
            public float Time;
        }

        #region Particle Stuff
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
        private float timeUntilNextEmit;

        private int min;
        private int max;
        #endregion

        private WeatherStats currentWeather;

        private float lightningTimer;
        private float nextLightningTimer;
        private float lightningAngle;

        private Transition currentTransition;
        private float transitionTimer;
        private WeatherType nextTransitionType;  //we can queue up one additional transition. 
        private float nextTransitionTime;

        public WeatherManager(GraphicsDevice device)
        {
            drawInstanceBuffer = new StructuredBuffer(device, typeof(RendererDeferred.InstancedDraw), MAX_RAIN_PARTICLES, BufferUsage.WriteOnly, ShaderAccess.Read);

            rainMesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE, Enums.Alignment.Center);
            //rainMesh = MeshHelper.MakeCenteredQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

            FastList<VertexCube> vertices = new FastList<VertexCube>();
            var indices = new List<int>();

            const int CYLINDER_NUM_SIDES = 16;

            for (int i = 0; i < CYLINDER_NUM_SIDES; i++)
            {
                float tc = (float)i / (float)CYLINDER_NUM_SIDES;
                float tn = ((float)i + 1) / (float)CYLINDER_NUM_SIDES;

                float cc = float.Cos(float.Pi * 2 * tc);
                float sc = float.Sin(float.Pi * 2 * tc);
                float cn = float.Cos(float.Pi * 2 * tn);
                float sn = float.Sin(float.Pi * 2 * tn);

                //cylinder_left/right_top/bottom_near/far
                Vector3 p1 = new Vector3(cc, 0, sc);
                Vector3 p2 = new Vector3(cc, 1, sc);
                Vector3 p3 = new Vector3(cn, 1, sn);
                Vector3 p4 = new Vector3(cn, 0, sn);

                Vector2 tc1 = new Vector2(tc * 4f, 1);
                Vector2 tc2 = new Vector2(tc * 4f, 0);
                Vector2 tc3 = new Vector2(tn * 4f, 0);
                Vector2 tc4 = new Vector2(tn * 4f, 1);

                int offset = vertices.Length;
                indices.Add(offset + 0);
                indices.Add(offset + 1);
                indices.Add(offset + 2);
                indices.Add(offset + 2);
                indices.Add(offset + 3);
                indices.Add(offset + 0);

                vertices.Add(new VertexCube(p1, Color.White, tc1, new Vector3(0, 1, 0)));
                vertices.Add(new VertexCube(p2, Color.White, tc2, new Vector3(0, 1, 0)));
                vertices.Add(new VertexCube(p3, Color.White, tc3, new Vector3(0, 1, 0)));
                vertices.Add(new VertexCube(p4, Color.White, tc4, new Vector3(0, 1, 0)));
            }

            skyboxCloudsMesh = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices));
            //skyboxCloudsMesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);

            currentWeather = MakeWeatherState(WeatherType.Cloudy, Color.White);
        }

        public void Update(double deltaTime, float worldTime, Vector4 lightColor)
        {
            timeUntilNextEmit -= (float)deltaTime;

            if (transitionTimer > 0)
            {
                transitionTimer -= (float)deltaTime;

                //transitioning to or from clear
                if (currentTransition.A.WType == WeatherType.Clear)
                    currentTransition.A = MakeDefaultWeatherState(new Color(lightColor));

                if (currentTransition.B.WType == WeatherType.Clear)
                    currentTransition.B = MakeDefaultWeatherState(new Color(lightColor));

                if (transitionTimer <= 0)
                    currentWeather = currentTransition.B;
            }
            else
            {
                if (nextTransitionTime > 0)
                {
                    IDoTransition(nextTransitionType, nextTransitionTime);
                    nextTransitionTime = 0;
                }
            }

            /*if (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.L) && transitionTimer <= 0)
            {
                if (currentWeather.WType == WeatherType.Cloudy)
                    DoTransition(WeatherType.Storming, 2f);
                else DoTransition(WeatherType.Cloudy, 2f);
                *//*if (currentWeather.WType == WeatherType.Clear)
                    DoTransition(WeatherType.Cloudy, 2f);
                else if (currentWeather.WType == WeatherType.Cloudy)
                    DoTransition(WeatherType.SparselyCloudy, 2f);
                else if (currentWeather.WType == WeatherType.SparselyCloudy)
                    DoTransition(WeatherType.Overcast, 2f);
                else if (currentWeather.WType == WeatherType.Overcast)
                    DoTransition(WeatherType.Raining, 2f);
                else if (currentWeather.WType == WeatherType.Raining)
                    DoTransition(WeatherType.Storming, 2f);
                else if (currentWeather.WType == WeatherType.Storming)
                    DoTransition(WeatherType.Clear, 2f);*//*
            }*/

            if (currentWeather.WType == WeatherType.Raining || IsTransitioningTo(WeatherType.Raining) || IsTransitioningFrom(WeatherType.Raining))
            {
                ref WeatherStats modifying = ref currentWeather;

                if (IsTransitioningTo(WeatherType.Raining))
                    modifying = ref currentTransition.B;
                else if (IsTransitioningFrom(WeatherType.Raining))
                    modifying = ref currentTransition.A;

                if (SurfaceTimeHelper.IsDay(worldTime))
                {
                    modifying.DirLightColor = Utility.MultiLerp(1 - SurfaceTimeHelper.GetTimeOfDay(worldTime), Color.Lerp, rainingDLightColors);
                    modifying.SkyboxColor = Utility.MultiLerp(1 - SurfaceTimeHelper.GetTimeOfDay(worldTime), Color.Lerp, rainingSkyboxColors);
                }
                else
                {
                    modifying.DirLightColor = rainingDLightColors[0];
                    modifying.SkyboxColor = rainingSkyboxColors[0];
                }
            }

            if (currentWeather.WType == WeatherType.Storming || IsTransitioningTo(WeatherType.Storming) || IsTransitioningFrom(WeatherType.Storming))
            {
                ref WeatherStats modifying = ref currentWeather;

                if (IsTransitioningTo(WeatherType.Storming))
                    modifying = ref currentTransition.B;
                else if (IsTransitioningFrom(WeatherType.Storming))
                    modifying = ref currentTransition.A;

                if (SurfaceTimeHelper.IsDay(worldTime))
                {
                    modifying.DirLightColor = Utility.MultiLerp(1 - SurfaceTimeHelper.GetTimeOfDay(worldTime), Color.Lerp, stormingDLightColors);
                    modifying.SkyboxColor = Utility.MultiLerp(1 - SurfaceTimeHelper.GetTimeOfDay(worldTime), Color.Lerp, stormingSkyboxColors);
                }
                else
                {
                    modifying.DirLightColor = stormingDLightColors[0];
                    modifying.SkyboxColor = stormingSkyboxColors[0];
                }
            }
        }

        public bool UpdateClientLight(double deltaTime, float worldTime, Skybox skybox, ref Vector3 lightDir, ref Vector4 lightColor)
        {
            if (transitionTimer > 0)
            {
                float p = 1 - (transitionTimer / currentTransition.Time);
                Main.Renderer.FogExtents = Vector2.Lerp(currentTransition.A.FogExtents, currentTransition.B.FogExtents, p);
                if (skybox != null)
                {
                    skybox.WeatherAlpha = MathHelper.Lerp(currentTransition.A.SkyboxAlpha, currentTransition.B.SkyboxAlpha, p);
                    skybox.WeatherColor = Color.Lerp(currentTransition.A.SkyboxColor, currentTransition.B.SkyboxColor, p);
                    lightColor = Color.Lerp(currentTransition.A.DirLightColor, currentTransition.B.DirLightColor, p).ToVector4()
                        * (1 - SurfaceTimeHelper.GetTimeOfDay(worldTime));
                }
                //TODO: I don't really like the solution of multiplying by time of day since this is less controllable.
            }
            else
            {
                Main.Renderer.FogExtents = currentWeather.FogExtents;
                skybox.WeatherAlpha = currentWeather.SkyboxAlpha;
                skybox.WeatherColor = currentWeather.SkyboxColor;
                lightColor = currentWeather.DirLightColor.ToVector4()
                    * (1 - SurfaceTimeHelper.GetTimeOfDay(worldTime));
                //TODO: I don't really like the solution of multiplying by time of day since this is less controllable.
            }

            if (currentWeather.WType == WeatherType.Storming || IsTransitioningTo(WeatherType.Storming) || IsTransitioningFrom(WeatherType.Storming))
            {
                ref WeatherStats modifying = ref currentWeather;

                if (lightningTimer > 0)
                {
                    lightningTimer -= (float)deltaTime;
                    modifying.DirLightColor = Utility.MultiLerp(1 - lightningTimer / LIGHTNING_TIME, Color.Lerp, lightningColors);
                    modifying.SkyboxColor = Utility.MultiLerp(1 - lightningTimer / LIGHTNING_TIME, Color.Lerp, lightningColors);

                    lightDir = Vector3.Normalize(Vector3.Transform(new Vector3(0, 0, 1),
                        Matrix.CreateRotationX(MathHelper.ToRadians(-45f)) *
                        Matrix.CreateRotationY(lightningAngle)));
                }

                if (nextLightningTimer <= 0)
                {
                    nextLightningTimer = Main.random.NextFloat(nextLightningRange.X, nextLightningRange.Y);

                    DoLightning(Main.random.NextFloat(0, float.Pi * 2f));

                    return true;
                }
                else nextLightningTimer -= (float)deltaTime;
            }

            return false;
        }

        public void DoLightning(float angle)
        {
            lightningTimer = LIGHTNING_TIME;
            lightningAngle = angle;
        }

        public void UpdateClient(double deltaTime, Camera camera, Vector3 position, ICubeGetter cubeView)
        {
            timeUntilNextEmit -= (float)deltaTime;

            if (currentWeather.WType == WeatherType.Raining || IsTransitioningTo(WeatherType.Raining) || IsTransitioningFrom(WeatherType.Raining))
            {
                EmitWeatherParticles(position, emissionSettingsHeavy);
            }

            if (currentWeather.WType == WeatherType.Storming || IsTransitioningTo(WeatherType.Storming) || IsTransitioningFrom(WeatherType.Storming))
            {
                EmitWeatherParticles(position, emissionSettingsVeryHeavy);
            }

            UpdateWeatherParticles(cubeView, camera, deltaTime);
        }

        public WeatherType GetCurrentWeather()
        {
            return currentWeather.WType;
        }

        public TransitionInfo GetCurrentTransition()
        {
            return new TransitionInfo()
            {
                A = currentTransition.A.WType,
                B = currentTransition.B.WType,
                Time = currentTransition.Time,
                TimeLeft = transitionTimer
            };
        }

        public bool IsTransitioning()
        {
            return transitionTimer > 0;
        }

        public void DoTransition(WeatherType to, float time)
        {
            if (!IsTransitioning())
                IDoTransition(to, time);
            else
            {
                nextTransitionType = to;
                nextTransitionTime = time;
            }
        }

        private void IDoTransition(WeatherType to, float time)
        {
            currentTransition = new Transition()
            {
                A = currentWeather,
                B = MakeWeatherState(to),
                Time = time
            };

            transitionTimer = time;
        }

        private WeatherStats MakeDefaultWeatherState(Color currentLightColor)
        {
            WeatherStats s = new WeatherStats()
            {
                FogExtents = Options.DefaultFogExtents,
                SkyboxAlpha = 0,
                SkyboxColor = Color.Transparent,
                DirLightColor = currentLightColor,
            };

            return s;
        }

        private WeatherStats MakeWeatherState(WeatherType weatherType, Color? currentLightColor = null)
        {
            WeatherStats stats;

            switch (weatherType)
            {
                case WeatherType.Raining:
                    stats = new WeatherStats()
                    {
                        FogExtents = new Vector2(Cube.CUBE_SCALE * 8, Cube.CUBE_SCALE * 32),
                        SkyboxAlpha = 1,
                        SkyboxColor = Color.White,
                        DirLightColor = Color.White * 0.5f
                    };
                    break;
                case WeatherType.Storming:
                    stats = new WeatherStats()
                    {
                        FogExtents = new Vector2(Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 30),
                        SkyboxAlpha = 1,
                        SkyboxColor = new Color(16, 16, 16, 255),
                        DirLightColor = Color.White * 0.15f
                    };
                    break;
                case WeatherType.Overcast:
                    stats = new WeatherStats()
                    {
                        WType = WeatherType.Overcast,
                        FogExtents = Options.DefaultFogExtents * 0.95f,
                        SkyboxAlpha = 0.95f,
                        SkyboxColor = Color.White,
                        DirLightColor = currentLightColor.GetValueOrDefault(Color.White)
                    };
                    break;
                case WeatherType.SparselyCloudy:
                case WeatherType.Cloudy:
                    stats = new WeatherStats()
                    {
                        FogExtents = Options.DefaultFogExtents * 0.95f,
                        SkyboxAlpha = 0,
                        SkyboxColor = Color.Transparent,
                        DirLightColor = currentLightColor.GetValueOrDefault(Color.White)
                    };
                    break;
                case WeatherType.Clear:
                default:
                    stats = MakeDefaultWeatherState(currentLightColor.GetValueOrDefault(Color.White));
                    break;
            }

            stats.WType = weatherType;
            return stats;
        }

        public bool IsTransitioningFrom(WeatherType weatherType)
        {
            return transitionTimer > 0 && (currentWeather.WType == weatherType || currentTransition.A.WType == weatherType);
        }

        public bool IsTransitioningTo(WeatherType weatherType)
        {
            return transitionTimer > 0 && currentTransition.B.WType == weatherType;
        }

        public bool IsTransitioningFromOrTo(WeatherType weatherType)
        {
            return IsTransitioningFrom(weatherType) || IsTransitioningTo(weatherType);
        }

        private bool IsTransitioningToSelf()
        {
            return currentTransition.A.WType == currentTransition.B.WType;
        }

        private void EmitWeatherParticles(Vector3 position, ParticleEmissionSettings settings)
        {
            if (timeUntilNextEmit <= 0)
            {
                timeUntilNextEmit += Main.random.NextFloat(settings.timeUntilNextEmit.X, settings.timeUntilNextEmit.Y);

                int num = Main.random.Next(settings.particlesPerEmit.X, settings.particlesPerEmit.Y);
                
                if (IsTransitioning())
                    num = (int)(num * (1 - transitionTimer / currentTransition.Time));

                const float MAX_RADIUS = Cube.CUBE_SCALE * 32;

                for (int i = 0; i < MAX_RAIN_PARTICLES; i++)
                {
                    if (!particles[i].inUse)
                    {
                        float r = Main.random.NextFloat(0, MAX_RADIUS);

                        Vector2 ang = Main.random.NextAngle();
                        particles[i].inUse = true;
                        particles[i].position = position + new Vector3(ang.X * r, 0, ang.Y * r);
                        particles[i].position.Y = position.Y - Cube.CUBE_SCALE * 16;  //place at the top of the world for now

                        num--;

                        if (num <= 0)
                            break;
                    }
                }
            }
        }

        private void UpdateWeatherParticles(ICubeGetter cubeView, Camera camera, double deltaTime)
        {
            min = MAX_RAIN_PARTICLES;
            max = 0;

            Matrix fallingMatrix = 
                Matrix.CreateScale(0.25f) *
                Matrix.CreateRotationX(Math.Clamp(-camera.RotationEuler.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                Matrix.CreateRotationY(-camera.RotationEuler.Y);
            Matrix onGroundMatrix =
                Matrix.CreateScale(0.25f) *
                Matrix.CreateTranslation(0, Cube.CUBE_SCALE / 2f, 0) *
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

            cubeView.GetCubes(queryPositions.AsSpan()[min..max], touchedCubes.AsSpan()[min..max], Main.Registry.CubeRegistry.Air);

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
        }

        public void Draw(GraphicsDevice device, Camera camera, float worldTime)
        {
            Main.Renderer.DrawsPassGBufferInstanced.Add(new RendererDeferred.InstancedGBufferDraw(
                materialRain, rainMesh, drawInstanceBuffer, min, max - min));

            if (currentWeather.WType == WeatherType.Cloudy || IsTransitioningFrom(WeatherType.Cloudy) || IsTransitioningTo(WeatherType.Cloudy))
            {
                const float ONE_FULL_ROTATION_CLOUDY = 6 * 60;

                float angle = (worldTime % ONE_FULL_ROTATION_CLOUDY) / ONE_FULL_ROTATION_CLOUDY;

                float p = 1;

                if (!IsTransitioningToSelf())
                {
                    if (IsTransitioningFrom(WeatherType.Cloudy))
                        p = transitionTimer / currentTransition.Time;
                    else if (IsTransitioningTo(WeatherType.Cloudy))
                        p = 1 - transitionTimer / currentTransition.Time;
                }

                Main.Renderer.DrawsSkyboxPass.Add(new RendererDeferred.TransparentDraw()
                {
                    SortValue = 199,
                    Material = materialCloudy,
                    TintColor = Color.White.ToVector4() * 0.65f * (1 - SurfaceTimeHelper.GetTimeOfDay(worldTime)) * p,
                    Transform =
                    Matrix.CreateScale(1, 0.5f, 1) *
                    Matrix.CreateRotationY(MathHelper.ToRadians(angle)) *
                    Matrix.CreateTranslation(camera.Position - Vector3.Up * 0.25f),
                    Mesh = skyboxCloudsMesh,
                });

                Main.Renderer.DrawsSkyboxPass.Add(new RendererDeferred.TransparentDraw()
                {
                    SortValue = 199,
                    Material = new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                    TintColor = Color.White.ToVector4() * 0.65f * (1 - SurfaceTimeHelper.GetTimeOfDay(worldTime)) * p,
                    Transform =
                    Matrix.CreateRotationY(MathHelper.ToRadians(angle)) *
                    Matrix.CreateTranslation(camera.Position - Vector3.Up * 1.25f),
                    Mesh = skyboxCloudsMesh,
                });
            }
            if (currentWeather.WType == WeatherType.SparselyCloudy || IsTransitioningFrom(WeatherType.SparselyCloudy) || IsTransitioningTo(WeatherType.SparselyCloudy))
            {
                const float ONE_FULL_ROTATION_SPARSECLOUDY = 60 * 4f;

                float angle = (worldTime % ONE_FULL_ROTATION_SPARSECLOUDY) / ONE_FULL_ROTATION_SPARSECLOUDY;

                float p = 1;

                if (!IsTransitioningToSelf())
                {
                    if (IsTransitioningFrom(WeatherType.SparselyCloudy))
                        p = transitionTimer / currentTransition.Time;
                    else if (IsTransitioningTo(WeatherType.SparselyCloudy))
                        p = 1 - transitionTimer / currentTransition.Time;
                }

                Main.Renderer.DrawsSkyboxPass.Add(new RendererDeferred.TransparentDraw()
                {
                    SortValue = 199,
                    Material = materialSparselyCloudy,
                    TintColor = Color.White.ToVector4() * 0.65f * (1 - SurfaceTimeHelper.GetTimeOfDay(worldTime)) * p,
                    Transform =
                    Matrix.CreateScale(1, 0.5f, 1) *
                    Matrix.CreateRotationY(MathHelper.ToRadians(angle)) *
                    Matrix.CreateTranslation(camera.Position - Vector3.Up * 0.25f),
                    Mesh = skyboxCloudsMesh,
                });

                Main.Renderer.DrawsSkyboxPass.Add(new RendererDeferred.TransparentDraw()
                {
                    SortValue = 199,
                    Material = materialFog,
                    TintColor = Color.White.ToVector4() * 0.65f * (1 - SurfaceTimeHelper.GetTimeOfDay(worldTime)) * p,
                    Transform =
                    Matrix.CreateScale(1, 0.25f, 1) *
                    Matrix.CreateRotationY(MathHelper.ToRadians(angle)) *
                    Matrix.CreateTranslation(camera.Position - Vector3.Up * 0.25f),
                    Mesh = skyboxCloudsMesh,
                });

                Main.Renderer.DrawsSkyboxPass.Add(new RendererDeferred.TransparentDraw()
                {
                    SortValue = 199,
                    Material = new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                    TintColor = Color.White.ToVector4() * 0.65f * (1 - SurfaceTimeHelper.GetTimeOfDay(worldTime)) * p,
                    Transform =
                    Matrix.CreateRotationY(MathHelper.ToRadians(angle)) *
                    Matrix.CreateTranslation(camera.Position - Vector3.Up * 1.25f),
                    Mesh = skyboxCloudsMesh,
                });
            }
            if (currentWeather.WType == WeatherType.Clear || IsTransitioningFrom(WeatherType.Clear) || IsTransitioningTo(WeatherType.Clear))
            {
                float p = 1;

                if (!IsTransitioningToSelf())
                {
                    if (IsTransitioningFrom(WeatherType.Clear))
                        p = transitionTimer / currentTransition.Time;
                    else if (IsTransitioningTo(WeatherType.Clear))
                        p = 1 - transitionTimer / currentTransition.Time;
                }

                Main.Renderer.DrawsSkyboxPass.Add(new RendererDeferred.TransparentDraw()
                {
                    SortValue = 199,
                    Material = materialFog,
                    TintColor = Color.White.ToVector4() * 0.65f * (1 - SurfaceTimeHelper.GetTimeOfDay(worldTime)) * p,
                    Transform =
                    Matrix.CreateScale(1, 0.5f, 1) *
                    Matrix.CreateTranslation(camera.Position - Vector3.Up * 0.25f),
                    Mesh = skyboxCloudsMesh,
                });

                Main.Renderer.DrawsSkyboxPass.Add(new RendererDeferred.TransparentDraw()
                {
                    SortValue = 199,
                    Material = new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                    TintColor = Color.White.ToVector4() * 0.65f * (1 - SurfaceTimeHelper.GetTimeOfDay(worldTime)) * p,
                    Transform =
                    Matrix.CreateTranslation(camera.Position - Vector3.Up * 1.25f),
                    Mesh = skyboxCloudsMesh,
                });
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put((int)this.currentWeather.WType);
            writer.Put((int)this.nextTransitionType);
            writer.Put(this.nextTransitionTime);
            writer.Put(this.transitionTimer);
            writer.Put(currentTransition.Time);
            writer.Put((int)this.currentTransition.A.WType);
            writer.Put((int)this.currentTransition.B.WType);
            writer.Put(lightningTimer);
            writer.Put(nextLightningTimer);
            writer.Put(lightningAngle);
        }

        public void Deserialize(NetDataReader reader)
        {
            currentWeather = MakeWeatherState((WeatherType)reader.GetInt());
            nextTransitionType = (WeatherType)reader.GetInt();
            nextTransitionTime = reader.GetFloat();
            transitionTimer = reader.GetFloat();
            currentTransition = new Transition
            {
                Time = reader.GetFloat(),
                A = MakeWeatherState((WeatherType)reader.GetInt()),
                B = MakeWeatherState((WeatherType)reader.GetInt()),
            };
            lightningTimer = reader.GetFloat();
            nextLightningTimer = reader.GetFloat();
            lightningAngle = reader.GetFloat();
        }
    }
}
