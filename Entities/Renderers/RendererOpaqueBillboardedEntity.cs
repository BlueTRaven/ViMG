using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Items;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public class RendererOpaqueBillboardedEntity : EntityRenderer
    {
        private class TypeStats
        {
            public RendererDeferred.DrawMaterial Material;
            public FastList<RendererDeferred.InstancedDraw> Draws;  //we cache a list here so we don't have to always allocate during a frame.
            public StructuredBuffer SBO;

            public Vector2 Scale;

            public TypeStats(RendererDeferred.DrawMaterial material, Vector2? scale = null)
            {
                this.Material = material;
                this.Scale = scale ?? Vector2.One;
                Draws = new FastList<RendererDeferred.InstancedDraw>();
            }

            // NOTE:
            // RendererOpaqueBillboardedEntity is made with opaques in mind. Transparents are only somewhat supported and aren't going to be super optimized.
            // This is because opaques can be easily batched into instances, but transparents, because they must be sorted with respect to many other
            // things, cannnot. (We use a deferred renderer, after all.)
            // Therefore, if you have a transparent entity, consider making it its own EntityRendererer instead, where you can optimize it better for that use case.
            public virtual bool GetShouldDrawTransparent(Entity entity)
            {
                return false;
            }

            // NOTE:
            // .A is discarded unless GetShouldDrawTransparent returns true, in which case it acts normally as alpha.
            public virtual Vector4 GetColor(Entity entity)
            {
                return Color.White.ToVector4();
            }

            public virtual Vector3 GetPosition(Entity entity)
            {
                return entity.Position;
            }

            public virtual RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
            {
                return new RendererDeferred.DrawSourceRectParameters
                {
                    UseSourceRect = false,
                };
            }
        }

        private class TypeStatsSourceRect : TypeStats
        {
            private readonly Rectangle sourceRect;

            public TypeStatsSourceRect(RendererDeferred.DrawMaterial material, Rectangle sourceRect, Vector2? scale = null) : base(material, scale)
            {
                this.sourceRect = sourceRect;
            }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
            {
                return new RendererDeferred.DrawSourceRectParameters
                {
                    SourceRectPos = new Vector2(sourceRect.X, sourceRect.Y),
                    SourceRectFarPos = new Vector2(sourceRect.Right, sourceRect.Bottom),
                    UseSourceRect = true,
                };
            }
        }

        private class TypeStatsSkeleton : TypeStats
        {
            public TypeStatsSkeleton() : base(new RendererDeferred.DrawMaterial("skeleton"), new Vector2(1, 2))
            {
            }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
            {
                Skeleton skeleton = (Skeleton)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 16, 32);

                if (skeleton.state != Skeleton.State.Active)
                    sourceRect = new RectangleF(16, 0, 16, 32);

                return new RendererDeferred.DrawSourceRectParameters(sourceRect);
            }
        }
        
        private class TypeStatsBigSlime : TypeStats
        {
            public TypeStatsBigSlime() : base(new RendererDeferred.DrawMaterial("slime"), new Vector2(2)) { }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
            {
                SlimeBig slime = entity as SlimeBig;

                int ysrc = 32;

                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                float interval = MathHelper.Lerp(minInterval, maxInterval, slime.ai.JumpTimer / slime.ai.JumpTime) * 2;

                if (slime.ai.OnGround && (slime.Alive % interval) / interval < 0.5f)
                    ysrc = 64;

                return new RendererDeferred.DrawSourceRectParameters(slime.noticeHandler.Noticed ? new RectangleF(32, ysrc, 32, 32) : new RectangleF(0, ysrc, 32, 32));
            }
        }

        private class TypeStatsSlime : TypeStats
        {
            public TypeStatsSlime() : base(new RendererDeferred.DrawMaterial("slime"), new Vector2(1)) { }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
            {
                Slime slime = entity as Slime;

                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;
                int ysrc = 0;
                float interval = MathHelper.Lerp(minInterval, maxInterval, slime.ai.JumpTimer / slime.ai.JumpTime) * 2;

                if (slime.ai.OnGround && (slime.alive % interval) / interval < 0.5f)
                    ysrc = 16;

                return new RendererDeferred.DrawSourceRectParameters(slime.noticeHandler.Noticed ? new RectangleF(16, ysrc, 16, 16) : new RectangleF(0, ysrc, 16, 16));
            }
        }

        private class TypeStatsCaveSlime : TypeStats
        {
            public TypeStatsCaveSlime() : base(new RendererDeferred.DrawMaterial("slime"), new Vector2(1)) { }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
            {
                CaveSlime slime = entity as CaveSlime;

                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;
                int ysrc = 0;
                float interval = MathHelper.Lerp(minInterval, maxInterval, slime.ai.JumpTimer / slime.ai.JumpTime) * 2;

                if (slime.ai.OnGround && (slime.alive % interval) / interval < 0.5f)
                    ysrc = 16;

                return new RendererDeferred.DrawSourceRectParameters(slime.noticeHandler.Noticed ? new RectangleF(48, ysrc, 16, 16) : new RectangleF(32, ysrc, 16, 16));
            }
        }

        private class TypeStatsGhost : TypeStats
        {
            public TypeStatsGhost() : base(new RendererDeferred.DrawMaterial("grave_ghost"), new Vector2(2))
            {
            }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
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

                return new RendererDeferred.DrawSourceRectParameters(sourceRect);
            }

            public override Vector3 GetPosition(Entity entity)
            {
                Vector3 offset = Vector3.Zero;

                offset.Y = MathF.Sin(MathF.PI * 2 * (entity.Alive % 4f) / 4f) * Cube.CUBE_SCALE * 0.5f;

                return base.GetPosition(entity) + offset;
            }
        }

        private class TypeStatsCultist : TypeStats
        {
            public TypeStatsCultist() : base(new RendererDeferred.DrawMaterial("cultist"), new Vector2(19f / 16f, 32f / 16f))
            {
            }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
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

                return new RendererDeferred.DrawSourceRectParameters(sourceRect);
            }
        }

        private class TypeStatsDucken : TypeStats
        {
            public TypeStatsDucken() : base(new RendererDeferred.DrawMaterial("ducken"))
            {
            }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
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

                return new RendererDeferred.DrawSourceRectParameters(sourceRect);
            }
        }

        private class TypeStatsGhoul : TypeStats
        {
            public TypeStatsGhoul() : base(new RendererDeferred.DrawMaterial("ghoul"), new Vector2(1, 2))
            {

            }

            public override bool GetShouldDrawTransparent(Entity entity)
            {
                Ghoul ghoul = entity as Ghoul;
                return ghoul.IsInLight();
            }

            public override Vector4 GetColor(Entity entity)
            {
                Ghoul ghoul = entity as Ghoul;
                return base.GetColor(entity) * ghoul.GetAlpha();
            }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
            {
                return new RendererDeferred.DrawSourceRectParameters(new RectangleF(0, 0, 16, 32));
            }
        }

        private class TypeStatsLeviathan : TypeStats
        {
            public TypeStatsLeviathan() : base(new RendererDeferred.DrawMaterial("leviathan"), new Vector2(6))
            {
            }

            public override bool GetShouldDrawTransparent(Entity entity)
            {
                EntityLeviathan leviathan = entity as EntityLeviathan;
                return leviathan.state == EntityLeviathan.State.Watching;
            }

            public override Vector4 GetColor(Entity entity)
            {
                EntityLeviathan leviathan = entity as EntityLeviathan;
                return base.GetColor(entity) * leviathan.GetAlpha();
            }

            public override RendererDeferred.DrawSourceRectParameters GetSourceRect(Entity entity)
            {
                return new RendererDeferred.DrawSourceRectParameters(new RectangleF(0, 0, 64, 64));
            }
        }

        private TypeStats[] typeStats =
        [
            new TypeStatsSourceRect(new RendererDeferred.DrawMaterial("imp"), new Rectangle(0, 16, 16, 16)),
            new TypeStatsSkeleton(),
            new TypeStatsSlime(),
            new TypeStatsBigSlime(),
            new TypeStatsCaveSlime(),
            new TypeStatsGhost(),
            new TypeStatsCultist(),
            new TypeStatsSourceRect(new RendererDeferred.DrawMaterial("salamander"), new Rectangle(0, 0, 16, 16)),
            new TypeStatsDucken(),
            new TypeStatsGhoul(),
            new TypeStats(new RendererDeferred.DrawMaterial("glow_node")),
            new TypeStatsLeviathan(),
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
        ];

        public VerySimpleMesh mesh;

        public RendererOpaqueBillboardedEntity(GraphicsDevice device) : base("generic_billboard", device)
        {
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
                Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE) *
                    Matrix.CreateScale(stats.Scale.X, stats.Scale.Y, 1) *
                    billboard *
                    Matrix.CreateTranslation(stats.GetPosition(entity));
                Matrix.Transpose(ref mat, out mat);

                if (!stats.GetShouldDrawTransparent(entity))
                {
                    RendererDeferred.InstancedDraw draw = baseDraw with
                    {
                        World = mat,
                        WorldNormal = Matrix.Transpose(Matrix.Invert(mat)),
                        SourceRect = stats.GetSourceRect(entity),
                        TintColor = stats.GetColor(entity).ToVector3(),
                    };

                    stats.Draws.Add(draw);
                }
                else
                {
                    float distance = (Main.camera.Position - entity.Position).Length();

                    RendererDeferred.TransparentDraw draw = new RendererDeferred.TransparentDraw
                    {
                        Material = stats.Material,
                        SourceRect = stats.GetSourceRect(entity),
                        TintColor = stats.GetColor(entity),
                        Mesh = mesh,
                        Transform = mat,
                        SortValue = distance,
                    };

                    Main.Renderer.AddTransparentDraw(draw);
                }
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
