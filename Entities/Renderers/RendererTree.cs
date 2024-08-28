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
        private RendererDeferred.DrawMaterial material;
        private VerySimpleMesh mesh;
        private StructuredBuffer SBO;
        private FastList<RendererDeferred.InstancedDraw> draws = new FastList<RendererDeferred.InstancedDraw>();
        private bool needsRebuild;
        private bool needsReupload;

        public RendererTree(GraphicsDevice device) : base("tree", device)
        {
            material = new RendererDeferred.DrawMaterial("tree");
            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();
            MeshHelper.MakeXMeshVerts(vertices, indices, Vector3.Zero, Vector3.One, new RectangleF(0, 0, 1, 1));

            mesh = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));// MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
        }

        protected override void OnEntityOfOurTypeAdded(int renderedTypeIndex, Entity entity)
        {
            base.OnEntityOfOurTypeAdded(renderedTypeIndex, entity);

            needsRebuild = true;
        }

        protected override void OnEntityOfOurTypeRemoved(int renderedTypeIndex, Entity entity)
        {
            base.OnEntityOfOurTypeRemoved(renderedTypeIndex, entity);

            needsRebuild = true;
        }

        private void BuildList(IReadOnlyCollection<Entity> renderingEntities)
        {
            for (int e = 0; e < renderingEntities.Count; e++)
            {
                Tree tree = renderingEntities.ElementAt(e) as Tree;

                RenderTree(tree);
            }

            needsReupload = true;
        }

        private void RenderTree(Tree tree)
        {
            for (int i = 0; i < tree.Size; i++)
            {
                if (i == 0)
                {
                    Matrix w = Matrix.CreateTranslation(tree.Position);
                    Matrix.Transpose(ref w, out w);
                    draws.Add(new RendererDeferred.InstancedDraw()
                    {
                        World = w,
                        WorldNormal = Matrix.Transpose(Matrix.Invert(w)),
                        SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(32, 80, 16, 16)),
                    });
                }
                else if (i == tree.MaxSize - 1)
                {
                    Matrix w = Matrix.CreateScale(2.5f, 3, 2.5f) * Matrix.CreateTranslation(tree.Position + new Vector3(0, Cube.CUBE_SCALE * i, 0));
                    Matrix.Transpose(ref w, out w);
                    draws.Add(new RendererDeferred.InstancedDraw()
                    {
                        World = w,
                        WorldNormal = Matrix.Transpose(Matrix.Invert(w)),
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
                        WorldNormal = Matrix.Transpose(Matrix.Invert(w)),
                        SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(32, 48, 16, 16)),
                    });
                }
            }
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex)
        {
            IReadOnlyList<Entity> ents = entityManager.GetAll<Tree>();

            for (int i = 0; i < ents.Count; i++)
            {
                Tree tree = ents.ElementAt(i) as Tree;
                if (tree.NeedsRerender)
                {
                    //if the tree needs to be updated, just rerender the entire list
                    //Kinda gross...
                    needsRebuild = true;
                    tree.NeedsRerender = false;
                }
            }

            if (needsRebuild)
            {
                draws.Clear();
                //draws = new FastList<RendererDeferred.InstancedDraw>();
                BuildList(entityManager.GetAll<Tree>());

                needsRebuild = false;
            }

            if (needsReupload)
            {
                if (SBO == null || SBO.ElementCount < draws.Length)
                {
                    if (SBO != null)
                        SBO.Dispose();

                    SBO = new StructuredBuffer(device, typeof(RendererDeferred.InstancedDraw), draws.Buffer.Length, BufferUsage.WriteOnly, ShaderAccess.Read);
                }
                SBO.SetData(draws.Buffer);

                needsReupload = false;
            }

            Main.Renderer.DrawsPassGBufferInstanced.Add(new RendererDeferred.InstancedGBufferDraw(material, mesh, SBO, 0, draws.Length));
        }

        private static Type[] renderedTypes = new Type[] { typeof(Tree) };
        public override Type[] GetRenderedTypes()
        {
            return renderedTypes;
        }
    }
}
