using BepuPhysics.Constraints;
using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.IMGUIImpl;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities.Renderers
{
    // TODO: xmeshed entities are typically attached to cubes. They don't change much. We may benefit from allowing the option to NOT reupload SBOs from frame to frame.
    public class RendererOpaqueXMeshEntity : EntityRenderer
    {
        public record struct RenderedEntityDrawStats
        {
            public bool shouldDraw = true;

            public RectangleF? sourceRect;
            public Matrix? matrix;  //Overrides position and scale if valid
            public Vector3? position; //if null, just uses entity position!
            public Vector2? scale;

            public Color? color = Color.White;

            public RenderedEntityDrawStats() { }
        }

        public abstract class RenderedEntity : IRegisterable
        {
            public string Identifier { get; set; }
            public int EntityTypeId;

            public RendererDeferred.DrawMaterial Material;
            public FastList<RendererDeferred.InstancedDraw> Draws;  //we cache a list here so we don't have to always allocate during a frame.
            public StructuredBuffer SBO;

            public RenderedEntity(string identifier, int entityTypeId, RendererDeferred.DrawMaterial material)
            {
                this.Identifier = identifier;
                this.EntityTypeId = entityTypeId;
                this.Material = material;
                Draws = new FastList<RendererDeferred.InstancedDraw>();
            }


            public abstract RenderedEntityDrawStats[] GetDrawStats(Entity entity);
        }

        private static VerySimpleMesh mesh;

        public ObjRegistry<RenderedEntity> registry;

        public RendererOpaqueXMeshEntity(GraphicsDevice device) : base("xmesh", device)
        {
            mesh = MeshHelper.MakeXMesh(device, Vector3.Zero, 1);

            registry = new ObjRegistry<RenderedEntity>();
        }

        private int[]? renderedTypesCache = null;
        public override int[] GetRenderedTypes()
        {
            if (renderedTypesCache == null)
            {
                renderedTypesCache = new int[registry.Count];
                int i = 0;
                foreach (RenderedEntity stats in registry.GetIterable())
                {
                    renderedTypesCache[i] = stats.EntityTypeId;
                    i++;
                }
            }
            return renderedTypesCache;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> entities)
        {
            RenderedEntity stats = registry.Get(renderedTypeIndex + 1);
            Type type = Main.Registry.EntityRegistry.Get(stats.EntityTypeId).type;

            //var entities = entityManager.GetAll(type);

            stats.Draws.Clear();

            Matrix billboard = Matrix.CreateRotationX(Math.Clamp(-Main.camera.RotationEuler.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                    Matrix.CreateRotationY(-Main.camera.RotationEuler.Y);

            RendererDeferred.InstancedDraw baseDraw = new RendererDeferred.InstancedDraw()
            {
                SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(0, 16, 16, 16)),
                TintColor = Color.White.ToVector3(),
            };

            foreach (Entity entity in entities)
            {
                IMGUIConsole.Assert(entity.GetType() == type);

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
