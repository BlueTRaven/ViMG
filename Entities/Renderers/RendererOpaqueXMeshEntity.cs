using BepuPhysics.Constraints;
using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities.Renderers
{
    // TODO: xmeshed entities are typically attached to cubes. They don't change much. We may benefit from allowing the option to NOT reupload SBOs from frame to frame.
    public class RendererOpaqueXMeshEntity : EntityRenderer
    {
        private record struct RenderedEntityDrawStats
        {
            public bool shouldDraw = true;

            public RectangleF? sourceRect;
            public Matrix? matrix;  //Overrides position and scale if valid
            public Vector3? position; //if null, just uses entity position!
            public Vector2? scale;

            public Color? color = Color.White;

            public RenderedEntityDrawStats() { }
        }

        private abstract class RenderedEntity
        {
            public RendererDeferred.DrawMaterial Material;
            public FastList<RendererDeferred.InstancedDraw> Draws;  //we cache a list here so we don't have to always allocate during a frame.
            public StructuredBuffer SBO;

            public RenderedEntity(RendererDeferred.DrawMaterial material)
            {
                this.Material = material;
                Draws = new FastList<RendererDeferred.InstancedDraw>();
            }

            public abstract RenderedEntityDrawStats[] GetDrawStats(Entity entity);
        }

        private class RenderedEntityAncientAltar : RenderedEntity
        {
            public RenderedEntityAncientAltar() : base(new RendererDeferred.DrawMaterial("cubes_textures"))
            {
            }

            private static RenderedEntityDrawStats[] cachedStats = new RenderedEntityDrawStats[1];
            public override RenderedEntityDrawStats[] GetDrawStats(Entity entity)
            {
                cachedStats[0] = new RenderedEntityDrawStats
                {
                    position = entity.Position + new Vector3(0, Cube.CUBE_SCALE, 0),
                    sourceRect = new RectangleF(112, 16, 16, 16),
                };

                return cachedStats;
            }
        }

        private class RenderedEntityCaveRoot : RenderedEntity
        {
            public RenderedEntityCaveRoot() : base(new RendererDeferred.DrawMaterial("cubes_textures"))
            {
            }

            private static RenderedEntityDrawStats[] cachedStats = new RenderedEntityDrawStats[1];
            public override RenderedEntityDrawStats[] GetDrawStats(Entity entity)
            {
                EntityCaveRoot caveRoot = entity as EntityCaveRoot;

                RectangleF sourceRect = new RectangleF(0, 176, 16, 16);

                if (entity.world.GetTime() <= caveRoot.save.grownTime)
                {
                    float growthP = (entity.world.GetTime() - caveRoot.save.creationTime) / (caveRoot.save.grownTime - caveRoot.save.creationTime);

                    const int stages = 3;

                    int currentStage = (int)((float)stages * growthP);

                    sourceRect.x = currentStage * 16;
                }
                else sourceRect.x = 2 * 16;

                cachedStats[0] = new RenderedEntityDrawStats
                {
                    sourceRect = sourceRect,
                };

                return cachedStats;
            }
        }

        private class RenderedEntitySapling : RenderedEntity
        {
            public RenderedEntitySapling() : base(new RendererDeferred.DrawMaterial("cubes_textures"))
            {
            }

            private static RenderedEntityDrawStats[] cachedStats = new RenderedEntityDrawStats[1];
            public override RenderedEntityDrawStats[] GetDrawStats(Entity entity)
            {
                cachedStats[0] = new RenderedEntityDrawStats
                {
                    position = entity.Position + new Vector3(Cube.CUBE_SCALE / 2f, 0, Cube.CUBE_SCALE / 2f),
                    sourceRect = (entity as Sapling).GetSourceRect(),
                };
                return cachedStats;
            }
        }

        private class RenderedEntityCaveCompass : RenderedEntity
        {
            public RenderedEntityCaveCompass() : base(new RendererDeferred.DrawMaterial("cubes_textures"))
            {
            }

            private static RenderedEntityDrawStats[] cachedStats = new RenderedEntityDrawStats[1];
            public override RenderedEntityDrawStats[] GetDrawStats(Entity entity)
            {
                cachedStats[0] = new RenderedEntityDrawStats
                {
                    matrix = (entity as EntityCaveCompass).GetMatrix(),
                    sourceRect = new RectangleF(16, 16, 16, 16),
                    shouldDraw = true,
                    color = Color.White,
                };
                return cachedStats;
            }
        }

        private static VerySimpleMesh mesh;

        private static RenderedEntity[] renderedTypes = [
            new RenderedEntityAncientAltar(),
            new RenderedEntityCaveRoot(),
            new RenderedEntitySapling(),
        ];
        private static Type[] types = [
            typeof(AncientAltar),
            typeof(EntityCaveRoot),
            typeof(Sapling),
        ];

        public RendererOpaqueXMeshEntity(GraphicsDevice device) : base("xmesh", device)
        {
            mesh = MeshHelper.MakeXMesh(device, Vector3.Zero, 1);
        }

        public override Type[] GetRenderedTypes()
        {
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> entities)
        {
            Type type = types[renderedTypeIndex];
            RenderedEntity stats = renderedTypes[renderedTypeIndex];

            //var entities = entityManager.GetAll(type);

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
                if (entity == null)
                {
                    Console.WriteLine("Entity was null");
                    continue;
                }
                RenderedEntityDrawStats[] drawStats = stats.GetDrawStats(entity);
                foreach (RenderedEntityDrawStats drawStat in drawStats)
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

                    Matrix mat = drawStat.matrix ?? Matrix.CreateScale(Cube.CUBE_SCALE) *
                        Matrix.CreateScale(scale.X, scale.Y, 1) *
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
