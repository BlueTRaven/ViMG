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
using static ViMG.Collision3D;
using static ViMG.Entities.EntityHelper;

namespace ViMG.Entities.Renderers
{
    public class RendererOpaqueBillboardedEntity : EntityRenderer
    {
        public record struct TypeStatsDrawStats
        {
            public bool shouldDraw = true;

            public RectangleF? sourceRect;
            public Vector3? position; //if null, just uses entity position!
            public Vector2? scale;

            public Color? color = Color.White;

            public TypeStatsDrawStats() { }
        }

        public abstract class TypeStats : IRegisterable
        {
            public string Identifier { get; set; }

            public RendererDeferred.DrawMaterial Material;
            public FastList<RendererDeferred.InstancedDraw> Draws;  //we cache a list here so we don't have to always allocate during a frame.
            public StructuredBuffer SBO;
            public Type EntityType;

            public TypeStats(string identifier, Type entityType, RendererDeferred.DrawMaterial material)
            {
                this.Identifier = identifier;
                this.EntityType = entityType;
                this.Material = material;
                Draws = new FastList<RendererDeferred.InstancedDraw>();
            }


            public abstract TypeStatsDrawStats[] GetDrawStats(Entity entity);
        }

        public ObjRegistry<TypeStats> registry;

        public VerySimpleMesh mesh;

        public RendererOpaqueBillboardedEntity(GraphicsDevice device) : base("generic_billboard", device)
        { 
            mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Bottom);

            registry = new ObjRegistry<TypeStats>();
        }

        private Type?[]? renderedTypesCache = null;
        public override Type?[] GetRenderedTypes()
        {
            if (renderedTypesCache == null)
            {
                renderedTypesCache = new Type[registry.Count + 1];
                renderedTypesCache[0] = null;
                int i = 1;
                foreach (TypeStats stats in registry.GetIterable())
                {
                    renderedTypesCache[i] = stats.EntityType;
                    i++;
                }
            }
            return renderedTypesCache;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> renderedEntities)
        {
            TypeStats stats = registry.Get(renderedTypeIndex);
            Type type = stats.EntityType;

            var entities = renderedEntities;//entityManager.GetAll(type);

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
