using BepuPhysics.Constraints;
using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D9;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ViMG.Cubes;
using ViMG.Items;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public class RendererOpaqueBillboardedEntity : EntityRenderer
    {
        private record struct TypeStatsDrawStats
        {
            public bool shouldDraw = true;

            public RectangleF? sourceRect;
            public Vector3? position; //if null, just uses entity position!
            public Vector2? scale;

            public Color? color = Color.White;

            public TypeStatsDrawStats() { }
        }

        private abstract class TypeStats
        {
            public RendererDeferred.DrawMaterial Material;
            public FastList<RendererDeferred.InstancedDraw> Draws;  //we cache a list here so we don't have to always allocate during a frame.
            public StructuredBuffer SBO;

            public TypeStats(RendererDeferred.DrawMaterial material)
            {
                this.Material = material;
                Draws = new FastList<RendererDeferred.InstancedDraw>();
            }

            public abstract TypeStatsDrawStats[] GetDrawStats(Entity entity);
        }

        private class TypeStatsGeneric : TypeStats
        {
            private readonly RendererDeferred.DrawMaterial material;
            private readonly Vector2 scale;
            private readonly Vector3 offset;
            private readonly Color color;
            private readonly RectangleF? sourceRect;

            public TypeStatsGeneric(RendererDeferred.DrawMaterial material, Vector2? scale = null, Vector3? offset = null, Color? color = null, RectangleF? sourceRect = null) : base(material)
            {
                this.material = material;
                this.scale = scale ?? new Vector2(1);
                this.offset = offset ?? new Vector3();
                this.color = color ?? Color.White;
                this.sourceRect = sourceRect;
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];

            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                TypeStatsDrawStats stats = new TypeStatsDrawStats
                {
                    position = entity.Position + offset,
                    scale = scale,
                    color = color,
                    sourceRect = sourceRect,
                    shouldDraw = true,
                };

                cachedStats[0] = stats;
                return cachedStats;
            }
        }

        private class TypeStatsSkeleton : TypeStats
        {
            public TypeStatsSkeleton() : base(new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Skeleton skeleton = (Skeleton)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 16, 32);

                if (skeleton.state != Skeleton.State.Active)
                    sourceRect = new RectangleF(16, 0, 16, 32);

                cachedStats[0] = new TypeStatsDrawStats
                {
                    scale = new Vector2(1, 2),
                    sourceRect = sourceRect,
                };
                return cachedStats;
            }
        }
        
        private class TypeStatsBigSlime : TypeStats
        {
            public TypeStatsBigSlime() : base(new RendererDeferred.DrawMaterial("slime")) { }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                SlimeBig slime = entity as SlimeBig;

                int ysrc = 32;

                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                float interval = MathHelper.Lerp(minInterval, maxInterval, slime.ai.JumpTimer / slime.ai.JumpTime) * 2;

                if (slime.ai.OnGround && (slime.Alive % interval) / interval < 0.5f)
                    ysrc = 64;

                cachedStats[0] = new TypeStatsDrawStats
                {
                    sourceRect = slime.noticeHandler.Noticed ? new RectangleF(32, ysrc, 32, 32) : new RectangleF(0, ysrc, 32, 32),
                    scale = new Vector2(2),
                };

                return cachedStats;
            }
        }

        private class TypeStatsSlime : TypeStats
        {
            public TypeStatsSlime() : base(new RendererDeferred.DrawMaterial("slime")) { }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Slime slime = entity as Slime;

                int ysrc = 0;

                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                float interval = MathHelper.Lerp(minInterval, maxInterval, slime.ai.JumpTimer / slime.ai.JumpTime) * 2;

                if (slime.ai.OnGround && (slime.Alive % interval) / interval < 0.5f)
                    ysrc = 16;

                cachedStats[0] = new TypeStatsDrawStats
                {
                    sourceRect = slime.noticeHandler.Noticed ? new RectangleF(16, ysrc, 16, 16) : new RectangleF(0, ysrc, 16, 16),
                };

                return cachedStats;
            }
        }

        private class TypeStatsCaveSlime : TypeStats
        {
            public TypeStatsCaveSlime() : base(new RendererDeferred.DrawMaterial("slime")) { }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Slime slime = entity as Slime;

                int ysrc = 0;

                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                float interval = MathHelper.Lerp(minInterval, maxInterval, slime.ai.JumpTimer / slime.ai.JumpTime) * 2;

                if (slime.ai.OnGround && (slime.Alive % interval) / interval < 0.5f)
                    ysrc = 16;

                cachedStats[0] = new TypeStatsDrawStats
                {
                    sourceRect = slime.noticeHandler.Noticed ? new RectangleF(48, ysrc, 16, 16) : new RectangleF(32, ysrc, 16, 16),
                };

                return cachedStats;
            }
        }

        private class TypeStatsGhost : TypeStats
        {
            public TypeStatsGhost() : base(new RendererDeferred.DrawMaterial("grave_ghost"))
            {
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Ghost ghost = (Ghost)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 32, 32);

                Vector3 velXZ = new Vector3(ghost.ai.Facing.X, 0, ghost.ai.Facing.Z);
                velXZ.Normalize();

                int direction = 0;
                float facingDotCamera = Vector3.Dot(velXZ, Main.camera.ForwardYawOnly);
                bool flipX = false;

                if (facingDotCamera < -0.3f)
                {
                    direction = 2;
                    sourceRect.y = 64;
                }
                else if (facingDotCamera < 0.2f)
                {
                    direction = 1;
                    sourceRect.y = 32;

                    float facing = velXZ.X * Main.camera.ForwardYawOnly.Z - velXZ.Z * Main.camera.ForwardYawOnly.X;

                    if (facing < 0)
                    {
                        flipX = true;
                    }
                }

                if (flipX)
                {
                    sourceRect.x += 32;
                    sourceRect.width = -32;
                }

                if (ghost.ai.GetState() == AIFlierMelee<Ghost>.State.Attack)
                    sourceRect = new RectangleF(0, 96, 32, 32);
                else if (ghost.ai.GetState() == AIFlierMelee<Ghost>.State.AttackStun)
                    sourceRect = new RectangleF(32, 96, 32, 32);

                Vector3 offset = Vector3.Zero;

                offset.Y = MathF.Sin(MathF.PI * 2 * (entity.Alive % 4f) / 4f) * Cube.CUBE_SCALE * 0.5f;

                cachedStats[0] = new TypeStatsDrawStats
                {
                    position = entity.Position + offset,
                    sourceRect = sourceRect,
                    scale = new Vector2(2),
                };
                return cachedStats;
            }
        }

        private class TypeStatsCultist : TypeStats
        {
            public TypeStatsCultist() : base(new RendererDeferred.DrawMaterial("cultist"))
            {
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Cultist cultist = (Cultist)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 19, 32);

                if (cultist.ai.GetState() == AIWalkerShooter<Cultist>.State.Normal)
                {
                    if (cultist.ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        float animP = (entity.Alive % 0.75f) / 0.75f;

                        int frame = (int)(animP * 2f);

                        sourceRect = new RectangleF(22 + frame * 22, 0, 19, 32);
                    }
                }
                else if (cultist.ai.GetState() == AIWalkerShooter<Cultist>.State.Attack)
                {
                    sourceRect = new RectangleF(65, 0, 19, 32);
                }

                cachedStats[0] = new TypeStatsDrawStats
                {
                    sourceRect = sourceRect,
                    scale = new Vector2(19f / 16f, 32f / 16f),
                };

                return cachedStats;
            }
        }

        private class TypeStatsDucken : TypeStats
        {
            public TypeStatsDucken() : base(new RendererDeferred.DrawMaterial("ducken"))
            {
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Ducken ducken = (Ducken)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 32, 32);

                Vector3 velXZ = new Vector3(ducken.ai.Facing.X, 0, ducken.ai.Facing.Z);
                velXZ.Normalize();

                int direction = 0;
                float facingDotCamera = Vector3.Dot(velXZ, Main.camera.ForwardYawOnly);
                bool flipX = false;

                if (facingDotCamera < -0.3f)
                {
                    direction = 2;
                    sourceRect.y = 64;
                }
                else if (facingDotCamera < 0.2f)
                {
                    direction = 1;
                    sourceRect.y = 32;

                    float facing = velXZ.X * Main.camera.ForwardYawOnly.Z - velXZ.Z * Main.camera.ForwardYawOnly.X;

                    if (facing < 0)
                    {
                        flipX = true;
                    }
                }

                if (ducken.ai.GetState() == AIPassive<Ducken>.State.Normal)
                {
                    if (ducken.ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        int numFrames;

                        if (direction == 0 || direction == 2)
                            numFrames = 4;
                        else if (direction == 1)
                            numFrames = 2;
                        else numFrames = 0;

                        float animP = (entity.Alive % 0.75f) / 0.75f;

                        int frame = (int)(animP * numFrames);

                        sourceRect.x += 32 * frame;

                        if (flipX)
                        {
                            sourceRect.x += 32;
                            sourceRect.width = -32;
                        }
                    }
                }

                cachedStats[0] = new TypeStatsDrawStats
                {
                    sourceRect = sourceRect,
                };
                return cachedStats;
            }
        }

        private class TypeStatsGhoul : TypeStats
        {
            public TypeStatsGhoul() : base(new RendererDeferred.DrawMaterial("ghoul"))
            {

            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Ghoul ghoul = entity as Ghoul;
                Color color = Color.White * ghoul.GetAlpha();

                cachedStats[0] = new TypeStatsDrawStats
                {
                    color = color,
                    sourceRect = new RectangleF(0, 0, 16, 32),
                    scale = new Vector2(1, 2),
                };

                return cachedStats;
            }
        }

        private class TypeStatsLeviathan : TypeStats
        {
            public TypeStatsLeviathan() : base(new RendererDeferred.DrawMaterial("leviathan"))
            {
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                EntityLeviathan leviathan = entity as EntityLeviathan;

                cachedStats[0] = new TypeStatsDrawStats
                {
                    color = Color.White * leviathan.GetAlpha(),
                    sourceRect = new RectangleF(0, 0, 64, 64),
                    scale = new Vector2(6),
                };

                return cachedStats;
            }
        }

        private class TypeStatsHeart : TypeStats
        {
            public TypeStatsHeart() : base(new RendererDeferred.DrawMaterial("heart"))
            {
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Heart heart = entity as Heart;
                float healthPercent = (float)heart.Health / (float)heart.MaxHealth;

                float interval = MathHelper.Lerp(0.25f, 2f, healthPercent);

                float t = (entity.Alive % interval) / interval;

                float s = MathF.Sin(MathF.PI * 2 * t) * 0.5f + 0.5f;

                float scale = MathHelper.Lerp(0.75f, 1.15f, s);

                cachedStats[0] = new TypeStatsDrawStats
                {
                    sourceRect = new RectangleF(0, 0, 16, 21),
                    scale = new Vector2(scale),
                };

                return cachedStats;
            }
        }

        private class TypeStatsPlayerBubble : TypeStats
        {
            public TypeStatsPlayerBubble() : base(new RendererDeferred.DrawMaterial("bubble"))
            {
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                float t0 = (entity.Alive % 1.75f) / 1.75f;
                float t1 = ((entity.Alive + 0.45f) % 2.05f) / 2.05f;
                float s0 = MathF.Sin(MathF.PI * 2 * t0) * 0.5f + 0.5f;
                float s1 = MathF.Sin(MathF.PI * 2 * t1) * 0.5f + 0.5f;

                float scaleX = MathHelper.Lerp(1f, 1.15f, s0);
                float scaleY = MathHelper.Lerp(0.95f, 1.15f, s1);

                cachedStats[0] = new TypeStatsDrawStats
                {
                    scale = new Vector2(scaleX, scaleY),
                    shouldDraw = !(entity as PlayerBubble).exploding,
                    sourceRect = new RectangleF(0, 0, 64, 64),
                };

                return cachedStats;
            }
        }

        private class TypeStatsSnake : TypeStats
        {
            public TypeStatsSnake() : base(new RendererDeferred.DrawMaterial("snake"))
            {
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[1];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Snake snake = entity as Snake;

                Vector3 velXZ = new Vector3(snake.ai.Velocity.X, 0, snake.ai.Velocity.Z);
                velXZ.Normalize();

                float facingDotCamera = Vector3.Dot(velXZ, -Main.camera.Forward);

                //Facing within 45 degrees of the camera.
                bool isFacingCamera = facingDotCamera < MathHelper.ToRadians(45);

                RectangleF sourceRect = new RectangleF(0, 0, 32, 16);
                Vector2 scale = Vector2.One;

                if (isFacingCamera)
                {
                    sourceRect = new RectangleF(0, 16, 16, 16);
                }

                if (snake.ai.GetState() == AIWalkerMelee<Snake>.State.Normal)
                {
                    if (snake.ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        float animP = (entity.Alive % 0.75f) / 0.75f;

                        int frame = (int)(animP * 2f);

                        sourceRect.x += sourceRect.width * frame;
                    }
                }
                else if (snake.ai.GetState() == AIWalkerMelee<Snake>.State.Attack)
                {
                    scale = new Vector2(2);
                    sourceRect.y = 32;
                    sourceRect.width = 32;
                    sourceRect.height = 32;
                    const int NUM_FRAMES = 4;

                    int frame = (int)((1 - (snake.ai.AttackTimer / snake.ai.AttackLockTime)) * NUM_FRAMES);

                    sourceRect.x = 32 * frame;
                }

                cachedStats[0] = new TypeStatsDrawStats
                {
                    sourceRect = sourceRect,
                    scale = scale,
                };

                return cachedStats;
            }
        }

        private class TypeStatsSnakeFlying : TypeStats
        {
            public TypeStatsSnakeFlying() : base(new RendererDeferred.DrawMaterial("snake"))
            {
            }

            private static TypeStatsDrawStats[] cachedStats = new TypeStatsDrawStats[2];
            public override TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                SnakeFlying snake = entity as SnakeFlying;

                RectangleF sourceRectSnake = new RectangleF(0, 34, 32, 32);

                if (snake.aiFlying.GetState() == AIFlierMelee<SnakeFlying>.State.Attack)
                {
                    const int ATT_NUM_FRAMES = 4;
                    int frame = (int)((1 - (snake.aiFlying.AttackTimer / snake.aiFlying.AttackLockTime)) * ATT_NUM_FRAMES);
                    sourceRectSnake = new RectangleF(32 * frame, 34, 32, 32);
                }

                RectangleF sourceRectWings = new RectangleF(0, 64, 32, 32);

                const int WINGS_NUM_FRAMES = 3;
                int wingFrame = (int)((1 - ((entity.Alive % 0.25f) / 0.25f)) * WINGS_NUM_FRAMES);
                sourceRectWings.x = 32 * wingFrame;


                cachedStats[0] = new TypeStatsDrawStats
                {
                    sourceRect = sourceRectWings,
                    scale = new Vector2(2),
                    position = entity.Position,
                };

                cachedStats[1] = new TypeStatsDrawStats
                {
                    sourceRect = sourceRectSnake,
                    scale = new Vector2(2),
                    position = entity.Position,
                };

                return cachedStats;
            }
        }

        private TypeStats[] typeStats =
        [
            new TypeStatsGeneric(new RendererDeferred.DrawMaterial("imp"), sourceRect: new RectangleF(0, 16, 16, 16)),
            new TypeStatsSkeleton(),
            new TypeStatsSlime(),
            new TypeStatsBigSlime(),
            new TypeStatsCaveSlime(),
            new TypeStatsGhost(),
            new TypeStatsCultist(),
            new TypeStatsGeneric(new RendererDeferred.DrawMaterial("salamander"), sourceRect: new RectangleF(0, 0, 16, 16)),
            new TypeStatsDucken(),
            new TypeStatsGhoul(),
            new TypeStatsGeneric(new RendererDeferred.DrawMaterial(Main.assetsManager.GetAsset<Texture2D>("glow_node"), emissive: DrawHelper.WhitePixel), offset: new Vector3(0, -Cube.CUBE_SCALE, 0)),
            new TypeStatsLeviathan(),
            new TypeStatsHeart(),
            new TypeStatsPlayerBubble(),
            new TypeStatsSnake(),
            new TypeStatsSnakeFlying(),
        ];
        private Type[] renderedTypes =
        [
            typeof(Imp),
            typeof(Skeleton),
            typeof(Slime),
            typeof(SlimeBig),
            typeof(CaveSlime),
            typeof(Ghost),
            typeof(Cultist),
            typeof(CaveSalamander),
            typeof(Ducken),
            typeof(Ghoul),
            typeof(GlowNode),
            typeof(EntityLeviathan),
            typeof(Heart),
            typeof(PlayerBubble),
            typeof(Snake),
            typeof(SnakeFlying),
        ];

        public VerySimpleMesh mesh;

        public RendererOpaqueBillboardedEntity(GraphicsDevice device) : base("generic_billboard", device)
        {
            Debug.Assert(typeStats.Length == renderedTypes.Length);

            mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Bottom);// MeshHelper.MakeEnemyQuad(device, 1, 1);
        }

        public override Type[] GetRenderedTypes()
        {
            return renderedTypes;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex)
        {
            Type type = renderedTypes[renderedTypeIndex];
            TypeStats stats = typeStats[renderedTypeIndex];

            var entities = entityManager.GetAll(type);

            stats.Draws.Clear();

            Matrix billboard = Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                    Matrix.CreateRotationY(-Main.camera.Rotation.Y);

            RendererDeferred.InstancedDraw baseDraw = new RendererDeferred.InstancedDraw()
            {
                SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(0, 16, 16, 16)),
                TintColor = Color.White.ToVector3(),
            };

            foreach (Entity entity in entities)
            {
                TypeStatsDrawStats[] drawStats = stats.GetDrawStats(entity);
                foreach (TypeStatsDrawStats drawStat in drawStats)
                {
                    Vector2 scale = drawStat.scale ?? new Vector2(1);
                    Color color = drawStat.color ?? Color.White;
                    Vector3 position = drawStat.position ?? entity.Position;

                    RendererDeferred.DrawSourceRectParameters sourceRect;
                    if (drawStat.sourceRect.HasValue)
                    {
                        sourceRect = new RendererDeferred.DrawSourceRectParameters(drawStat.sourceRect.Value);
                    }
                    else sourceRect = new RendererDeferred.DrawSourceRectParameters();

                    Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE) *
                        Matrix.CreateScale(scale.X, scale.Y, 1) *
                        billboard *
                        Matrix.CreateTranslation(position);
                    Matrix.Transpose(ref mat, out mat);

                    if (color.A == 255)
                    {
                        RendererDeferred.InstancedDraw draw = baseDraw with
                        {
                            World = mat,
                            WorldNormal = Matrix.Transpose(Matrix.Invert(mat)),
                            SourceRect = sourceRect,
                            TintColor = color.ToVector3(),
                        };

                        stats.Draws.Add(draw);
                    }
                    else
                    {
                        float distance = (Main.camera.Position - position).Length();

                        RendererDeferred.TransparentDraw draw = new RendererDeferred.TransparentDraw
                        {
                            Material = stats.Material,
                            SourceRect = sourceRect,
                            TintColor = color.ToVector4(),
                            Mesh = mesh,
                            Transform = mat,
                            SortValue = distance,
                        };

                        Main.Renderer.AddTransparentDraw(draw);
                    }
                }
                //if (!stats.ShouldDraw(entity))
                //    continue;
                //Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE) *
                //    Matrix.CreateScale(stats.GetScale(entity).X, stats.GetScale(entity).Y, 1) *
                //    billboard *
                //    Matrix.CreateTranslation(stats.GetPosition(entity));
                //Matrix.Transpose(ref mat, out mat);

                //if (!stats.GetShouldDrawTransparent(entity))
                //{
                //    RendererDeferred.InstancedDraw draw = baseDraw with
                //    {
                //        World = mat,
                //        WorldNormal = Matrix.Transpose(Matrix.Invert(mat)),
                //        SourceRect = stats.GetSourceRect(entity),
                //        TintColor = stats.GetColor(entity).ToVector3(),
                //    };

                //    stats.Draws.Add(draw);
                //}
                //else
                //{
                //    float distance = (Main.camera.Position - entity.Position).Length();

                //    RendererDeferred.TransparentDraw draw = new RendererDeferred.TransparentDraw
                //    {
                //        Material = stats.Material,
                //        SourceRect = stats.GetSourceRect(entity),
                //        TintColor = stats.GetColor(entity),
                //        Mesh = mesh,
                //        Transform = mat,
                //        SortValue = distance,
                //    };

                //    Main.Renderer.AddTransparentDraw(draw);
                //}
            }

            if (stats.SBO == null || stats.SBO.ElementCount < stats.Draws.Length)
            {
                if (stats.SBO != null)
                    stats.SBO.Dispose();

                stats.SBO = new StructuredBuffer(device, typeof(RendererDeferred.InstancedDraw), stats.Draws.Buffer.Length, BufferUsage.WriteOnly, ShaderAccess.Read);
            }

            stats.SBO.SetData(stats.Draws.Buffer);

            Main.Renderer.DrawsPassGBufferInstanced.Add(new RendererDeferred.InstancedGBufferDraw(
                stats.Material, mesh, stats.SBO, 0, stats.Draws.Length));
        }
    }
}
