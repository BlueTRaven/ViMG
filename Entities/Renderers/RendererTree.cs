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
    public class RendererTree : EntityRenderer
    {
        private (VertexBuffer VBO, IndexBuffer IBO) mesh;
        private StructuredBuffer SBO;
        private FastList<RendererDeferred.InstancedDraw> draws = new FastList<RendererDeferred.InstancedDraw>();
        private bool needsRebuild;
        private bool needsReupload;

        public RendererTree(GraphicsDevice device) : base("tree", device)
        {
            List<VertexCube> vertices = new List<VertexCube>();
            List<int> indices = new List<int>();
            MeshHelper.MakeXMeshVerts(vertices, indices, Vector3.Zero, Vector3.One, new RectangleF(0, 0, 1, 1));

            mesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
        }

        protected override void OnEntityOfOurTypeAdded(Entity entity)
        {
            base.OnEntityOfOurTypeAdded(entity);

            //we can just add to the list of draws
            Tree tree = entity as Tree;

            for (int i = 0; i <= tree.Size; i++)
            {
                if (i == 0)
                {
                    Matrix w = Matrix.CreateTranslation(tree.Position);
                    Matrix.Transpose(ref w, out w);
                    draws.Add(new RendererDeferred.InstancedDraw()
                    {
                        World = w,
                        WorldNormal = Matrix.Invert(w),
                        SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(32, 80, 16, 16)),
                    });
                }
                else if (i == tree.MaxSize)
                {
                    Matrix w = Matrix.CreateScale(2.5f, 3, 2.5f) * Matrix.CreateTranslation(tree.Position + new Vector3(0, Cube.CUBE_SCALE * i, 0));
                    Matrix.Transpose(ref w, out w);
                    draws.Add(new RendererDeferred.InstancedDraw()
                    {
                        World = w,
                        WorldNormal = Matrix.Invert(w),
                        SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(0, 0, 80, 48)),
                    });
                }
                else
                {
                    Matrix w = Matrix.CreateTranslation(tree.Position + new Vector3(0, Cube.CUBE_SCALE * i, 0));
                    Matrix.Transpose(ref w, out w);
                    draws.Add(new RendererDeferred.InstancedDraw()
                    {
                        World = w,
                        WorldNormal = Matrix.Invert(w),
                        SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(32, 48, 16, 16)),
                    });
                }
            }

            needsReupload = true;
        }

        protected override void OnEntityOfOurTypeRemoved(Entity entity)
        {
            base.OnEntityOfOurTypeRemoved(entity);

            //This is the lazy way...
            //Since the entity may have been removed out of pretty much anywhere in the list,
            //and we really don't want to have to keep parity in indices between our draw list and the entity list (pretty much impossible),
            //just rebuild the entire thing from scratch every time an entity was removed.
            needsRebuild = true;
        }

        private void BuildList(IReadOnlyCollection<Entity> renderingEntities)
        {
            for (int e = 0; e < renderingEntities.Count; e++)
            {
                Tree tree = renderingEntities.ElementAt(e) as Tree;

                for (int i = 0; i <= tree.Size; i++)
                {
                    if (i == 0)
                    {
                        Matrix w = Matrix.CreateTranslation(tree.Position);
                        Matrix.Transpose(ref w, out w);
                        draws.Add(new RendererDeferred.InstancedDraw()
                        {
                            World = w,
                            WorldNormal = Matrix.Invert(w),
                            SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(32, 80, 16, 16)),
                        });
                    }
                    else if (i == tree.MaxSize)
                    {
                        Matrix w = Matrix.CreateScale(2.5f, 3, 2.5f) * Matrix.CreateTranslation(tree.Position + new Vector3(0, Cube.CUBE_SCALE * i, 0));
                        Matrix.Transpose(ref w, out w);
                        draws.Add(new RendererDeferred.InstancedDraw()
                        {
                            World = w,
                            WorldNormal = Matrix.Invert(w),
                            SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(0, 0, 80, 48)),
                        });
                    }
                    else
                    {
                        Matrix w = Matrix.CreateTranslation(tree.Position + new Vector3(0, Cube.CUBE_SCALE * i, 0));
                        Matrix.Transpose(ref w, out w);
                        draws.Add(new RendererDeferred.InstancedDraw()
                        {
                            World = w,
                            WorldNormal = Matrix.Invert(w),
                            SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(32, 48, 16, 16)),
                        });
                    }
                }
            }
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager)
        {
            if (draws == null || needsRebuild)
            {
                draws = new FastList<RendererDeferred.InstancedDraw>();
                BuildList(entityManager.GetAll<Tree>());

                needsRebuild = false;
            }

            if (needsReupload)
            {
                if (SBO == null || SBO.ElementCount < draws.Length)
                    SBO = new StructuredBuffer(device, typeof(RendererDeferred.InstancedDraw), draws.Buffer.Length, BufferUsage.WriteOnly, ShaderAccess.Read);
                SBO.SetData(draws.Buffer);

                needsReupload = false;
            }

            Main.Renderer.DrawsPassGBufferInstanced.Add(new RendererDeferred.InstancedGBufferDraw(Main.assetsManager.GetAsset<Texture2D>("tree"),
                DrawHelper.WhitePixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO, SBO));
        }

        public override Type GetRenderedType()
        {
            return typeof(Tree);
        }
    }
}
