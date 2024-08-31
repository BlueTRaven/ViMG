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

        private TypeStats[] typeStats =
        [
            new TypeStatsSourceRect(new RendererDeferred.DrawMaterial("imp"), new Rectangle(0, 16, 16, 16)),
            new TypeStatsSkeleton(),
            new TypeStatsSlime(),
            new TypeStatsBigSlime(),
            new TypeStatsCaveSlime(),
            new TypeStatsGhost(),
            new TypeStatsCultist(),
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

                RendererDeferred.InstancedDraw draw = baseDraw with
                {
                    World = mat,
                    WorldNormal = Matrix.Transpose(Matrix.Invert(mat)),
                };

                draw.SourceRect = stats.GetSourceRect(entity);

                stats.Draws.Add(draw);
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
