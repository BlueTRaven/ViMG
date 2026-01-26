using BepuPhysics.Trees;
using BrUtility;
using Engine.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
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

        private static int[] renderedTypes = [0];
        public override int[] GetRenderedTypes()
        {
            if (renderedTypes[0] == 0)
                renderedTypes[0] = Main.Registry.EntityRegistry.Get<Tree>().Id;
            return renderedTypes;
        }

        // TODO: can we optimize this like how we did it the old way, such that we only rerender when the tree changes?
        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int type)
        {
            draws.Clear();
            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entCurr = client.Current().entities.GetById(reference.id);
                var entPrev = client.Previous(1).entities.GetById(reference.id);

                if (entCurr.counters[0] != 0)
                {
                    int size = entCurr.counters[0];
                    int maxSize = entCurr.counters[1];
                    for (int j = 0; j < size; j++)
                    {
                        if (j == 0)
                        {
                            Matrix w = Matrix.CreateTranslation(entCurr.position);
                            Matrix.Transpose(ref w, out w);
                            draws.Add(new RendererDeferred.InstancedDraw()
                            {
                                World = w,
                                WorldNormal = Matrix.Transpose(Matrix.Invert(w)),
                                SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(32, 80, 16, 16)),
                            });
                        }
                        else if (j == maxSize - 1)
                        {
                            Matrix w = Matrix.CreateScale(2.5f, 3, 2.5f) * Matrix.CreateTranslation(entCurr.position + new Vector3(0, Cube.CUBE_SCALE * j, 0));
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
                            Matrix w = Matrix.CreateTranslation(entCurr.position + new Vector3(0, Cube.CUBE_SCALE * j, 0));
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
            }
            
            if (SBO == null || SBO.ElementCount < draws.Length)
            {
                if (SBO != null)
                    SBO.Dispose();

                SBO = new StructuredBuffer(device, typeof(RendererDeferred.InstancedDraw), draws.Buffer.Length, BufferUsage.WriteOnly, ShaderAccess.Read);
            }
            SBO.SetData(draws.Buffer);

            client.Renderer.DrawsPassGBufferInstanced.Add(new RendererDeferred.InstancedGBufferDraw(material, mesh, SBO, 0, draws.Length));
        }
    }
}
